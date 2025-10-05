using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Mechanical_Keyboard.Helpers;
using Mechanical_Keyboard.Models;
using Microsoft.UI.Xaml.Media;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Mechanical_Keyboard.ViewModels
{
    public partial class ImportDialogViewModel : INotifyPropertyChanged
    {
        private string _packName = string.Empty;
        private string? _description;
        private string? _iconSourcePath; // This will become the private backing field
        private bool _generatePitchVariants = true;
        private string _validationMessage = string.Empty;
        private Brush? _validationMessageBrush;
        private bool _isPackNameValid = false;
        private bool _isAtLeastOneFileAssigned = false;

        public string PackName
        {
            get => _packName;
            set
            {
                if (SetProperty(ref _packName, value))
                {
                    ValidatePackName();
                }
            }
        }

        public string? Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        // This is the new "smart" property the UI will bind to.
        public string ResolvedIconPath => _iconSourcePath ?? "ms-appx:///Assets/StoreLogo.png";

        public bool GeneratePitchVariants
        {
            get => _generatePitchVariants;
            set => SetProperty(ref _generatePitchVariants, value);
        }

        public ObservableCollection<StagedFile> StagedFiles { get; } = [];

        public List<KeyAssignmentType> KeyAssignmentTypes { get; } =
            [.. Enum.GetValues<KeyAssignmentType>()];

        public string ValidationMessage
        {
            get => _validationMessage;
            private set => SetProperty(ref _validationMessage, value);
        }

        public Brush? ValidationMessageBrush
        {
            get => _validationMessageBrush;
            private set => SetProperty(ref _validationMessageBrush, value);
        }

        public bool CanImport => _isPackNameValid && _isAtLeastOneFileAssigned;

        public ICommand SelectIconCommand { get; }

        public ImportDialogViewModel(IEnumerable<string> filePaths)
        {
            foreach (var path in filePaths)
            {
                // Pass the list of types into the StagedFile constructor.
                var stagedFile = new StagedFile(path, this, KeyAssignmentTypes);
                StagedFiles.Add(stagedFile);
            }

            SelectIconCommand = new AsyncRelayCommand<object?>(_ => SelectIconAsync());
            
            // Validate all properties to ensure a consistent state before binding.
            ValidateAssignments();
            ValidatePackName();
        }

        private void ValidatePackName()
        {
            _isPackNameValid = !string.IsNullOrWhiteSpace(PackName);
            OnPropertyChanged(nameof(CanImport));
        }

        public void ValidateAssignments()
        {
            _isAtLeastOneFileAssigned = StagedFiles.Any(f => KeyAssignmentTypes[f.AssignmentIndex] == KeyAssignmentType.KeyPress);
            OnPropertyChanged(nameof(CanImport));
        }

        private async Task SelectIconAsync()
        {
            var filePicker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                FileTypeFilter = { ".png", ".jpg", ".jpeg", ".bmp", ".gif" }
            };

            var hwnd = App.GetMainWindowHandle();
            InitializeWithWindow.Initialize(filePicker, hwnd);

            var file = await filePicker.PickSingleFileAsync();
            if (file != null)
            {
                _iconSourcePath = file.Path;
                OnPropertyChanged(nameof(ResolvedIconPath)); // Notify the UI to update the image
            }
        }

        public SoundPackCreationModel? CreateModel()
        {
            if (!CanImport) return null;

            var filesToImport = StagedFiles
                .Where(f => KeyAssignmentTypes[f.AssignmentIndex] != KeyAssignmentType.Unassigned)
                .Select(f => new SoundFileToImport
                {
                    FilePath = f.FilePath,
                    Assignment = KeyAssignmentTypes[f.AssignmentIndex]
                }).ToList();

            return new SoundPackCreationModel
            {
                PackName = PackName,
                Description = Description,
                IconSourcePath = _iconSourcePath, // Use the private field here
                GeneratePitchVariants = GeneratePitchVariants,
                FilesToImport = filesToImport
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
