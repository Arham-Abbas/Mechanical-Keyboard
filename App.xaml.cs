using Mechanical_Keyboard.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Threading.Tasks;
using H.NotifyIcon;
using Microsoft.UI.Dispatching; // Required for DispatcherQueue

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Mechanical_Keyboard
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;
        private KeyboardSoundService? _keyboardSoundService;
        private TaskbarIcon? _trayIcon;
        private SettingsService? _settingsService;
        private DispatcherQueue? _dispatcherQueue;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            InitializeTrayIcon();
            await InitializeServicesAsync();
        }

        private async Task InitializeServicesAsync()
        {
            // Create services on a background thread to avoid blocking the UI
            await Task.Run(() =>
            {
                _settingsService = new SettingsService();
                _keyboardSoundService = new KeyboardSoundService(_settingsService.CurrentSettings.SoundFilePath);
            });

            // Enable the hook on the UI thread to prevent deadlocks
            if (_keyboardSoundService != null && _dispatcherQueue != null)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    _keyboardSoundService.IsEnabled = _settingsService!.CurrentSettings.IsEnabled;
                });
            }
        }

        private void InitializeTrayIcon()
        {
            _trayIcon = new TaskbarIcon
            {
                ToolTipText = "Mechanical Keyboard",
                IconSource = new BitmapImage(new Uri("ms-appx:///Assets/TrayIcon.ico"))
            };

            var menu = new MenuFlyout();
            var settingsItem = new MenuFlyoutItem { Text = "Settings" };
            settingsItem.Click += TrayIcon_Settings_Click;
            var exitItem = new MenuFlyoutItem { Text = "Exit" };
            exitItem.Click += TrayIcon_Exit_Click;

            menu.Items.Add(settingsItem);
            menu.Items.Add(new MenuFlyoutSeparator());
            menu.Items.Add(exitItem);

            _trayIcon.ContextFlyout = menu;
            _trayIcon.ForceCreate();
        }

        private void TrayIcon_Settings_Click(object sender, RoutedEventArgs e)
        {
            if (_window == null) return;
            
            _window.Activate();
            
            // Bring window to foreground
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(_window);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow?.Show();
            appWindow?.MoveInZOrderAtTop();
        }

        private void TrayIcon_Exit_Click(object sender, RoutedEventArgs e)
        {
            // Cleanly dispose all resources before exiting
            _keyboardSoundService?.Dispose();
            _trayIcon?.Dispose();
            Current.Exit();
        }
    }
}
