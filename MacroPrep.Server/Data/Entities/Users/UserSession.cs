using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MacroPrep.Server.Data.Entities
{
    public class UserSession
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        [Required]
        public DateTimeOffset ExpiresAt { get; set; } = DateTimeOffset.UtcNow.AddDays(7);

        public bool IsRevoked { get; set; } = false;

        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}
