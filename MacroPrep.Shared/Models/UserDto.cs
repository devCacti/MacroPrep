using System.ComponentModel.DataAnnotations;

namespace MacroPrep.Shared.Models
{
    // User Data Transfer Object (DTO) for API responses and requests
    public class UserDto
    {
        public Guid Id { get; set; }

        [Required, StringLength(50), MinLength(3)]
        public string UserName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        //MeasurementSystem...  (To be implemented)
        //DietPreferences...    (To be implemented)

        // Validation fields
        public bool IsVerified { get; set; } = false;

        public string? TimeZoneId { get; set; }


        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
