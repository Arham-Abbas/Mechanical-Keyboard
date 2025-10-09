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
using System.Linq;

namespace Mechanical_Keyboard
{
    public partial class App : Application
    {
        public static MainWindow? MainWindow { get; private set; }
        public static SettingsService? SettingsService { get; private set; }
        public static KeyboardSoundService? KeyboardSoundService { get; private set; }
        public static DispatcherQueue? DispatcherQueue { get; private set; }
        private Window? _window;
        private bool _handleWindowClosed = true;
        private ToggleMenuFlyoutItem? _toggleEnabledMenuItem;
        private TaskbarIcon? _trayIcon;

        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            try
            {
                DispatcherQueue = DispatcherQueue.GetForCurrentThread();

                SettingsService = new SettingsService();

                var currentVersion = Helpers.AppInfo.Version;
                var lastRunVersion = SettingsService.CurrentSettings.LastRunAppVersion;

                if (currentVersion != lastRunVersion)
                {
                    if (SettingsService.CurrentSettings.RestoreDeletedPacksOnUpdate)
                    {
                        SettingsService.CurrentSettings.DeletedDefaultPacks.Clear();
                    }
                    SettingsService.CurrentSettings.LastRunAppVersion = currentVersion;
                    SettingsService.SaveSettings(SettingsService.CurrentSettings);
                }

                // --- Robust Service Initialization ---
                try
                {
                    var allPacks = SettingsService.GetAvailableSoundPacks();
                    var initialPack = allPacks.FirstOrDefault(p => p.PackDirectory == SettingsService.CurrentSettings.SoundPackDirectory)
                                      ?? allPacks.FirstOrDefault();

                    if (initialPack != null)
                    {
                        // Ensure the settings reflect the pack that is about to be loaded.
                        if (SettingsService.CurrentSettings.SoundPackDirectory != initialPack.PackDirectory)
                        {
                            SettingsService.CurrentSettings.SoundPackDirectory = initialPack.PackDirectory;
                            SettingsService.SaveSettings(SettingsService.CurrentSettings);
                        }
                        KeyboardSoundService = new KeyboardSoundService(initialPack);
                    }
                    else
                    {
                        Debug.WriteLine($"[FATAL] No sound packs found. The sound service will be disabled.");
                        KeyboardSoundService = null;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[FATAL] Sound service failed to initialize: {ex.Message}. The sound service will be disabled.");
                    KeyboardSoundService = null;
                }
                // --- End Robust Service Initialization ---

                if (KeyboardSoundService != null)
                {
                    KeyboardSoundService.PropertyChanged += KeyboardSoundService_PropertyChanged;
                }

                InitializeTrayIcon();

                if (_toggleEnabledMenuItem != null)
                {
                    if (KeyboardSoundService != null)
                    {
                        KeyboardSoundService.IsEnabled = SettingsService.CurrentSettings.IsEnabled;
                        _toggleEnabledMenuItem.IsChecked = KeyboardSoundService.IsEnabled;
                    }
                    else
                    {
                        _toggleEnabledMenuItem.IsChecked = false;
                        _toggleEnabledMenuItem.IsEnabled = false;
                    }
                }

                _ = RegisterStartupTaskAsync();

                _window = new MainWindow();
                MainWindow = _window as MainWindow;
                _window.Closed += (s, e) =>
                {
                    if (_handleWindowClosed)
                    {
                        e.Handled = true;
                        _window.Hide();
                    }
                };

                var appActivationArguments = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();
                if (appActivationArguments.Kind == ExtendedActivationKind.StartupTask)
                {
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

        private void KeyboardSoundService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(KeyboardSoundService.IsEnabled) && _toggleEnabledMenuItem != null)
            {
                _toggleEnabledMenuItem.IsChecked = KeyboardSoundService!.IsEnabled;
            }
        }

        private void ShowSettingsWindow()
        {
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
            if (SettingsService != null && SettingsService.CurrentSettings.IsFirstRun)
            {
                var startupTask = await StartupTask.GetAsync("MechanicalKeyboardAutoStart");
                if (startupTask.State == StartupTaskState.Disabled)
                {
                    await startupTask.RequestEnableAsync();
                }

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
