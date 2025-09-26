using System.Text.Json.Serialization;

namespace Mechanical_Keyboard.Models
{
    public class SettingsModel
    {
        public bool IsEnabled { get; set; } = true;
        public double Volume { get; set; } = 1.0; // Range from 0.0 to 1.0
        public string SoundPackName { get; set; } = "Default";
        public string? SoundFilePath { get; set; }
    }

    /// <summary>
    ///   This class enables the JSON source generator for SettingsModel.
    ///   It improves performance and makes the app compatible with AOT compilation.
    /// </summary>
    [JsonSerializable(typeof(SettingsModel))]
    internal partial class SettingsJsonContext : JsonSerializerContext
    {
    }
}
