using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Mechanical_Keyboard.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using H.NotifyIcon;

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
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();

            // Initialize and start the keyboard sound service
            var soundPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "key-press.wav");
            _keyboardSoundService = new KeyboardSoundService(soundPath);
            _keyboardSoundService.Start();

            // Get the tray icon from resources
            _trayIcon = (TaskbarIcon)Resources["TrayIcon"];
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
