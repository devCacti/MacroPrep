using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;

namespace MacroPrep.Client.Services
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;
        private readonly AuthenticationState _anonymousState = new(new ClaimsPrincipal(new ClaimsIdentity()));
        private readonly CustomHttpHandler _customHttpHandler;
        private readonly NavigationManager Nav;

        public CustomAuthStateProvider(ILocalStorageService localStorage, NavigationManager navigationManager, CustomHttpHandler customHttpHandler)
        {
            _localStorage = localStorage;
            Nav = navigationManager;
            _customHttpHandler = customHttpHandler;

            Nav.LocationChanged += OnLocationChanged;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");

            if (string.IsNullOrWhiteSpace(token))
            {
                await _localStorage.RemoveItemAsync("authToken");
                return _anonymousState;
            }

            if (ShouldRefreshToken(token))
            {
                var refreshSuccess = await _customHttpHandler.RefreshTokenAsync();

                if (refreshSuccess)
                {
                    token = await _localStorage.GetItemAsync<string>("authToken");
                }
                else
                {
                    await _localStorage.RemoveItemAsync("authToken");
                    return _anonymousState;
                }
            }

            try
            {
                var claims = ParseClaimsFromJwt(token!);
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt")));
            }
            catch
            {
                await _localStorage.RemoveItemAsync("authToken");
                return _anonymousState;
            }
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public void NotifyUserLogin(string token)
        {
            var identity = new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt");
            var user = new ClaimsPrincipal(identity);

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        public void NotifyUserLogout()
        {
            var identity = new ClaimsIdentity();
            var user = new ClaimsPrincipal(identity);

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            return keyValuePairs!.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString()!));
        }

        private byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }

        private bool ShouldRefreshToken(string token)
        {
            try
            {
                var claims = ParseClaimsFromJwt(token);
                var expClaim = claims.FirstOrDefault(c => c.Type == "exp")?.Value;

                if (expClaim != null && long.TryParse(expClaim, out var exp))
                {
                    var expTime = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;

                    // If it expires in less than 2 minutes (or is already expired), refresh it
                    return expTime <= DateTime.UtcNow.AddMinutes(2);
                }
                return true; // If we can't read the expiration, assume we need a refresh
            }
            catch
            {
                return true;
            }
        }

        public async Task ForceLogoutAsync()
        {
            await _localStorage.RemoveItemAsync("authToken");
            NotifyAuthenticationStateChanged(Task.FromResult(_anonymousState));
        }
    }
}
