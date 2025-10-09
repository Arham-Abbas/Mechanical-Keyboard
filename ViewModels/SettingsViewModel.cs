using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Mechanical_Keyboard.Dialogs;
using Mechanical_Keyboard.Exceptions;
using Mechanical_Keyboard.Helpers;
using Mechanical_Keyboard.Models;
using Mechanical_Keyboard.Services;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Mechanical_Keyboard.ViewModels
{
    // Helper class for grouping sound packs in the UI
    public partial class SoundPackGroup(string key, List<SoundPackInfo> items) : List<SoundPackInfo>(items)
    {
        public string Key { get; private set; } = key;
    }

    public partial class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly SettingsService _settingsService;
        private SoundPackInfo? _selectedSoundPack;
        private bool _isStartupTaskEnabled;
        private bool _isGridView = true;
        private bool _isServiceToggleEnabled = true;
        public bool IsInitialized { get; private set; } = false;

        // This command will handle the logic for the startup toggle
        public ICommand SetStartupTaskCommand { get; }

        // Properties for General Settings
        public bool IsEnabled
        {
            get => App.KeyboardSoundService?.IsEnabled ?? false;
            set
            {
                if (App.KeyboardSoundService != null && App.KeyboardSoundService.IsEnabled != value)
                {
                    App.KeyboardSoundService.IsEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsServiceToggleEnabled
        {
            get => _isServiceToggleEnabled;
            set
            {
                if (_isServiceToggleEnabled != value)
                {
                    _isServiceToggleEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public double Volume
        {
            get => (App.KeyboardSoundService?.Volume ?? 1.0) * 100;
            set
            {
                if (App.KeyboardSoundService != null)
                {
                    App.KeyboardSoundService.Volume = value / 100.0;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsStartupTaskEnabled
        {
            get => _isStartupTaskEnabled;
            // The setter is private to prevent the UI from changing it directly
            private set
            {
                if (_isStartupTaskEnabled != value)
                {
                    _isStartupTaskEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        // Property for the restore toggle
        public bool RestoreDeletedPacksOnUpdate
        {
            get => _settingsService.CurrentSettings.RestoreDeletedPacksOnUpdate;
            set
            {
                if (_settingsService.CurrentSettings.RestoreDeletedPacksOnUpdate != value)
                {
                    _settingsService.CurrentSettings.RestoreDeletedPacksOnUpdate = value;
                    _settingsService.SaveSettings(_settingsService.CurrentSettings);
                    OnPropertyChanged();
                }
            }
        }

        // A grouped collection for the UI and a flat list for internal logic.
        public ObservableCollection<SoundPackGroup> SoundPackGroups { get; } = [];
        private List<SoundPackInfo> _allSoundPacksFlat = [];
        public bool HasCustomPacks => SoundPackGroups.Any(g => g.Key == "Custom Packs");

        public SoundPackInfo? SelectedSoundPack
        {
            get => _selectedSoundPack;
            private set // Setter is now private
            {
                if (_selectedSoundPack == value) return;

                _selectedSoundPack = value;

                OnPropertyChanged();
                (DeleteSoundPackCommand as AsyncRelayCommand<object?>)?.OnCanExecuteChanged();
            }
        }

        public void OnSoundPackSelected(SoundPackInfo? pack)
        {
            if (pack == _selectedSoundPack)
            {
                return;
            }

            // Handle the case where no pack is selected (e.g., after deleting the last one)
            if (pack == null)
            {
                App.KeyboardSoundService?.ClearSoundCache();
                _settingsService.CurrentSettings.SoundPackDirectory = string.Empty;
                _settingsService.SaveSettings(_settingsService.CurrentSettings);
                SelectedSoundPack = null;
                return;
            }

            // --- Validation and State Change ---
            try
            {
                App.KeyboardSoundService?.ReloadSoundPack(pack);
                _settingsService.CurrentSettings.SoundPackDirectory = pack.PackDirectory;
                _settingsService.SaveSettings(_settingsService.CurrentSettings);
                SelectedSoundPack = pack;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Failed to switch to sound pack '{pack.DisplayName}': {ex.Message}");
            }
        }

        public ICommand ImportSoundPackCommand { get; }
        public ICommand DeleteSoundPackCommand { get; }
        public ICommand RestoreDefaultPacksCommand { get; }

        // Properties and commands for the view switcher
        public bool IsGridView
        {
            get => _isGridView;
            set
            {
                if (_isGridView != value)
                {
                    _isGridView = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsListView));
                }
            }
        }

        public bool IsListView => !_isGridView;

        public ICommand SetGridViewCommand { get; }
        public ICommand SetListViewCommand { get; }

        // Command for the preview button
        public ICommand PreviewSoundPackCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public SettingsViewModel()
        {
            _settingsService = App.SettingsService!;
            ImportSoundPackCommand = new AsyncRelayCommand<object?>(ImportSoundPackAsync);
            DeleteSoundPackCommand = new AsyncRelayCommand<object?>(DeleteSoundPackAsync, _ => SelectedSoundPack != null);
            RestoreDefaultPacksCommand = new RelayCommand(RestoreDefaultPacks);
            SetStartupTaskCommand = new AsyncRelayCommand<bool?>(SetStartupTaskStateAsync);
            SetGridViewCommand = new RelayCommand(() => IsGridView = true);
            SetListViewCommand = new RelayCommand(() => IsGridView = false);

            PreviewSoundPackCommand = new RelayCommand<SoundPackInfo?>(PreviewSoundPack);

            // Set the flag to false before loading data
            IsInitialized = false;

            LoadSoundPacks();

            // Trust that the KeyboardSoundService has already loaded the correct pack on startup.
            // Here, we just synchronize the ViewModel's state to match the service's state
            // without triggering another reload.
            var initialPack = _allSoundPacksFlat.FirstOrDefault(p => p.PackDirectory == _settingsService.CurrentSettings.SoundPackDirectory);
            _selectedSoundPack = initialPack ?? _allSoundPacksFlat.FirstOrDefault();


            CheckServiceState();

            _ = RefreshStartupTaskStateAsync();

            if (App.KeyboardSoundService != null)
            {
                App.KeyboardSoundService.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(IsEnabled))
                    {
                        OnPropertyChanged(nameof(IsEnabled));
                    }
                };
            }

            // Set the flag to true only after all setup is complete.
            IsInitialized = true;
        }

        public async Task RefreshStartupTaskStateAsync()
        {
            var startupTask = await StartupTask.GetAsync("MechanicalKeyboardAutoStart");
            IsStartupTaskEnabled = startupTask.State is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
        }

        private async Task SetStartupTaskStateAsync(bool? enable)
        {
            if (enable is null) return;

            var startupTask = await StartupTask.GetAsync("MechanicalKeyboardAutoStart");

            if (enable.Value)
            {
                var newState = await startupTask.RequestEnableAsync();
                if (newState == StartupTaskState.DisabledByUser)
                {
                    await DialogHelper.ShowStartupTaskDisabledDialogAsync();
                }
            }
            else
            {
                startupTask.Disable();
            }

            await RefreshStartupTaskStateAsync();
        }

        private void LoadSoundPacks()
        {
            SoundPackGroups.Clear();
            _allSoundPacksFlat = _settingsService.GetAvailableSoundPacks();

            var groups = _allSoundPacksFlat
                .GroupBy(p => p.IsDefault ? "Default Packs" : "Custom Packs")
                .OrderByDescending(g => g.Key == "Default Packs") // Ensure "Default" is first
                .Select(g => new SoundPackGroup(g.Key, [.. g.OrderBy(p => p.DisplayName)]));

            foreach (var group in groups)
            {
                // Only add groups that have items
                if (group.Any())
                {
                    SoundPackGroups.Add(group);
                }
            }

            OnPropertyChanged(nameof(HasCustomPacks));
            CheckServiceState();
        }

        private void CheckServiceState()
        {
            IsServiceToggleEnabled = _allSoundPacksFlat.Count > 0;
            if (!IsServiceToggleEnabled)
            {
                IsEnabled = false;
            }
        }

        private async Task ImportSoundPackAsync(object? arg)
        {
            IReadOnlyList<StorageFile>? files = null;
            try
            {
                var filePicker = new FileOpenPicker
                {
                    SuggestedStartLocation = PickerLocationId.MusicLibrary,
                    FileTypeFilter = { ".wav", ".mp3", ".m4a", ".aac" },
                    ViewMode = PickerViewMode.List
                };

                var hwnd = App.GetMainWindowHandle();
                InitializeWithWindow.Initialize(filePicker, hwnd);

                files = await filePicker.PickMultipleFilesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] File picker failed: {ex.Message}");
                return;
            }

            if (files == null || files.Count == 0) return;

            var dialogViewModel = new ImportDialogViewModel(files.Select(f => f.Path));
            var dialog = new CreateSoundPackDialog(dialogViewModel)
            {
                XamlRoot = App.MainWindow?.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                var model = dialogViewModel.CreateModel();
                if (model != null)
                {
                    try
                    {
                        await _settingsService.CreateSoundPackAsync(model);
                    }
                    catch (PackExistsException ex)
                    {
                        var overwriteResult = await DialogHelper.ShowOverwriteConfirmationDialogAsync(ex.PackName);
                        if (overwriteResult == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
                        {
                            await _settingsService.CreateSoundPackAsync(model, overwrite: true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ERROR] Failed to create sound pack: {ex.Message}");
                    }
                    finally
                    {
                        LoadSoundPacks();
                        var newPack = _allSoundPacksFlat.FirstOrDefault(p => p.DisplayName == model.PackName && !p.IsDefault);
                        OnSoundPackSelected(newPack);
                    }
                }
            }
        }

        private async Task DeleteSoundPackAsync(object? _)
        {
            if (SelectedSoundPack == null) return;

            var packToDelete = SelectedSoundPack;
            var result = await DialogHelper.ShowDeleteConfirmationDialogAsync(packToDelete.DisplayName, packToDelete.IsDefault);
            if (result != Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                return;
            }

            await _settingsService.DeleteSoundPackAsync(packToDelete);
            LoadSoundPacks();

            // After deleting, select the first available pack, or null if none exist.
            OnSoundPackSelected(_allSoundPacksFlat.FirstOrDefault());
        }

        private void RestoreDefaultPacks()
        {
            var previouslySelectedPackDir = SelectedSoundPack?.PackDirectory;

            _settingsService.RestoreDefaultPacks();
            LoadSoundPacks();

            if (previouslySelectedPackDir != null)
            {
                // Find the equivalent pack in the new list and set it.
                var packToReselect = _allSoundPacksFlat.FirstOrDefault(p => p.PackDirectory == previouslySelectedPackDir);
                SelectedSoundPack = packToReselect;
            }

            // If nothing was selected before, or the old selection is gone, select the first available.
            if (SelectedSoundPack == null)
            {
                OnSoundPackSelected(_allSoundPacksFlat.FirstOrDefault());
            }
            else
            {
                // If the selection is still valid, just notify the UI to re-sync.
                OnPropertyChanged(nameof(SelectedSoundPack));
            }
        }

        private static void PreviewSoundPack(SoundPackInfo? pack)
        {
            if (pack is null || App.KeyboardSoundService is null)
            {
                return;
            }

            var soundFilePath = Path.Combine(pack.PackDirectory, "key-press.wav");
            App.KeyboardSoundService.PlayPreviewSound(soundFilePath);
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            App.DispatcherQueue?.TryEnqueue(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }
    }
}
