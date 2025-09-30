using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Mechanical_Keyboard.Helpers;
using Mechanical_Keyboard.Models;
using Mechanical_Keyboard.Services;
using Windows.ApplicationModel;

namespace Mechanical_Keyboard.ViewModels
{
    public partial class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly SettingsService _settingsService;
        private SoundPackInfo? _selectedSoundPack;
        private bool _isStartupTaskEnabled;

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
            // The setter is now private to prevent the UI from changing it directly
            private set
            {
                if (_isStartupTaskEnabled != value)
                {
                    _isStartupTaskEnabled = value;
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
                if (_selectedSoundPack != value && value != null)
                {
                    _selectedSoundPack = value;
                    _settingsService.CurrentSettings.SoundPackName = value.DisplayName;
                    _settingsService.SaveSettings(_settingsService.CurrentSettings);
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ImportSoundPackCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public SettingsViewModel()
        {
            _settingsService = App.SettingsService!;
            ImportSoundPackCommand = new RelayCommand(ImportSoundPack);
            
            // Initialize the new command
            SetStartupTaskCommand = new AsyncRelayCommand<bool?>(SetStartupTaskStateAsync);

            LoadSoundPacks();
            _selectedSoundPack = SoundPacks.FirstOrDefault(p => p.DisplayName == _settingsService.CurrentSettings.SoundPackName);

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

        // This method is now a proper async Task and is executed by the command
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
        }

        private static void ImportSoundPack()
        {
            // This method will be implemented later with the new "Import Dialog"
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            App.DispatcherQueue?.TryEnqueue(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }
    }
}
