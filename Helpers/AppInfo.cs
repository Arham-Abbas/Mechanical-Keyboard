using System;
using Windows.ApplicationModel;

namespace Mechanical_Keyboard.Helpers
{
    /// <summary>
    /// Provides static, app-wide information for data binding.
    /// </summary>
    internal static class AppInfo
    {
        public static string Version => $"Version {GetAppVersion()}";
        public static Uri GitHubRepoUri => new("https://github.com/Arham-Abbas/Mechanical-Keyboard");
        public static Uri GitHubIssuesUri => new("https://github.com/Arham-Abbas/Mechanical-Keyboard/issues");

        private static string GetAppVersion()
        {
            try
            {
                var version = Package.Current.Id.Version;
                // Format as Major.Minor.Build.Revision for consistency
                return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
            }
            catch
            {
                // Fallback for when the app is not running in a packaged context
                return "0.0.0.0 (Unpackaged)";
            }
        }
    }
}
