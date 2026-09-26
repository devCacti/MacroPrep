using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacroPrep.Shared.Models.SemanticVersioning
{
    public class SemanticVersionsPagesDto
    {
        public int VersionCount { get; set; }
        public int PageCount { get; set; }
        public List<SemanticVersionDto> Versions { get; set; } = [];
    }
}
