using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Mechanical_Keyboard.Models;

namespace Mechanical_Keyboard.Services
{
    public class SettingsService
    {
        private readonly string _settingsFilePath;
        private readonly string _customSoundPacksFolderPath;
        public SettingsModel CurrentSettings { get; private set; }

        public event EventHandler? SoundPackChanged;

        public SettingsService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolderPath = Path.Combine(appDataPath, "Mechanical Keyboard");
            _customSoundPacksFolderPath = Path.Combine(appFolderPath, "SoundPacks");
            Directory.CreateDirectory(_customSoundPacksFolderPath);

            _settingsFilePath = Path.Combine(appFolderPath, "settings.json");
            CurrentSettings = LoadSettings();
        }

        public List<SoundPackInfo> GetAvailableSoundPacks()
        {
            var soundPacks = new List<SoundPackInfo>();

            // 1. Scan for default packs in the installation directory
            var defaultPacksPath = Path.Combine(AppContext.BaseDirectory, "Assets", "SoundPacks");
            if (Directory.Exists(defaultPacksPath))
            {
                foreach (var dir in Directory.GetDirectories(defaultPacksPath))
                {
                    var pack = LoadPackInfo(dir);
                    if (pack != null) soundPacks.Add(pack);
                }
            }

            // 2. Scan for custom packs in the AppData directory
            foreach (var dir in Directory.GetDirectories(_customSoundPacksFolderPath))
            {
                var pack = LoadPackInfo(dir);
                if (pack != null) soundPacks.Add(pack);
            }

            return soundPacks;
        }

        private static SoundPackInfo? LoadPackInfo(string directoryPath)
        {
            var metadataPath = Path.Combine(directoryPath, "pack.json");
            if (!File.Exists(metadataPath)) return null;

            try
            {
                var json = File.ReadAllText(metadataPath);
                var packInfo = JsonSerializer.Deserialize(json, SoundPackInfoJsonContext.Default.SoundPackInfo);
                if (packInfo != null)
                {
                    // Set the directory path on the model
                    packInfo.PackDirectory = directoryPath;

                    if (string.IsNullOrWhiteSpace(packInfo.DisplayName))
                    {
                        packInfo.DisplayName = new DirectoryInfo(directoryPath).Name;
                    }
                }
                return packInfo;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Failed to load pack info from '{metadataPath}': {ex.Message}");
                return null;
            }
        }

        public string GetSoundPackDirectory(string packName)
        {
            // Check default packs first
            var defaultPackPath = Path.Combine(AppContext.BaseDirectory, "Assets", "SoundPacks", packName);
            if (Directory.Exists(defaultPackPath))
            {
                return defaultPackPath;
            }

            // Then check custom packs
            var customPackPath = Path.Combine(_customSoundPacksFolderPath, packName);
            if (Directory.Exists(customPackPath))
            {
                return customPackPath;
            }

            // Fallback to the default directory if the named pack isn't found
            return Path.Combine(AppContext.BaseDirectory, "Assets", "SoundPacks", "Default");
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
            var destinationPath = Path.Combine(_customSoundPacksFolderPath, newPackName);

            if (Directory.Exists(destinationPath))
            {
                // Handle potential conflicts, just append a number
                newPackName = $"{newPackName}_{DateTime.Now:yyyyMMddHHmmss}";
                destinationPath = Path.Combine(_customSoundPacksFolderPath, newPackName);
                
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
