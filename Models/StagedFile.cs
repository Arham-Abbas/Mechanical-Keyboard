using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Mechanical_Keyboard.ViewModels;

namespace Mechanical_Keyboard.Models
{
    public partial class StagedFile(string filePath, ImportDialogViewModel parentViewModel, List<KeyAssignmentType> keyAssignmentTypes) : INotifyPropertyChanged
    {
        private int _assignmentIndex;

        public string FilePath { get; } = filePath;
        public string FileName { get; } = Path.GetFileName(filePath);
        public long FileSize { get; } = new FileInfo(filePath).Length;
        public ImportDialogViewModel ParentViewModel { get; } = parentViewModel;

        // Add a property to hold the list of available assignments.
        public List<KeyAssignmentType> KeyAssignmentTypes { get; } = keyAssignmentTypes;

        public int AssignmentIndex
        {
            get => _assignmentIndex;
            set
            {
                if (_assignmentIndex != value)
                {
                    _assignmentIndex = value;
                    OnPropertyChanged();
                    // Notify the parent view model that an assignment has changed.
                    ParentViewModel.ValidateAssignments();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
