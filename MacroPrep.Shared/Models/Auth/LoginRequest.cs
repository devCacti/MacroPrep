using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MacroPrep.Shared.Models.Auth
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Username or Email is required")]
        public string UserNameOrEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [PasswordPropertyText(true)]
        public string Password { get; set; } = string.Empty;
    }
}
