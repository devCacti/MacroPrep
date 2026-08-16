using MacroPrep.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacroPrep.Shared.Models.SemanticVersioning
{
    public class CreateSemanticVersionRequest
    {
        public required int Major { get; set; }
        public required int Minor { get; set; }
        public required int Patch { get; set; }

        // I could want to make a version invalid right at creation time
        public bool VersionIsValid { get; set; } = true;
        public string? Hash { get; set; } = null;

        // Which Project Component this version belongs to
        public SystemComponent Component { get; set; } = SystemComponent.Client;
    }
}
