using MacroPrep.Shared.Models.Auth;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;

namespace MacroPrep.Client.Services
{
    public class CustomHttpHandler : DelegatingHandler
    {
        private readonly ILocalStorageService _localStorage;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly NavigationManager _navigationManager;
        private bool _isRefreshingToken = false; // Flag to prevent multiple simultaneous token refreshes

        public CustomHttpHandler(ILocalStorageService localStorage, IHttpClientFactory httpClientFactory, NavigationManager navigationManager)
        {
            _localStorage = localStorage;
            _httpClientFactory = httpClientFactory;
            _navigationManager = navigationManager;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath.ToLower();
            if (path.Contains("/login") || path.Contains("/register"))
                return await base.SendAsync(request, cancellationToken);

            var token = await _localStorage.GetItemAsync<string>("authToken");
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized && !_isRefreshingToken)
            {
                _isRefreshingToken = true;

                try
                {
                    var refreshSuccessful = await RefreshTokenAsync();

                    if (refreshSuccessful)
                    {
                        var newToken = await _localStorage.GetItemAsync<string>("authToken");
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);

                        _isRefreshingToken = false;
                        return await base.SendAsync(request, cancellationToken);
                    }
                }
                catch (Exception)
                {
                    // Handle exceptions (e.g., network errors) if needed
                    throw;
                }

                // Redirect to login if the response is not 200 OK or if an exception occurs
                _isRefreshingToken = false;
                await _localStorage.RemoveItemAsync("authToken");
                _navigationManager.NavigateTo("/auth/login", true);
            }

            return response;
        }

        private async Task<bool> RefreshTokenAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("AuthClient");

                var response = await client.PostAsync("api/auth/refresh", null);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

                    await _localStorage.SetItemAsync("authToken", result!.Token);

                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private async Task ForceLogout()
        {
            await _localStorage.RemoveItemAsync("authToken");
            _navigationManager.NavigateTo("/auth/login", true);
        }

        private async Task<HttpRequestMessage> CloneRequest(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);

            if (request.Content != null)
            {
                var ms = new MemoryStream();
                await request.Content.CopyToAsync(ms);
                ms.Position = 0;
                clone.Content = new StreamContent(ms);

                foreach (var header in request.Content.Headers)
                    clone.Content.Headers.Add(header.Key, header.Value);
            }

            foreach (var header in request.Headers)
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

            return clone;
        }
    }
}
