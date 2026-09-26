using MacroPrep.Shared.Models.SemanticVersioning;

namespace MacroPrep.Shared.Models.SemanticVersioning
{
    public class VersionCheckResponseDto
    {
        public SemanticVersionDto? CurrentVersion { get; set; }
        public int VersionsBehind { get; set; }
        public bool NeedsUpdate { get; set; }
    }
}