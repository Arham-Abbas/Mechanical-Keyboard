using Mechanical_Keyboard.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using H.NotifyIcon;
using Microsoft.UI.Dispatching;
using Mechanical_Keyboard.Helpers;
using Windows.ApplicationModel;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Mechanical_Keyboard
{
    public partial class App : Application
    {
        private Window? _window;
        private bool _handleWindowClosed = true;
        private ToggleMenuFlyoutItem? _toggleEnabledMenuItem;
        private TaskbarIcon? _trayIcon; // Store as a field to allow for proper disposal

        // Public, static properties to act as singletons for the services
        public static KeyboardSoundService? KeyboardSoundService { get; private set; }
        public static SettingsService? SettingsService { get; private set; }
        public static DispatcherQueue? DispatcherQueue { get; private set; }

        public App()
        {
            InitializeComponent();
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            try
            {
                DispatcherQueue = DispatcherQueue.GetForCurrentThread();
                
                InitializeTrayIcon();
                
                await Task.WhenAll(
                    RegisterStartupTaskAsync(),
                    InitializeServicesAsync()
                );

                bool startMinimized = args.Arguments.Contains("/background");
                if (!startMinimized)
                {
                    ShowSettingsWindow();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FATAL] Unhandled exception in OnLaunched: {ex}");
                if (Debugger.IsAttached) Debugger.Break();
            }
        }

        private void InitializeTrayIcon()
        {
            _trayIcon = new TaskbarIcon
            {
                ToolTipText = "Mechanical Keyboard",
                IconSource = new BitmapImage(new Uri("ms-appx:///Assets/TrayIcon.ico"))
            };

            var showSettingsCommand = new XamlUICommand();
            showSettingsCommand.ExecuteRequested += (s, e) => ShowSettingsWindow();
            _trayIcon.LeftClickCommand = showSettingsCommand;

            var menu = new MenuFlyout();
            _toggleEnabledMenuItem = new ToggleMenuFlyoutItem { Text = "Enabled" };
            var toggleEnabledCommand = new XamlUICommand();
            toggleEnabledCommand.ExecuteRequested += ToggleEnabledCommand_ExecuteRequested;
            _toggleEnabledMenuItem.Command = toggleEnabledCommand;

            var exitItem = new MenuFlyoutItem { Text = "Exit" };
            var exitApplicationCommand = new XamlUICommand();
            exitApplicationCommand.ExecuteRequested += ExitApplicationCommand_ExecuteRequested;
            exitItem.Command = exitApplicationCommand;

            menu.Items.Add(_toggleEnabledMenuItem);
            menu.Items.Add(new MenuFlyoutSeparator());
            menu.Items.Add(exitItem);

            _trayIcon.ContextFlyout = menu;
            _trayIcon.ForceCreate();
        }

        private async Task InitializeServicesAsync()
        {
            await Task.Run(() =>
            {
                SettingsService = new SettingsService();
                KeyboardSoundService = new KeyboardSoundService(SettingsService.CurrentSettings.SoundFilePath);
            });

            if (KeyboardSoundService != null && DispatcherQueue != null)
            {
                // Subscribe to the service's PropertyChanged event to keep the UI in sync.
                KeyboardSoundService.PropertyChanged += KeyboardSoundService_PropertyChanged;

                // Set the initial state on the UI thread.
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (KeyboardSoundService != null && _toggleEnabledMenuItem != null)
                    {
                        KeyboardSoundService.IsEnabled = SettingsService!.CurrentSettings.IsEnabled;
                        _toggleEnabledMenuItem.IsChecked = KeyboardSoundService.IsEnabled;
                    }
                });
            }
        }

        private void KeyboardSoundService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // This event is raised by the service whenever its state changes.
            // Update the tray icon's checkmark here.
            if (e.PropertyName == nameof(KeyboardSoundService.IsEnabled) && _toggleEnabledMenuItem != null)
            {
                _toggleEnabledMenuItem.IsChecked = KeyboardSoundService!.IsEnabled;
            }
        }

        private void ShowSettingsWindow()
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

        private void ToggleEnabledCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            // This command's ONLY responsibility is to change the state in the service.
            if (KeyboardSoundService == null) return;
            
            // The service will then raise the PropertyChanged event, which updates all UI.
            KeyboardSoundService.IsEnabled = !KeyboardSoundService.IsEnabled;
        }

        private void ExitApplicationCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            _handleWindowClosed = false;
            
            KeyboardSoundService?.Dispose();
            _trayIcon?.Dispose(); // Now correctly disposes the tray icon.
            
            _window?.Close();

            if (_window == null)
            {
                Current.Exit();
            }
        }

        private static async Task RegisterStartupTaskAsync()
        {
            var startupTask = await StartupTask.GetAsync("MechanicalKeyboardAutoStart");
            
            if (startupTask.State == StartupTaskState.Disabled)
            {
                await startupTask.RequestEnableAsync();
            }
        }
    }
}
