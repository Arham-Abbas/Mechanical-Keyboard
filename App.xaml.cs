using Mechanical_Keyboard.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System;
using System.Threading.Tasks;
using H.NotifyIcon;
using Microsoft.UI.Dispatching;
using Mechanical_Keyboard.Helpers;

namespace Mechanical_Keyboard
{
    public partial class App : Application
    {
        private Window? _window;
        private KeyboardSoundService? _keyboardSoundService;
        private TaskbarIcon? _trayIcon;
        private SettingsService? _settingsService;
        private DispatcherQueue? _dispatcherQueue;
        private bool _handleWindowClosed = true;

        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            InitializeTrayIcon();
            Task.Run(InitializeServicesAsync);
        }

        private void InitializeTrayIcon()
        {
            var showSettingsCommand = (XamlUICommand)Resources["ShowSettingsCommand"];
            showSettingsCommand.ExecuteRequested += ShowSettingsCommand_ExecuteRequested;

            var exitApplicationCommand = (XamlUICommand)Resources["ExitApplicationCommand"];
            exitApplicationCommand.ExecuteRequested += ExitApplicationCommand_ExecuteRequested;

            _trayIcon = (TaskbarIcon)Resources["TrayIcon"];
            _trayIcon.ForceCreate();
        }

        private async Task InitializeServicesAsync()
        {
            await Task.Run(() =>
            {
                _settingsService = new SettingsService();
                _keyboardSoundService = new KeyboardSoundService(_settingsService.CurrentSettings.SoundFilePath);
            });

            if (_keyboardSoundService != null && _dispatcherQueue != null)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    _keyboardSoundService.IsEnabled = _settingsService!.CurrentSettings.IsEnabled;
                });
            }
        }

        private void ShowSettingsCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (_window == null)
            {
                _window = new MainWindow();
                _window.Closed += (s, e) =>
                {
                    if (_handleWindowClosed)
                    {
                        e.Handled = true;
                        _window.Hide();
                    }
                };
            }
            
            _window.Activate();
            _window.GetAppWindow().Show();
        }

        private void ExitApplicationCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            _handleWindowClosed = false;
            
            _keyboardSoundService?.Dispose();
            _trayIcon?.Dispose();
            
            _window?.Close();

            if (_window == null)
            {
                Current.Exit();
            }
        }
    }
}
