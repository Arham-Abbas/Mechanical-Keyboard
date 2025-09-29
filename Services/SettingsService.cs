using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Mechanical_Keyboard.Models;

namespace Mechanical_Keyboard.Services
{
    public class SettingsService
    {
        private readonly string _settingsFilePath;
        private readonly string _soundPacksFolderPath;
        public SettingsModel CurrentSettings { get; private set; }

        public event EventHandler? SoundPackChanged;

        public SettingsService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolderPath = Path.Combine(appDataPath, "Mechanical Keyboard");
            _soundPacksFolderPath = Path.Combine(appFolderPath, "SoundPacks");
            Directory.CreateDirectory(_soundPacksFolderPath);

            _settingsFilePath = Path.Combine(appFolderPath, "settings.json");
            CurrentSettings = LoadSettings();
        }

        public List<string> GetAvailableSoundPacks()
        {
            var packs = new List<string> { "Default" };
            var customPackDirectories = Directory.GetDirectories(_soundPacksFolderPath);
            // Use OfType<string>() to safely filter out any potential nulls from Path.GetFileName
            packs.AddRange(customPackDirectories.Select(Path.GetFileName).OfType<string>());
            return packs;
        }

        public string GetSoundPackDirectory(string packName)
        {
            if (packName == "Default")
            {
                return Path.Combine(AppContext.BaseDirectory, "Assets");
            }
            return Path.Combine(_soundPacksFolderPath, packName);
        }

        public async Task<string?> ImportSoundPackAsync(string sourceFolderPath)
        {
            // A valid pack must contain at least a key-press.wav
            if (!File.Exists(Path.Combine(sourceFolderPath, "key-press.wav")))
            {
                Debug.WriteLine("[ERROR] Import failed: Source folder does not contain a 'key-press.wav'.");
                return null;
            }

            var newPackName = new DirectoryInfo(sourceFolderPath).Name;
            var destinationPath = Path.Combine(_soundPacksFolderPath, newPackName);

            if (Directory.Exists(destinationPath))
            {
                // Handle potential conflicts, just append a number
                newPackName = $"{newPackName}_{DateTime.Now:yyyyMMddHHmmss}";
                destinationPath = Path.Combine(_soundPacksFolderPath, newPackName);
                
            }
            
            Directory.CreateDirectory(destinationPath);

            foreach (var file in Directory.GetFiles(sourceFolderPath, "*.wav"))
            {
                var destFile = Path.Combine(destinationPath, Path.GetFileName(file));
                await Task.Run(() => File.Copy(file, destFile, true));
            }

            return newPackName;
        }

        public SettingsModel LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    return JsonSerializer.Deserialize(json, SettingsJsonContext.Default.SettingsModel) ?? new SettingsModel();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Failed to load settings: {ex.Message}");
            }
            return new SettingsModel();
        }

        public void SaveSettings(SettingsModel settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings, SettingsJsonContext.Default.SettingsModel);
                File.WriteAllText(_settingsFilePath, json);

                // If the sound pack name changed, raise the event
                if (CurrentSettings.SoundPackName != settings.SoundPackName)
                {
                    SoundPackChanged?.Invoke(this, EventArgs.Empty);
                }
                CurrentSettings = settings;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Failed to save settings: {ex.Message}");
            }
        }
    }
}
