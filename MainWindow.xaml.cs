using Mechanical_Keyboard.ViewModels;
using Microsoft.UI.Xaml;

namespace Mechanical_Keyboard
{
    public sealed partial class MainWindow : Window
    {
        public SettingsViewModel ViewModel { get; }

        public MainWindow()
        {
            InitializeComponent();
            Title = "Mechanical Keyboard Settings";

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            ViewModel = new SettingsViewModel(
                App.KeyboardSoundService!,
                App.SettingsService!
            );

            // Set the initial selected item in the NavigationView
            NavView.SelectedItem = GeneralNavItem;
        }
    }
}