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
using WinRT.Interop;
using Microsoft.Windows.AppLifecycle;
using Microsoft.UI;
using Microsoft.UI.Windowing;

namespace Mechanical_Keyboard
{
    public partial class App : Application
    {
        private Window? _window;
        private bool _handleWindowClosed = true;
        private ToggleMenuFlyoutItem? _toggleEnabledMenuItem;
        private TaskbarIcon? _trayIcon;

        public static KeyboardSoundService? KeyboardSoundService { get; private set; }
        public static SettingsService? SettingsService { get; private set; }
        public static DispatcherQueue? DispatcherQueue { get; private set; }

        // This static property provides access to the main window
        public static Window? MainWindow => (Current as App)?._window;

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

                // Create the main window instance but don't show it yet.
                _window = new MainWindow();
                _window.Closed += (s, e) =>
                {
                    if (_handleWindowClosed)
                    {
                        e.Handled = true;
                        _window.Hide();
                    }
                };

                // Use the AppLifecycle API to check the activation kind
                var appActivationArguments = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();
                if (appActivationArguments.Kind == ExtendedActivationKind.StartupTask)
                {
                    // If launched from startup, minimize the window.
                    var hWnd = WindowNative.GetWindowHandle(_window);
                    var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
                    var appWindow = AppWindow.GetFromWindowId(windowId);
                    if (appWindow.Presenter is OverlappedPresenter presenter)
                    {
                        presenter.Minimize();
                    }
                }
                else
                {
                    // If launched normally (e.g., from Start Menu), activate and show the window.
                    _window.Activate();
                    _window.GetAppWindow().Show();
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
                SettingsService.SoundPackChanged += OnSoundPackChanged;

                var soundPackDir = SettingsService.GetSoundPackDirectory(SettingsService.CurrentSettings.SoundPackName);
                KeyboardSoundService = new KeyboardSoundService(soundPackDir);
            });

            if (KeyboardSoundService != null)
            {
                KeyboardSoundService.PropertyChanged += KeyboardSoundService_PropertyChanged;

                DispatcherQueue?.TryEnqueue(() =>
                {
                    if (KeyboardSoundService != null && _toggleEnabledMenuItem != null)
                    {
                        KeyboardSoundService.IsEnabled = SettingsService!.CurrentSettings.IsEnabled;
                        _toggleEnabledMenuItem.IsChecked = KeyboardSoundService.IsEnabled;
                    }
                });
            }
        }

        private async void OnSoundPackChanged(object? sender, EventArgs e)
        {
            KeyboardSoundService?.Dispose();

            await Task.Run(() =>
            {
                var soundPackDir = SettingsService!.GetSoundPackDirectory(SettingsService.CurrentSettings.SoundPackName);
                KeyboardSoundService = new KeyboardSoundService(soundPackDir);
            });

            if (KeyboardSoundService != null)
            {
                KeyboardSoundService.PropertyChanged += KeyboardSoundService_PropertyChanged;
                KeyboardSoundService.IsEnabled = SettingsService!.CurrentSettings.IsEnabled;
            }
        }

        private void KeyboardSoundService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(KeyboardSoundService.IsEnabled) && _toggleEnabledMenuItem != null)
            {
                _toggleEnabledMenuItem.IsChecked = KeyboardSoundService!.IsEnabled;
            }
        }

        private void ShowSettingsWindow()
        {
            // This method ensures the window is visible and activated.
            if (_window != null)
            {
                _window.Activate();
                _window.GetAppWindow().Show();
            }
        }

        private void ToggleEnabledCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (KeyboardSoundService == null || SettingsService == null) return;

            KeyboardSoundService.IsEnabled = !KeyboardSoundService.IsEnabled;

            SettingsService.CurrentSettings.IsEnabled = KeyboardSoundService.IsEnabled;
            SettingsService.SaveSettings(SettingsService.CurrentSettings);
        }

        private void ExitApplicationCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            _handleWindowClosed = false;
            
            KeyboardSoundService?.Dispose();
            _trayIcon?.Dispose();
            
            _window?.Close();

            if (_window == null)
            {
                Current.Exit();
            }
        }

        private static async Task RegisterStartupTaskAsync()
        {
            // This logic only runs ONCE in the application's lifetime.
            if (SettingsService != null && SettingsService.CurrentSettings.IsFirstRun)
            {
                var startupTask = await StartupTask.GetAsync("MechanicalKeyboardAutoStart");
                if (startupTask.State == StartupTaskState.Disabled)
                {
                    // Request to enable the task on first run.
                    await startupTask.RequestEnableAsync();
                }

                // Mark first run as complete and save the setting.
                SettingsService.CurrentSettings.IsFirstRun = false;
                SettingsService.SaveSettings(SettingsService.CurrentSettings);
            }
        }

        public static IntPtr GetMainWindowHandle()
        {
            if (Current is App app && app._window != null)
            {
                return WindowNative.GetWindowHandle(app._window);
            }
            return IntPtr.Zero;
        }
    }
}
