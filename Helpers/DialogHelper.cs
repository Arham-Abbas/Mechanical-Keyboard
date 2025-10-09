using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Windows.System;

namespace Mechanical_Keyboard.Helpers
{
    public static class DialogHelper
    {
        private static ContentDialog? _progressDialog;

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

        public static async Task ShowImportSuccessDialogAsync(string packName)
        {
            if (App.MainWindow?.Content is not FrameworkElement rootElement) return;

            var dialog = new ContentDialog
            {
                Title = "Import Successful",
                Content = $"The sound pack '{packName}' was successfully imported.",
                CloseButtonText = "OK",
                XamlRoot = rootElement.XamlRoot
            };
            await dialog.ShowAsync();
        }

        public static async Task ShowImportFailedDialogAsync()
        {
            if (App.MainWindow?.Content is not FrameworkElement rootElement) return;

            var dialog = new ContentDialog
            {
                Title = "Import Failed",
                Content = "The selected folder is not a valid sound pack. A valid pack must contain at least a 'key-press.wav' file.",
                CloseButtonText = "OK",
                XamlRoot = rootElement.XamlRoot
            };
            await dialog.ShowAsync();
        }

        public static async Task<ContentDialogResult> ShowOverwriteConfirmationDialogAsync(string packName)
        {
            if (App.MainWindow?.Content is not FrameworkElement rootElement)
            {
                return ContentDialogResult.None;
            }

            var dialog = new ContentDialog
            {
                Title = "Pack Already Exists",
                Content = $"A sound pack named '{packName}' already exists. Do you want to overwrite it?",
                PrimaryButtonText = "Overwrite",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = rootElement.XamlRoot
            };
            return await dialog.ShowAsync();
        }

        public static async Task<ContentDialogResult> ShowDeleteConfirmationDialogAsync(string packName, bool isDefault)
        {
            if (App.MainWindow?.Content is not FrameworkElement rootElement)
            {
                return ContentDialogResult.None;
            }

            var title = isDefault ? "Hide Default Pack" : "Delete Custom Pack";
            var content = isDefault
                ? $"Are you sure you want to hide the default '{packName}' pack? You can restore it later from the settings."
                : $"Are you sure you want to permanently delete the '{packName}' sound pack? This action cannot be undone.";
            var primaryButtonText = isDefault ? "Hide" : "Delete";

            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = primaryButtonText,
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = rootElement.XamlRoot
            };

            // Apply destructive styling for permanent deletion of custom packs
            if (!isDefault)
            {
                dialog.PrimaryButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"];
            }

            return await dialog.ShowAsync();
        }

        public static async Task<ContentDialogResult> ShowFFmpegDownloadConfirmationDialogAsync()
        {
            if (App.MainWindow?.Content is not FrameworkElement rootElement)
            {
                return ContentDialogResult.None;
            }

            var dialog = new ContentDialog
            {
                Title = "FFmpeg Required",
                Content = "To import audio files, this application needs to download FFmpeg, a free and open-source audio conversion tool. It will be stored locally within the application's data folder. Do you want to proceed with the download?",
                PrimaryButtonText = "Download",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = rootElement.XamlRoot
            };
            return await dialog.ShowAsync();
        }

        public static void ShowProgressDialog(string title, string message)
        {
            if (_progressDialog != null) return;

            if (App.MainWindow?.Content is not FrameworkElement rootElement)
            {
                return;
            }

            _progressDialog = new ContentDialog
            {
                Title = title,
                Content = new StackPanel
                {
                    Spacing = 12,
                    Children =
                    {
                        new TextBlock { Text = message },
                        new ProgressRing { IsIndeterminate = true }
                    }
                },
                XamlRoot = rootElement.XamlRoot
            };

            _ = _progressDialog.ShowAsync();
        }

        public static void HideProgressDialog()
        {
            _progressDialog?.Hide();
            _progressDialog = null;
        }
    }
}
