using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MacroPrep.Shared.Models.Auth;

namespace MacroPrep.Server.Tests.Infrastructure
{
    public class AuthenticatedApiClient
    {
        public HttpClient Client;
        public CookieContainer CookieContainer { get; } = new();
        public string? CurrentToken { get; private set; }
        public string? CurrentUserId { get; private set; }

        public AuthenticatedApiClient(string baseUrl)
        {
            // A cookie container is needed to store the refresh token cookie, which is HttpOnly.
            var handler = new HttpClientHandler
            {
                CookieContainer = CookieContainer,
                UseCookies = true
            };

            Client = new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };
        }

        public async Task AuthenticateAsync(string userName, string email, string password)
        {
            Console.WriteLine($"Authenticating user: {userName} with email: {email}");
            // First, register the user
            var regDto = new RegisterRequestDto
            {
                UserName = userName,
                Email = email,
                Password = password,
                ConfirmPassword = password
            };

            // Send the registration request and get the token from the response
            var regResponse = await Client.PostAsJsonAsync("/api/auth/register", regDto);

            if (!regResponse.IsSuccessStatusCode && regResponse.StatusCode != System.Net.HttpStatusCode.Conflict)
            {
                // If there was an error besides a conflict, there is a problem with the registration process
                throw new Exception($"Registration failed: {regResponse.StatusCode}");
            }
            else if (regResponse.IsSuccessStatusCode)
            {
                // Get the token from the registration response
                var regResult = await regResponse.Content.ReadFromJsonAsync<LoginResponseDto>();

                if (regResult == null || string.IsNullOrEmpty(regResult.Token))
                {
                    throw new Exception("Registration succeeded but no token was returned.");
                }

                // Since the userId is returned inside the token (as NameIdentifier claim), we decode the token, just like the client does
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(regResult.Token);

                if (jwtToken == null)
                {
                    throw new Exception("Failed to decode JWT token.");
                }

                // Extract the user ID from the NameIdentifier claim
                var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    throw new Exception("User ID claim not found in token.");
                }

                // Save both the token and the user ID for later use
                CurrentToken = regResult.Token;
                CurrentUserId = userIdClaim.Value;
            }
            else if (regResponse.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                // If the user already exists, we can try to log in instead
                var loginDto = new LoginRequestDto
                {
                    UserNameOrEmail = userName,
                    Password = password
                };

                // Do a login request
                var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginDto);

                if (!loginResponse.IsSuccessStatusCode)
                {
                    throw new Exception($"Login failed: {loginResponse.StatusCode}");
                }

                var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();

                if (loginResult == null || string.IsNullOrEmpty(loginResult.Token))
                {
                    throw new Exception("Login succeeded but no token was returned.");
                }

                // Decode the token to extract the user ID
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(loginResult.Token);

                if (jwtToken == null)
                {
                    throw new Exception("Failed to decode JWT token.");
                }

                // Extract the user ID from the NameIdentifier claim
                var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    throw new Exception("User ID claim not found in token.");
                }

                CurrentToken = loginResult.Token;
                CurrentUserId = userIdClaim.Value;
            }

            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CurrentToken);
        }

        // Helper methods to make authenticated requests
        public Task<HttpResponseMessage> GetAsync(string uri) => Client.GetAsync(uri);
        public Task<HttpResponseMessage> PostAsync<T>(string uri, T content) => Client.PostAsJsonAsync(uri, content);
        public Task<HttpResponseMessage> PutAsync<T>(string uri, T content) => Client.PutAsJsonAsync(uri, content);
        public Task<HttpResponseMessage> DeleteAsync(string uri) => Client.DeleteAsync(uri);
    }
}
