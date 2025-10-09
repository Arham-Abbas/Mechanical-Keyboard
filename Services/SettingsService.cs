using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Mechanical_Keyboard.Exceptions;
using Mechanical_Keyboard.Helpers;
using Mechanical_Keyboard.Models;
using Microsoft.UI.Xaml.Controls;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace Mechanical_Keyboard.Services
{
    public class SettingsService
    {
        private readonly string _settingsFilePath;
        private readonly string _soundPacksFolderPath;
        private readonly string _ffmpegFolderPath;
        public SettingsModel CurrentSettings { get; private set; }

        public SettingsService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolderPath = Path.Combine(appDataPath, "Mechanical Keyboard");
            _soundPacksFolderPath = Path.Combine(appFolderPath, "SoundPacks");
            _ffmpegFolderPath = Path.Combine(appFolderPath, "FFmpeg");
            Directory.CreateDirectory(_soundPacksFolderPath);
            Directory.CreateDirectory(_ffmpegFolderPath);

            _settingsFilePath = Path.Combine(appFolderPath, "settings.json");
            CurrentSettings = LoadSettings();

            ConfigureFFmpeg();
        }

        private void ConfigureFFmpeg()
        {
            // Always point FFmpeg to our local app data folder.
            FFmpeg.SetExecutablesPath(_ffmpegFolderPath);
        }

        private static string GetBaseNameForAssignment(KeyAssignmentType assignment)
        {
            return assignment switch
            {
                KeyAssignmentType.KeyPress => "key-press",
                KeyAssignmentType.SpacePress => "space-press",
                KeyAssignmentType.EnterPress => "enter-press",
                KeyAssignmentType.BackspacePress => "backspace-press",
                KeyAssignmentType.ModifierPress => "modifier-press",
                _ => "key-press" // Default fallback for any other assignments
            };
        }

        public async Task CreateSoundPackAsync(SoundPackCreationModel model, bool overwrite = false)
        {
            var packDirectory = Path.Combine(_soundPacksFolderPath, model.PackName);
            if (Directory.Exists(packDirectory))
            {
                if (!overwrite)
                {
                    throw new PackExistsException(model.PackName);
                }
                Directory.Delete(packDirectory, true);
            }
            Directory.CreateDirectory(packDirectory);

            // Create a list of all file processing tasks to run them in parallel.
            var processingTasks = new List<Task>();

            // Add all audio file processing tasks.
            foreach (var file in model.FilesToImport)
            {
                var baseName = GetBaseNameForAssignment(file.Assignment);
                processingTasks.Add(ProcessAudioFileAsync(file.FilePath, packDirectory, baseName));
            }

            // Add the icon processing task if an icon was provided.
            var iconFileName = "cover.png";
            if (!string.IsNullOrEmpty(model.IconSourcePath))
            {
                processingTasks.Add(ProcessImageFileAsync(model.IconSourcePath, packDirectory, "cover"));
            }

            // Await all tasks to complete concurrently. This ensures atomicity.
            await Task.WhenAll(processingTasks);

            // Now that all files are guaranteed to be processed, create the metadata file.
            var packInfo = new SoundPackInfo
            {
                DisplayName = model.PackName,
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description,
                CoverImage = string.IsNullOrEmpty(model.IconSourcePath) ? string.Empty : iconFileName,
                HasPitchVariants = model.GeneratePitchVariants
            };

            var metadataPath = Path.Combine(packDirectory, "pack.json");
            var json = JsonSerializer.Serialize(packInfo, SoundPackInfoJsonContext.Default.SoundPackInfo);
            await File.WriteAllTextAsync(metadataPath, json);
        }

        private async Task<string> ProcessAudioFileAsync(string sourcePath, string destinationFolder, string baseName)
        {
            var finalDestinationPath = Path.Combine(destinationFolder, $"{baseName}.wav");
            var tempOutputPath = Path.ChangeExtension(Path.GetTempFileName(), ".wav");

            try
            {
                // Check if FFmpeg exists, and if not, ask the user before downloading.
                if (!File.Exists(Path.Combine(_ffmpegFolderPath, "ffmpeg.exe")))
                {
                    var result = await DialogHelper.ShowFFmpegDownloadConfirmationDialogAsync();
                    if (result != ContentDialogResult.Primary)
                    {
                        throw new OperationCanceledException("User cancelled FFmpeg download.");
                    }

                    DialogHelper.ShowProgressDialog("Downloading FFmpeg", "Please wait while the required audio components are downloaded...");

                    try
                    {
                        await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, _ffmpegFolderPath);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ERROR] FFmpeg download failed: {ex.Message}");
                        throw new InvalidOperationException("Failed to download FFmpeg. Please check your internet connection and try again.", ex);
                    }
                    finally
                    {
                        DialogHelper.HideProgressDialog();
                    }
                }

                var mediaInfo = await FFmpeg.GetMediaInfo(sourcePath);
                var audioStream = mediaInfo.AudioStreams.FirstOrDefault() ?? throw new InvalidDataException("The provided file does not contain a valid audio stream.");

                // Standardize to single-channel (mono), 16-bit WAV at a reasonable sample rate.
                audioStream
                    .SetChannels(1)
                    .SetSampleRate(44100);

                await FFmpeg.Conversions.New()
                    .AddStream(audioStream)
                    .SetOutput(tempOutputPath) // Convert to the temporary file
                    .Start();

                // Conversion successful, now move the file to its final destination.
                File.Move(tempOutputPath, finalDestinationPath, overwrite: true);

                return finalDestinationPath;
            }
            finally
            {
                // Ensure the temporary file is always cleaned up.
                if (File.Exists(tempOutputPath))
                {
                    File.Delete(tempOutputPath);
                }
            }
        }

        private static async Task<string> ProcessImageFileAsync(string sourcePath, string destinationFolder, string baseName)
        {
            var destinationPath = Path.Combine(destinationFolder, $"{baseName}.png");

            using var image = await SixLabors.ImageSharp.Image.LoadAsync(sourcePath);

            // Resize to a standard 128x128 square, preserving aspect ratio with padding.
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(128, 128),
                Mode = ResizeMode.Pad
            }));

            await image.SaveAsPngAsync(destinationPath);

            return destinationPath;
        }

        public List<SoundPackInfo> GetAvailableSoundPacks()
        {
            var soundPacks = new List<SoundPackInfo>();
            var defaultPacksPath = Path.Combine(AppContext.BaseDirectory, "Assets", "SoundPacks");

            // 1. Load custom packs from the writable AppData folder.
            foreach (var dir in Directory.GetDirectories(_soundPacksFolderPath))
            {
                var pack = LoadPackInfo(dir, isDefault: false);
                if (pack != null)
                {
                    soundPacks.Add(pack);
                }
            }

            // 2. Load default packs from the read-only installation folder.
            if (Directory.Exists(defaultPacksPath))
            {
                foreach (var dir in Directory.GetDirectories(defaultPacksPath))
                {
                    var pack = LoadPackInfo(dir, isDefault: true);
                    // Only add default packs if the user has NOT "deleted" it.
                    if (pack != null && !CurrentSettings.DeletedDefaultPacks.Contains(pack.DisplayName))
                    {
                        soundPacks.Add(pack);
                    }
                }
            }

            return soundPacks;
        }

        private static SoundPackInfo? LoadPackInfo(string directoryPath, bool isDefault)
        {
            var metadataPath = Path.Combine(directoryPath, "pack.json");
            if (!File.Exists(metadataPath)) return null;

            try
            {
                var json = File.ReadAllText(metadataPath);
                var packInfo = JsonSerializer.Deserialize(json, SoundPackInfoJsonContext.Default.SoundPackInfo);
                if (packInfo != null)
                {
                    packInfo.PackDirectory = directoryPath;
                    packInfo.IsDefault = isDefault;
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

        public async Task DeleteSoundPackAsync(SoundPackInfo packToDelete)
        {
            if (packToDelete.IsDefault)
            {
                // For a default pack, don't delete files. Just add its name to the "deleted" list.
                if (!CurrentSettings.DeletedDefaultPacks.Contains(packToDelete.DisplayName))
                {
                    CurrentSettings.DeletedDefaultPacks.Add(packToDelete.DisplayName);
                    SaveSettings(CurrentSettings);
                }
            }
            else
            {
                // For a custom pack, delete the directory from AppData.
                if (Directory.Exists(packToDelete.PackDirectory))
                {
                    await Task.Run(() => Directory.Delete(packToDelete.PackDirectory, true));
                }
            }
        }

        public void RestoreDefaultPacks()
        {
            CurrentSettings.DeletedDefaultPacks.Clear();
            SaveSettings(CurrentSettings);
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
                CurrentSettings = settings;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Failed to save settings: {ex.Message}");
            }
        }
    }
}
