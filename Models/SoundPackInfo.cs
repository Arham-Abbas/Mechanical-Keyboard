using System.IO;
using System.Text.Json.Serialization;

namespace Mechanical_Keyboard.Models
{
    public class SoundPackInfo
    {
        // These properties are loaded directly from pack.json
        public string DisplayName { get; set; } = "Unknown Pack";
        public string Description { get; set; } = string.Empty;
        public string CoverImage { get; set; } = string.Empty;
        public bool HasPitchVariants { get; set; } = true;

        // This property is not part of the JSON, it's set after loading.
        [JsonIgnore]
        public string PackDirectory { get; set; } = string.Empty;

        // Property to distinguish default packs from user-imported ones.
        [JsonIgnore]
        public bool IsDefault { get; set; }

        // This is the smart property the UI will bind to.
        [JsonIgnore]
        public string ResolvedCoverImagePath
        {
            get
            {
                if (!string.IsNullOrEmpty(CoverImage) && !string.IsNullOrEmpty(PackDirectory))
                {
                    var customImagePath = Path.Combine(PackDirectory, CoverImage);
                    if (File.Exists(customImagePath))
                    {
                        return customImagePath;
                    }
                }
                // Fallback to the default app icon if no custom image is found.
                return "ms-appx:///Assets/Square44x44Logo.scale-200.png";
            }
        }
    }

    [JsonSerializable(typeof(SoundPackInfo))]
    internal partial class SoundPackInfoJsonContext : JsonSerializerContext
    {
    }
}
