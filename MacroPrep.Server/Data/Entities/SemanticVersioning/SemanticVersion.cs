using MacroPrep.Shared.Enums;
using MacroPrep.Shared.Interfaces;
using Microsoft.AspNetCore.Antiforgery;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MacroPrep.Server.Data.Entities.SemanticVersioning
{
    public class SemanticVersion : IAuditable
    {
        [Key]
        public Guid VersionID { get; set; } = Guid.NewGuid();

        public required int Major { get; set; }
        public required int Minor { get; set; }
        public required int Patch { get; set; }

        // Hash of the version, GitHub Commit HASH
        public string? Hash { get; set; } = null;

        public bool VersionIsValid { get; set; } = true;

        public SystemComponent Component { get; set; } = SystemComponent.Client;

        public string Version => $"{Major}.{Minor}.{Patch}";

        public Guid? CreatedByUserID { get; set; }
        public Guid? UpdatedByUserID { get; set; }

        [ForeignKey("CreatedByUserID")]
        public virtual User CreatedBy { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UpdatedByUserID")]
        public virtual User UpdatedBy { get; set; } = null!;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
