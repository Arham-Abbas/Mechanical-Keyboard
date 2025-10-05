using System.Collections.Generic;

namespace Mechanical_Keyboard.Models
{
    public class SoundFileToImport
    {
        public required string FilePath { get; set; }
        public KeyAssignmentType Assignment { get; set; }
    }

    public class SoundPackCreationModel
    {
        public required string PackName { get; set; }
        public string? Description { get; set; }
        public string? IconSourcePath { get; set; }
        public bool GeneratePitchVariants { get; set; }
        public required List<SoundFileToImport> FilesToImport { get; set; }
    }
}
