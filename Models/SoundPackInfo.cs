using System;
using System.IO;
using System.Text.Json.Serialization;

namespace Mechanical_Keyboard.Models
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(SoundPackInfo))]
    public partial class SoundPackInfoJsonContext : JsonSerializerContext { }

    public class SoundPackInfo : IEquatable<SoundPackInfo>
    {
        // These properties are loaded directly from pack.json
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
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
                var customImagePath = Path.Combine(PackDirectory, CoverImage);
                if (!string.IsNullOrEmpty(CoverImage) && File.Exists(customImagePath))
                {
                    return customImagePath;
                }
                // Fallback to a default placeholder image if no cover image is found.
                return "ms-appx:///Assets/Square44x44Logo.scale-200.png";
            }
        }

        // IEquatable implementation
        public bool Equals(SoundPackInfo? other)
        {
            if (other is null) return false;
            // Two packs are the same if they point to the same directory.
            return PackDirectory == other.PackDirectory;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as SoundPackInfo);
        }

        public override int GetHashCode()
        {
            return PackDirectory.GetHashCode();
        }
    }
}
