using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MacroPrep.Shared.Models.Auth
{
    public class AccountSetupRequest
    {
        [Required(ErrorMessage = "First Name is required")]
        [MinLength(2, ErrorMessage = "First Name must be at least 2 characters long")]
        [MaxLength(50, ErrorMessage = "First Name cannot be longer than 50 characters")]
        public string FirstName { get; set; } = string.Empty;

        private string? _lastName;

        [MinLength(2, ErrorMessage = "Last Name must be at least 2 characters long")]
        [MaxLength(50, ErrorMessage = "Last Name cannot be longer than 50 characters")]
        public string? LastName
        {
            get => _lastName;
            set => _lastName = string.IsNullOrWhiteSpace(value) ? null : value;
        }

        [DataType(DataType.Date, ErrorMessage = "Invalid date format")]
        public DateOnly? DateOfBirth { get; set; }


        // For now this will not be an option, it will be added in the future
        //public string TimeZoneId { get; set; } = string.Empty;
    }
}
