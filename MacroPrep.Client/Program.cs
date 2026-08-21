using MacroPrep.Client;
using MacroPrep.Client.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MacroPrep.Client.Services.Offline;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

string serverAddress;

if (builder.HostEnvironment.IsDevelopment())
{
    serverAddress = "https://localhost:7273/";
} else {
    // Base address comes from the hosting environment, meaning that if the app is deployed in "https://macroprep.devcacti.com", that will be the base address.
    serverAddress = builder.HostEnvironment.BaseAddress;
}

// Add Authorization Core
builder.Services.AddAuthorizationCore();

// Register Blazored Local Storage for token management
builder.Services.AddBlazoredLocalStorage();

// Register the custom HTTP handler for automatic token refresh
builder.Services.AddTransient<CustomHttpHandler>();

// Configure named HttpClients for authentication and API calls
builder.Services.AddHttpClient("AuthClient", client => 
    client.BaseAddress = new Uri(serverAddress))
    .AddHttpMessageHandler<CustomHttpHandler>();

// The "ApiClient" will automatically include the JWT token in the Authorization header and handle token refresh
builder.Services.AddHttpClient("API", client =>
    client.BaseAddress = new Uri(serverAddress))
    .AddHttpMessageHandler<CustomHttpHandler>();

builder.Services.AddScoped<ShoppingListsService>();
builder.Services.AddScoped<RealTimeSyncService>();
builder.Services.AddScoped<SyncService>();
builder.Services.AddScoped<VersionService>();

// Register a default HttpClient that uses the "API" configuration, so it can be injected directly into components and services
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("API"));

// Register the custom AuthenticationStateProvider
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped(sp => (CustomAuthStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());
await builder.Build().RunAsync();
