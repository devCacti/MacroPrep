using MacroPrep.Server.Tests.Infrastructure;

namespace MacroPrep.Server.Tests.Functional.Auth
{
    public class RefreshTokenTests : FunctionalTestBase
    {
        [Fact]
        public async Task RefreshToken_ShouldReturnOk_WhenTokenIsValid()
        {
            var uniqueId = Guid.NewGuid().ToString("N")[..8]; // Generate a short unique ID with a length of 8 characters
            var userId = $"auto.test.{uniqueId}";
            var userEmail = userId + "@macroprep.local";

            await ApiClient.AuthenticateAsync(userId, userEmail, _config["TestPassword"] ?? throw new Exception("TestPassword not found in configuration."));

            // After authenticating, it's time to refresh the token using the given cookie.
            var response = await ApiClient.Client.PostAsync("/api/auth/refresh", null);

            response.EnsureSuccessStatusCode();
        }
    }
}
