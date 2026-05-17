using System.Text.Json.Serialization;

namespace MacroPrep.Shared.Models.Auth
{
    public record LoginResponse { [JsonPropertyName("token")] public string Token { get; set; } = string.Empty; }
}
