using System.ComponentModel;
using System.Runtime.CompilerServices;
using Mechanical_Keyboard.Services;

namespace Mechanical_Keyboard.ViewModels
{
    public partial class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly KeyboardSoundService _keyboardSoundService;
        private readonly SettingsService _settingsService;

        public bool IsEnabled
        {
            get => _keyboardSoundService.IsEnabled;
            set
            {
                if (_keyboardSoundService.IsEnabled != value)
                {
                    _keyboardSoundService.IsEnabled = value;
                    // The service will raise the event, no need to call OnPropertyChanged here.
                    SaveSettings();
                }
            }
        }

        public double Volume
        {
            get => _keyboardSoundService.Volume * 100;
            set
            {
                if ((_keyboardSoundService.Volume * 100) != value)
                {
                    _keyboardSoundService.Volume = value / 100.0;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public SettingsViewModel(KeyboardSoundService keyboardSoundService, SettingsService settingsService)
        {
            _keyboardSoundService = keyboardSoundService;
            _settingsService = settingsService;

            // Subscribe to the service's property changes
            _keyboardSoundService.PropertyChanged += KeyboardSoundService_PropertyChanged;
        }

        private void KeyboardSoundService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // When a property changes in the service, notify the UI to update itself.
            if (e.PropertyName == nameof(IsEnabled))
            {
                OnPropertyChanged(nameof(IsEnabled));
            }
        }

        private void SaveSettings()
        {
            var settings = _settingsService.CurrentSettings;
            settings.IsEnabled = this.IsEnabled;
            settings.Volume = _keyboardSoundService.Volume; // Get volume directly from the service
            _settingsService.SaveSettings(settings);
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            // This is already marshalled to the UI thread by the service
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
