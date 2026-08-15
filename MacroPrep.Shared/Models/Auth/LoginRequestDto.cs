using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MacroPrep.Shared.Interfaces;

namespace MacroPrep.Shared.Models.Auth
{
    public class LoginRequestDto : IAuthInterface
    {
        [Required(ErrorMessage = "Username or Email is required")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [PasswordPropertyText(true)]
        public string Password { get; set; } = string.Empty;
    }
}
