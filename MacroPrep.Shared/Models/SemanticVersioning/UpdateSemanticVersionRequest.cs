using MacroPrep.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacroPrep.Shared.Models.SemanticVersioning
{
    public class UpdateSemanticVersionRequest
    {
        public int? Major { get; set; }
        public int? Minor { get; set; }
        public int? Patch { get; set; }

        // Hash of the version, GitHub Commit HASH
        public string? Hash { get; set; } = null;

        public bool? SetValid { get; set; } = true;

        public SystemComponent Component { get; set; } = SystemComponent.Client;
    }
}
