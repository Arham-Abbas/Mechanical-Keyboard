using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Mechanical_Keyboard.Models
{
    public class SettingsModel
    {
        public bool IsEnabled { get; set; } = true;
        public double Volume { get; set; } = 1.0;
        public string SoundPackName { get; set; } = "Default";
        public bool IsFirstRun { get; set; } = true;

        // Properties for pack restoration and version tracking
        public bool RestoreDeletedPacksOnUpdate { get; set; } = true;
        public string LastRunAppVersion { get; set; } = "0.0.0";
        public List<string> DeletedDefaultPacks { get; set; } = [];

        // Property for custom FFmpeg path
        public string? FFmpegPath { get; set; }
    }

    [JsonSerializable(typeof(SettingsModel))]
    [JsonSerializable(typeof(List<string>))]
    internal partial class SettingsJsonContext : JsonSerializerContext
    {
    }
}