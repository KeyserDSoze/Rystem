using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Identity.Abstractions;

namespace Rystem.Authentication.Social.Blazor
{
    public sealed class SocialLoginManager
    {
        private readonly SocialLoginAppSettings _settings;
        private readonly SocialLoginLocalStorageService _localStorage;
        private readonly NavigationManager _navigationManager;
        private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider;
        private readonly HttpClient _client;
        public SocialLoginManager(SocialLoginAppSettings settings,
            SocialLoginLocalStorageService localStorage,
            IHttpClientFactory httpClientFactory,
            NavigationManager navigationManager,
            IAuthorizationHeaderProvider authorizationHeaderProvider)
        {
            _settings = settings;
            _localStorage = localStorage;
            _navigationManager = navigationManager;
            _authorizationHeaderProvider = authorizationHeaderProvider;
            _client = httpClientFactory.CreateClient(nameof(SocialLoginManager));
        }
        public string GetRedirectUri()
            => _navigationManager.BaseUri.Trim('/');
        public async ValueTask<bool> MeAsync()
        {
            var token = await _localStorage.GetTokenAsync();
            if (token != null)
            {
                _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);
                var request = new HttpRequestMessage(HttpMethod.Get, "/api/Authentication/Social/User");
                var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, default);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    try
                    {
                        _client.DefaultRequestHeaders.Authorization = null;
                        token = await _client.GetFromJsonAsync<Token>($"/api/Authentication/Social/Token?provider={SocialLoginProvider.DotNet}&code={token.RefreshToken}");
                        if (token is not null)
                        {
                            await _localStorage.SetTokenAsync(token);
                            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);
                            request = new HttpRequestMessage(HttpMethod.Get, "/api/Authentication/Social/User");
                            response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, default);
                            response.EnsureSuccessStatusCode();
                        }
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
                using var stream = await response.Content.ReadAsStreamAsync(default);
                var user = await stream.FromJsonAsync<SocialUser>();
                if (token is not null && user?.Username is not null)
                {
                    if (_authorizationHeaderProvider is SocialLoginAuthorizationHeaderProvider authorizationHeaderProvider)
                    {
                        authorizationHeaderProvider.SetToken(token.AccessToken);
                        return true;
                    }
                    return true;
                }
            }
            return false;
        }
        public async ValueTask<bool> FetchTokenAsync()
        {
            var token = await _localStorage.GetTokenAsync();
            if (token == null)
            {
                var uri = new Uri(_navigationManager.Uri);
                var queryStrings = System.Web.HttpUtility.ParseQueryString(uri.Query);
                var code = string.Empty;
                var state = string.Empty;
                var localState = await _localStorage.GetStateAsync();
                if (localState != null)
                {
                    if (queryStrings.AllKeys.Any(x => x == "state"))
                    {
                        state = queryStrings["state"];
                    }
                    if (!string.IsNullOrWhiteSpace(state))
                    {
                        if (state != localState?.Value)
                        {
                            return false;
                        }
                    }
                    if (queryStrings.AllKeys.Any(x => x == "code"))
                    {
                        code = queryStrings["code"];
                    }
                    if (string.IsNullOrWhiteSpace(code))
                        return false;
                    try
                    {
                        token = await _client.GetFromJsonAsync<Token>($"/api/Authentication/Social/Token?provider={localState.Provider}&code={code}");
                        if (token is not null)
                        {
                            await _localStorage.SetTokenAsync(token);
                            _navigationManager.NavigateTo(localState.Path);
                            return true;
                        }
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
            }
            else if (token.Expiring < DateTime.UtcNow)
            {
                try
                {
                    token = await _client.GetFromJsonAsync<Token>($"/api/Authentication/Social/Token?provider={SocialLoginProvider.DotNet}&code={token.RefreshToken}");
                    if (token is not null)
                    {
                        await _localStorage.SetTokenAsync(token);
                        return true;
                    }
                }
                catch
                {
                }
            }
            return false;
        }
    }
}
