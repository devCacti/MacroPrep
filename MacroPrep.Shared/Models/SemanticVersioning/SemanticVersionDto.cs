using MacroPrep.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacroPrep.Shared.Models.SemanticVersioning
{
    public class SemanticVersionDto
    {
        public required Guid VersionID { get; set; }

        public required int Major { get; set; }
        public required int Minor { get; set; }
        public required int Patch { get; set; }

        // Hash of the version, GitHub Commit HASH
        public string? Hash { get; set; } = null;

        // Wether it causes the client to refresh or not
        public bool ActiveVersion { get; set; } = false;
        public bool ForceRefresh { get; set; } = false;

        public SystemComponent Component { get; set; } = SystemComponent.Client;

        public string Version => $"{Major}.{Minor}.{Patch}";

        public static SemanticVersionDto DefaultEntry => new SemanticVersionDto
        {
            VersionID = Guid.Empty,
            Major = 0,
            Minor = 0,
            Patch = 0,
            Hash = null,
            ActiveVersion = true,
            ForceRefresh = false,
            Component = SystemComponent.Client
        };
    }
}
