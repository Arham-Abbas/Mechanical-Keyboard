using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Mechanical_Keyboard.Helpers;
using Mechanical_Keyboard.Services;
using Windows.Storage.Pickers;

namespace Mechanical_Keyboard.ViewModels
{
    public partial class SoundPacksViewModel : INotifyPropertyChanged
    {
        private readonly SettingsService _settingsService;
        private string _selectedSoundPack;

        public ObservableCollection<string> SoundPacks { get; } = [];

        public string SelectedSoundPack
        {
            get => _selectedSoundPack;
            set
            {
                if (_selectedSoundPack != value)
                {
                    _selectedSoundPack = value;
                    _settingsService.SaveSettings(new Models.SettingsModel
                    {
                        IsEnabled = _settingsService.CurrentSettings.IsEnabled,
                        Volume = _settingsService.CurrentSettings.Volume,
                        SoundPackName = value
                    });
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ImportSoundPackCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public SoundPacksViewModel()
        {
            _settingsService = App.SettingsService!;
            _selectedSoundPack = _settingsService.CurrentSettings.SoundPackName;
            
            ImportSoundPackCommand = new AsyncRelayCommand(ImportSoundPackAsync);

            LoadSoundPacks();
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

        private async Task ImportSoundPackAsync()
        {
            var folderPicker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.Downloads,
                FileTypeFilter = { "*" }
            };

            // Correctly get the window handle for the picker
            var hwnd = App.GetMainWindowHandle();
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                var newPackName = await _settingsService.ImportSoundPackAsync(folder.Path);
                if (newPackName != null)
                {
                    LoadSoundPacks();
                    SelectedSoundPack = newPackName;
                }
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
