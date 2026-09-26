using Microsoft.AspNetCore.Components;
using MacroPrep.Shared.Models.SemanticVersioning;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;

namespace MacroPrep.Client.Services
{
    public class AppUpdateService
    {
        private readonly HttpClient _http;
        private readonly NavigationManager _navManager;
        private readonly IConfiguration _config;
        private readonly IJSRuntime _jsRuntime;

        public AppUpdateService(HttpClient http, NavigationManager navManager, IConfiguration config, IJSRuntime jsRuntime)
        {
            _http = http;
            _navManager = navManager;
            _config = config;
            _jsRuntime = jsRuntime;
        }

        public async Task ExecuteUpdateCheckAsync()
        {
            try
            {
                // 1. Get the hardcoded version of the currently executing files
                var currentClientVersion = _config["ClientVersion"];

                // 2. Ask the server for the latest active version
                var serverVersion = await _http.GetFromJsonAsync<SemanticVersionDto>("api/version/client");

                if (serverVersion != null)
                {
                    // 3. Compare the strings. If the server is ahead and forces a refresh:
                    if (serverVersion.Version != currentClientVersion && serverVersion.ForceRefresh)
                    {
                        // Cache-busting safety mechanism
                        // If the browser refuses to load the new files, this prevents an infinite reload loop
                        var lastReloadAttempt = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", "last_reload_version");

                        if (lastReloadAttempt != serverVersion.Version)
                        {
                            await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "last_reload_version", serverVersion.Version);
                            _navManager.NavigateTo(_navManager.Uri, forceLoad: true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update check bypassed: {ex.Message}");
            }
        }
    }
}