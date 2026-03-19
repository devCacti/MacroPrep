using System.ComponentModel.DataAnnotations;
using MacroPrep.Shared.Enums.Account;

namespace MacroPrep.Server.Data.Entities
{
    // Subject to change as we add more features, but this is the core concept for the user entity
    public class User
    {
        [Key]
        public Guid Id { get; set; } = new Guid();

        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        // Password fields for secure storage
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string PasswordSalt { get; set; } = string.Empty;

        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = [];

        public bool IsVerified { get; set; } = false;
        public bool HasCompletedSetup { get; set; } = false; // This can be used to track if the user has completed any initial setup steps (e.g., onboarding, profile completion)

        public AccountType Type { get; set; } = AccountType.User;
        public AccountPlan Plan { get; set; } = AccountPlan.Free;

        public string? TimeZoneId { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
