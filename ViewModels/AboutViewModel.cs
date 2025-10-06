using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Mechanical_Keyboard.Helpers;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Mechanical_Keyboard.ViewModels
{
    public partial class AboutViewModel : INotifyPropertyChanged
    {
        private ImageSource? _gitHubAvatarSource;
        private string? _gitHubBio;

        public ImageSource? GitHubAvatarSource
        {
            get => _gitHubAvatarSource;
            private set { _gitHubAvatarSource = value; OnPropertyChanged(); }
        }

        public string? GitHubBio
        {
            get => _gitHubBio;
            private set { _gitHubBio = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public AboutViewModel()
        {
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            await LoadGitHubInfoAsync();
        }

        private async Task LoadGitHubInfoAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Mechanical-Keyboard", AppInfo.Version.Replace("Version ", "")));
                var response = await client.GetAsync("https://api.github.com/users/Arham-Abbas");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("avatar_url", out var avatarUrlElement) && avatarUrlElement.GetString() is { } avatarUrl)
                {
                    GitHubAvatarSource = new BitmapImage(new Uri(avatarUrl));
                }

                if (root.TryGetProperty("bio", out var bioElement))
                {
                    GitHubBio = bioElement.GetString();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Failed to load GitHub info: {ex.Message}");
                GitHubBio = "Could not load profile information.";
                GitHubAvatarSource = new BitmapImage(new Uri("ms-appx:///Assets/TrayIcon.ico"));
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            App.DispatcherQueue?.TryEnqueue(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }
    }
}
