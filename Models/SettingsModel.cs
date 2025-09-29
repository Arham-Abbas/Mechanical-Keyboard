using System.Text.Json.Serialization;

namespace Mechanical_Keyboard.Models
{
    public class SettingsModel
    {
        public bool IsEnabled { get; set; } = true;
        public double Volume { get; set; } = 1.0;
        public string SoundPackName { get; set; } = "Default";
        public string? SoundFilePath { get; set; }
    }

    [JsonSerializable(typeof(SettingsModel))]
    internal partial class SettingsJsonContext : JsonSerializerContext
    {
    }
}