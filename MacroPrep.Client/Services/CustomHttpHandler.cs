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
            if (request.RequestUri!.AbsolutePath.Contains("/auth/"))
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
                    var authClient = _httpClientFactory.CreateClient("AuthClient");

                    var refreshResponse = await authClient.PostAsync("api/auth/refresh", null, cancellationToken);

                    if (refreshResponse.IsSuccessStatusCode)
                    {
                        var result = await refreshResponse.Content.ReadFromJsonAsync<LoginResponse>();

                        await _localStorage.SetItemAsync("authToken", result!.Token);

                        var newRequest = await CloneRequest(request);
                        newRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);

                        _isRefreshingToken = false; // Reset the flag before retrying the request
                        return await base.SendAsync(newRequest, cancellationToken);
                    }
                }
                catch (Exception)
                {
                    // Handle exceptions (e.g., network errors) if needed
                }

                // Redirect to login if the response is not 200 OK or if an exception occurs
                _isRefreshingToken = false;
                await _localStorage.RemoveItemAsync("authToken");
                _navigationManager.NavigateTo("/login");
            }

            return response;
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
