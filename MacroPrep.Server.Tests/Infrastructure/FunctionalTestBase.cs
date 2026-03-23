using Microsoft.Extensions.Configuration;
using Xunit;

namespace MacroPrep.Server.Tests.Infrastructure
{
    public abstract class FunctionalTestBase : IAsyncLifetime
    {
        protected AuthenticatedApiClient ApiClient { get; private set; }
        protected readonly IConfiguration _config;

        protected FunctionalTestBase()
        {
            // Build configuration to access appsettings.json
            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json")
                .Build();

            // Initialize the API client with the base URL from configuration
            var baseUrl = _config["ApiBaseUrl"] ?? throw new Exception("ApiBaseUrl not found in configuration.");
            ApiClient = new AuthenticatedApiClient(baseUrl);
        }

        public async Task InitializeAsync()
        {
            var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8); // Generate a short unique ID
            var testUserName = $"auto.test.{uniqueId}";
            var testEmail = $"auto.test.{uniqueId}@macroprep.local";
            var testPassword = _config["TestPassword"] ?? throw new Exception("TestPassword not found in configuration.");

            await ApiClient.AuthenticateAsync(testUserName, testEmail, testPassword);
        }

        public Task DisposeAsync()
        {
            ApiClient.Client.Dispose();
            return Task.CompletedTask;
        }
    }
}
