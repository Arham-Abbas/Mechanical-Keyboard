using Mechanical_Keyboard.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Mechanical_Keyboard.Views
{
    public sealed partial class GeneralPage : Page
    {
        // This property provides the data context for the XAML page.
        public SettingsViewModel ViewModel { get; }

        public GeneralPage()
        {
            InitializeComponent();
            // Create the ViewModel, getting the singleton services from the App class.
            ViewModel = new SettingsViewModel(
                App.KeyboardSoundService!,
                App.SettingsService!
            );
        }
    }
}
