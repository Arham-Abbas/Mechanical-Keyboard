using Mechanical_Keyboard.ViewModels;
using Microsoft.UI.Xaml.Controls;
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

        public AboutViewModel ViewModel => (AboutViewModel)DataContext;

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
