using MacroPrep.Client;
using MacroPrep.Client.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add Authorization Core
builder.Services.AddAuthorizationCore();

// Register Blazored Local Storage for token management
builder.Services.AddBlazoredLocalStorage();

// Register the custom HTTP handler for automatic token refresh
builder.Services.AddTransient<CustomHttpHandler>();

// Configure named HttpClients for authentication and API calls
builder.Services.AddHttpClient("AuthClient", client =>
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

// The "ApiClient" will automatically include the JWT token in the Authorization header and handle token refresh
builder.Services.AddHttpClient("API", client =>
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<CustomHttpHandler>();

// Register a default HttpClient that uses the "API" configuration, so it can be injected directly into components and services
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("API"));

// Register the custom AuthenticationStateProvider
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

await builder.Build().RunAsync();
