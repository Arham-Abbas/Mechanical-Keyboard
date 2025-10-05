using Mechanical_Keyboard.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Mechanical_Keyboard.Dialogs
{
    public sealed partial class CreateSoundPackDialog : ContentDialog
    {
        public ImportDialogViewModel ViewModel { get; }

        public CreateSoundPackDialog(ImportDialogViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
        }
    }
}
