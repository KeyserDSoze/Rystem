using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.Identity.Abstractions;

namespace Rystem.Authentication.Social.Blazor
{
    public sealed class SocialLoginManager
    {
        private readonly SocialLoginAppSettings _settings;
        private readonly SocialLoginLocalStorageService _localStorage;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly NavigationManager _navigationManager;
        private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider;
        public SocialLoginManager(SocialLoginAppSettings settings,
            SocialLoginLocalStorageService localStorage,
            IHttpClientFactory httpClientFactory,
            NavigationManager navigationManager,
            IAuthorizationHeaderProvider authorizationHeaderProvider)
        {
            _settings = settings;
            _localStorage = localStorage;
            _httpClientFactory = httpClientFactory;
            _navigationManager = navigationManager;
            _authorizationHeaderProvider = authorizationHeaderProvider;
        }
        
        /// <summary>
        /// Get full redirect URI with smart detection
        /// 
        /// Logic:
        /// 1. If RedirectPath contains "://" → complete URI (mobile or custom)
        /// 2. If RedirectPath starts with "/" → relative path, prepend NavigationManager.BaseUri
        /// 3. If RedirectPath is empty → default to NavigationManager.BaseUri + "/account/login"
        /// </summary>
        public string GetFullRedirectUri()
        {
            var redirectPath = _settings.Platform?.RedirectPath;
            
            // If redirectPath is specified
            if (!string.IsNullOrWhiteSpace(redirectPath))
            {
                // If contains "://", it's a complete URI (mobile deep link or custom domain)
                if (redirectPath.Contains("://"))
                {
                    return redirectPath;
                }
                
                // If starts with "/", it's a relative path for web
                if (redirectPath.StartsWith('/'))
                {
                    return $"{_navigationManager.BaseUri.TrimEnd('/')}{redirectPath}";
                }
                
                // Fallback: assume it's a relative path without leading slash
                return $"{_navigationManager.BaseUri.TrimEnd('/')}/{redirectPath}";
            }
            
            // Default: current base URI + /account/login
            return $"{_navigationManager.BaseUri.TrimEnd('/')}/account/login";
        }
        
        /// <summary>
        /// [Deprecated] Use GetFullRedirectUri() instead
        /// Get redirect URI based on platform configuration
        /// </summary>
        [Obsolete("Use GetFullRedirectUri() instead")]
        public string GetRedirectUri()
        {
            return _navigationManager.BaseUri.TrimEnd('/');
        }
        
