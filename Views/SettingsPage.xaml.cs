using Mechanical_Keyboard.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Mechanical_Keyboard.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsViewModel ViewModel => (SettingsViewModel)DataContext;

        public SettingsPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (App.MainWindow != null)
            {
                App.MainWindow.Activated += Window_Activated;
            }
            _ = ViewModel.RefreshStartupTaskStateAsync();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (App.MainWindow != null)
            {
                App.MainWindow.Activated -= Window_Activated;
            }
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState != WindowActivationState.Deactivated)
            {
                _ = ViewModel.RefreshStartupTaskStateAsync();
            }
        }

        private void StartupToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggle)
            {
                // Determine the state BEFORE the user's toggle action
                bool previousState = ViewModel.IsStartupTaskEnabled;

                // If the UI's new state doesn't match the ViewModel's old state,
                // it means the user initiated this change.
                if (toggle.IsOn != previousState)
                {
                    // 1. Immediately revert the visual change to the authoritative state.
                    //    The UI will not appear to change.
                    toggle.IsOn = previousState;

                    // 2. Execute the command with the user's INTENDED new state.
                    ViewModel.SetStartupTaskCommand.Execute(!previousState);
                }
            }
        }
    }
}
