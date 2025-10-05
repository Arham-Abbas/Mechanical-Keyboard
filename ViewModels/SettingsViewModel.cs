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
    public partial class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly SettingsService _settingsService;
        private SoundPackInfo? _selectedSoundPack;
        private bool _isStartupTaskEnabled;
        private bool _isGridView = true;
        private bool _isServiceToggleEnabled = true;

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

        // Properties for Sound Pack Management
        public ObservableCollection<SoundPackInfo> SoundPacks { get; } = [];
        public SoundPackInfo? SelectedSoundPack
        {
            get => _selectedSoundPack;
            set
            {
                // This handles the case where the list is cleared and the selection is removed
                if (_selectedSoundPack == value) return;

                _selectedSoundPack = value;

                if (value != null)
                {
                    // 1. Persist the new setting
                    _settingsService.CurrentSettings.SoundPackName = value.DisplayName;
                    _settingsService.SaveSettings(_settingsService.CurrentSettings);

                    // 2. Directly command the service to reload
                    var packDirectory = _settingsService.GetSoundPackDirectory(value.DisplayName);
                    App.KeyboardSoundService?.ReloadSoundPack(packDirectory);
                }
                else
                {
                    // If no pack is selected (e.g., all were deleted), update settings accordingly
                    _settingsService.CurrentSettings.SoundPackName = string.Empty;
                    _settingsService.SaveSettings(_settingsService.CurrentSettings);
                }

                OnPropertyChanged();
                // Refresh the CanExecute state of the delete command
                (DeleteSoundPackCommand as AsyncRelayCommand<object?>)?.OnCanExecuteChanged();
            }
        }

        public ICommand ImportSoundPackCommand { get; }
        public ICommand DeleteSoundPackCommand { get; }

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
            SetStartupTaskCommand = new AsyncRelayCommand<bool?>(SetStartupTaskStateAsync);
            SetGridViewCommand = new RelayCommand(() => IsGridView = true);
            SetListViewCommand = new RelayCommand(() => IsGridView = false);

            // Correctly initialize the command to use the instance method
            PreviewSoundPackCommand = new RelayCommand<SoundPackInfo?>(p => PreviewSoundPack(p));

            LoadSoundPacks();
            _selectedSoundPack = SoundPacks.FirstOrDefault(p => p.DisplayName == _settingsService.CurrentSettings.SoundPackName);
            CheckServiceState();

            InitializeAsync();

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
        }

        private async void InitializeAsync()
        {
            await RefreshStartupTaskStateAsync();
        }

        public async Task RefreshStartupTaskStateAsync()
        {
            var startupTask = await StartupTask.GetAsync("MechanicalKeyboardAutoStart");
            IsStartupTaskEnabled = startupTask.State is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
        }

        // This method is a proper async Task and is executed by the command
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

            // After the operation, refresh the property from the OS to get the true state.
            await RefreshStartupTaskStateAsync();
        }

        private void LoadSoundPacks()
        {
            SoundPacks.Clear();
            var packs = _settingsService.GetAvailableSoundPacks();
            foreach (var pack in packs)
            {
                SoundPacks.Add(pack);
            }
            CheckServiceState();
        }

        private void CheckServiceState()
        {
            if (SoundPacks.Count == 0)
            {
                IsEnabled = false;
                IsServiceToggleEnabled = false;
            }
            else
            {
                IsServiceToggleEnabled = true;
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
                        SelectedSoundPack = SoundPacks.FirstOrDefault(p => p.DisplayName == model.PackName);
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

            // If the deleted pack was the last one, select null. Otherwise, select the first available pack.
            SelectedSoundPack = SoundPacks.FirstOrDefault();
        }

        // Method to handle the preview logic - now an instance method
        private void PreviewSoundPack(SoundPackInfo? pack)
        {
            if (pack is null || App.KeyboardSoundService is null)
            {
                return;
            }

            // Get the authoritative directory from the service, not from the UI object.
            var packDirectory = _settingsService.GetSoundPackDirectory(pack.DisplayName);
            var soundFilePath = Path.Combine(packDirectory, "key-press.wav");
            App.KeyboardSoundService.PlayPreviewSound(soundFilePath);
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            App.DispatcherQueue?.TryEnqueue(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }
    }
}
