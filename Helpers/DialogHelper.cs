using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Windows.System;

namespace Mechanical_Keyboard.Helpers
{
    public static class DialogHelper
    {
        public static async Task ShowStartupTaskDisabledDialogAsync()
        {
            if (App.MainWindow?.Content is not FrameworkElement rootElement)
            {
                return;
            }

            var dialog = new ContentDialog
            {
                Title = "Startup Task is Managed by Windows",
                Content = "You have previously disabled this app's startup task in Windows Settings or Task Manager.\n\nTo re-enable it, please use the button below to open the Startup Apps settings page.",
                PrimaryButtonText = "Open Startup Settings",
                CloseButtonText = "Close",
                XamlRoot = rootElement.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // Deep link to the Windows Settings page for startup apps
                await Launcher.LaunchUriAsync(new Uri("ms-settings:startupapps"));
            }
        }
    }
}
