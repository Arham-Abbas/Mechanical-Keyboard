using Mechanical_Keyboard.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Mechanical_Keyboard.Views
{
    public sealed partial class SoundPacksPage : Page
    {
        public SoundPacksViewModel ViewModel { get; }

        public SoundPacksPage()
        {
            InitializeComponent();
            ViewModel = new SoundPacksViewModel();
        }
    }
}
