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

        public SystemComponent Component { get; set; } = SystemComponent.Client;

        public string Version => $"{Major}.{Minor}.{Patch}";
    }
}
