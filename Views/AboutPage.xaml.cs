using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.ApplicationModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Mechanical_Keyboard.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AboutPage : Page
    {
        public string AppVersion { get; }

        public AboutPage()
        {
            InitializeComponent();
            AppVersion = GetAppVersion();
        }

        private static string GetAppVersion()
        {
            try
            {
                var version = Package.Current.Id.Version;
                // Format as Major.Minor.Build for user-friendliness
                return $"Version {version.Major}.{version.Minor}.{version.Build}";
            }
            catch
            {
                // Fallback for when the app is not running in a packaged context
                return "Version 0.0.0 (Unpackaged)";
            }
        }
    }
}
