using System;
using System.IO;
using System.Text.Json;
using Mechanical_Keyboard.Models;

namespace Mechanical_Keyboard.Services
{
    public class SettingsService
    {
        private readonly string _settingsFilePath;
        public SettingsModel CurrentSettings { get; private set; }

        public SettingsService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolderPath = Path.Combine(appDataPath, "MechanicalKeyboardEmulator");
            Directory.CreateDirectory(appFolderPath); // Ensure the directory exists
            _settingsFilePath = Path.Combine(appFolderPath, "settings.json");
            CurrentSettings = LoadSettings();
        }

        private SettingsModel LoadSettings()
        {
            if (!File.Exists(_settingsFilePath))
            {
                var defaultSettings = new SettingsModel
                {
                    SoundFilePath = Path.Combine(AppContext.BaseDirectory, "Assets", "key-press.wav")
                };
                SaveSettings(defaultSettings);
                return defaultSettings; // Corrected: Return the settings that were just saved.
            }

            try
            {
                var json = File.ReadAllText(_settingsFilePath);
                // Use the source generator for deserialization
                return JsonSerializer.Deserialize(json, SettingsJsonContext.Default.SettingsModel) ?? new SettingsModel();
            }
            catch
            {
                return new SettingsModel();
            }
        }

        public void SaveSettings(SettingsModel settings)
        {
            try
            {
                // Use the source generator for serialization
                var json = JsonSerializer.Serialize(settings, SettingsJsonContext.Default.SettingsModel);
                File.WriteAllText(_settingsFilePath, json);
                CurrentSettings = settings;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
    }
}
