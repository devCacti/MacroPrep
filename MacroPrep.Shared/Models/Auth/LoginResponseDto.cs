using System.Text.Json.Serialization;

namespace MacroPrep.Shared.Models.Auth
{
    public record LoginResponseDto { 
        [JsonPropertyName("token")] public string Token { get; set; } = string.Empty; 
    }
}