        public async Task<SocialUserWrapper<TUser>?> MeAsync<TUser>()
            where TUser : ISocialUser, new()
        {
            var token = await _localStorage.GetTokenAsync();
            if (token != null)
            {
                var client = _httpClientFactory.CreateClient(nameof(SocialLoginManager));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);
                var request = new HttpRequestMessage(HttpMethod.Get, "/api/Authentication/Social/User");
                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, default);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    try
                    {
                        client.DefaultRequestHeaders.Authorization = null;
                        token = await client.GetFromJsonAsync<Token>($"/api/Authentication/Social/Token?provider={SocialLoginProvider.DotNet}&code={token.RefreshToken}");
                        if (token is not null)
                        {
                            await _localStorage.SetTokenAsync(token);
                            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);
                            request = new HttpRequestMessage(HttpMethod.Get, "/api/Authentication/Social/User");
                            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, default);
                            response.EnsureSuccessStatusCode();
                        }
                    }
                    catch (Exception)
                    {
                        await LogoutAsync();
                        return default;
                    }
                }
                using var stream = await response.Content.ReadAsStreamAsync(default);
                var user = await stream.FromJsonAsync<TUser>();
                if (token is not null && user?.Username is not null)
                {
                    if (_authorizationHeaderProvider is SocialLoginAuthorizationHeaderProvider authorizationHeaderProvider)
                    {
                        authorizationHeaderProvider.SetToken(token.AccessToken);
                    }
                    return new SocialUserWrapper<TUser>
                    {
                        User = user,
                        CurrentToken = token.AccessToken
                    };
                }
            }
            return default;
        }
        private const string Origin = nameof(Origin);
        public async Task<Token?> FetchTokenAsync()
        {
            var token = await _localStorage.GetTokenAsync();
            if (token == null)
            {
                token = await CreateTokenAsync();
            }
            else if (DateTime.UtcNow > token.Expiring)
            {
                try
                {
                    var client = _httpClientFactory.CreateClient(nameof(SocialLoginManager));
                    client.DefaultRequestHeaders.Add(Origin, _navigationManager.BaseUri.Trim('/'));
                    token = await client.GetFromJsonAsync<Token>($"/api/Authentication/Social/Token?provider={SocialLoginProvider.DotNet}&code={token.RefreshToken}");
                    if (token is not null)
                    {
                        await _localStorage.SetTokenAsync(token);
                        return token;
                    }
                }
                catch
                {
                    await LogoutAsync();
                    token = await CreateTokenAsync();
                }
            }
            return token?.Expiring >= DateTime.UtcNow ? token : default;
        }
        private async Task<Token?> CreateTokenAsync()
        {
            var uri = new Uri(_navigationManager.Uri);
            var queryStrings = HttpUtility.ParseQueryString(uri.Query);
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
                        return default;
                    }
                }
                
                if (queryStrings.AllKeys.Any(x => x == "code"))
                {
                    code = queryStrings["code"];
                }
                
                if (string.IsNullOrWhiteSpace(code))
                    return default;
                
                var client = _httpClientFactory.CreateClient(nameof(SocialLoginManager));
                client.DefaultRequestHeaders.Add(Origin, _navigationManager.BaseUri.Trim('/'));
                
                // Retrieve code_verifier from localStorage (if exists - for PKCE)
                var codeVerifier = await _localStorage.GetItemAsync<string>("microsoft_code_verifier");
                
                // Extract redirectPath from current URI
                var currentUri = new Uri(_navigationManager.Uri);
                var redirectPath = currentUri.AbsolutePath;
                
                // Build query string with redirectPath
                var queryParams = $"/api/Authentication/Social/Token?provider={localState.Provider}&code={code}&redirectPath={Uri.EscapeDataString(redirectPath)}";
                
                HttpResponseMessage response;
                
                // If code_verifier exists, send it in the request body
                if (!string.IsNullOrWhiteSpace(codeVerifier))
                {
                    var additionalParams = new Dictionary<string, string>
                    {
                        { "code_verifier", codeVerifier }
                    };
                    
                    var content = JsonContent.Create(additionalParams);
                    response = await client.PostAsync(queryParams, content);
                    
                    // Cleanup: remove code_verifier from storage
                    await _localStorage.DeleteItemAsync("microsoft_code_verifier");
                    await _localStorage.DeleteItemAsync("microsoft_code_challenge");
                }
                else
                {
                    // Fallback: GET request without PKCE (backward compatibility)
                    response = await client.GetAsync(queryParams);
                }
                
                if (response.IsSuccessStatusCode)
                {
                    var token = await response.Content.ReadFromJsonAsync<Token>();
                    if (token is not null)
                    {
                        await _localStorage.SetTokenAsync(token);
                        _navigationManager.NavigateTo(localState.Path);
                        return token;
                    }
                }
                else
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        await LogoutAsync();
                    }
                }
            }
            
            return default;
        }
        public async ValueTask LogoutAsync()
        {
            await _localStorage.DeleteTokenAsync();
            await _localStorage.DeleteTokenAsync();
            if (_authorizationHeaderProvider is SocialLoginAuthorizationHeaderProvider authorizationHeaderProvider)
            {
                authorizationHeaderProvider.SetToken(string.Empty);
            }
        }
    }
}
