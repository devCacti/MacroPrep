using FluentAssertions;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Json;
using MacroPrep.Shared.Models.Auth;
using MacroPrep.Server.Tests.Infrastructure;
using Xunit;

namespace MacroPrep.Server.Tests.Functional.Auth
{
    // This is the only test that will not inherit from FunctionalTestBase,
    // because it needs to test the registration and login process itself,
    // which is required to get the token for the other tests
    public class AuthenticationTests
    {
        private readonly HttpClient _client;
        private readonly string _testPassword;

        public AuthenticationTests()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json")
                .Build();

            _client = new HttpClient { BaseAddress = new Uri(config["ApiBaseUrl"] ?? throw new Exception("ApiBaseUrl not found in configuration.")) };
            _testPassword = config["TestPassword"] ?? throw new Exception("TestPassword not found in configuration.");
        }

        // This test checks for Register success with valid data.
        [Fact]
        public async Task Register_ShouldReturnOk_WhenDataIsValid()
        {
            var uniqueId = Guid.NewGuid().ToString("N")[..8]; // Generate a short unique ID
            var regDto = new RegisterRequestDto
            {
                UserName = $"auto.test.{uniqueId}",
                Email = $"auto.test.{uniqueId}@macroprep.local",
                Password = _testPassword,
                ConfirmPassword = _testPassword
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register", regDto);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // This test checks for Login success with valid credentials.
        [Fact]
        public async Task Login_ShouldReturnOk_WhenDataIsValid()
        {
            // We will use AuthenticatedApiClient for this as it creates the account on its own given a username
            var authClient = new AuthenticatedApiClient(_client.BaseAddress!.ToString());

            var uniqueId = Guid.NewGuid().ToString("N")[..8]; // Generate a short unique ID

            var testUserName = $"auto.test.{uniqueId}";
            var testEmail = testUserName + "@macroprep.local";

            await authClient.AuthenticateAsync(testUserName, testEmail, _testPassword);
            Console.WriteLine($"Loging in user: {testUserName} with email: {testEmail}");

            // Now we will try to login with the same credentials to check if the login endpoint works as expected
            var loginDto = new LoginRequestDto
            {
                UserName = testUserName,
                Password = _testPassword
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
