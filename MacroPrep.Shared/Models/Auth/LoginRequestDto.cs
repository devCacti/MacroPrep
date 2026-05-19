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

        // This is to allow developers to understand that they can use either username or email to login, but it will be mapped to the UserName property internally.
        [JsonIgnore]
        public virtual string UserNameOrEmail {  get => UserName; set => UserName = value; }

        [Required(ErrorMessage = "Password is required")]
        [PasswordPropertyText(true)]
        public string Password { get; set; } = string.Empty;
    }
}
