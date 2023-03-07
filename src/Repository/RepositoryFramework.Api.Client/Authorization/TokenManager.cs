using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web;
using RepositoryFramework.Api.Client.DefaultInterceptor;

namespace RepositoryFramework.Api.Client.Authorization
{
    internal sealed class TokenManager : ITokenManager
    {
        private readonly ITokenAcquisition _tokenProvider;
        private readonly AuthenticatorSettings _settings;
        private readonly AuthenticationStateProvider? _authenticationStateProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public TokenManager(
        ITokenAcquisition tokenProvider,
        AuthenticatorSettings settings,
        IHttpContextAccessor httpContextAccessor,
        AuthenticationStateProvider? authenticationStateProvider = null)
        {
            _tokenProvider = tokenProvider;
            _settings = settings;
            _authenticationStateProvider = authenticationStateProvider;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task EnrichWithAuthorizationAsync(HttpClient client)
        {
            var tokenResponse = await Try.WithDefaultOnCatchAsync(() => GetTokenAsync()).NoContext();
            if (tokenResponse != null)
                tokenResponse = await Try.WithDefaultOnCatchAsync(() => RefreshTokenAsync()).NoContext();
            if (tokenResponse?.Exception == null && tokenResponse?.Entity != null)
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResponse.Entity);
        }
        public async Task<string?> GetTokenAsync()
        {
            ClaimsPrincipal? authUser = null;
            if (_httpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated == true)
                authUser = _httpContextAccessor.HttpContext.User;
            else if (_authenticationStateProvider != null)
            {
                var authState = await _authenticationStateProvider.GetAuthenticationStateAsync().NoContext();
                authUser = authState.User;
            }
            if (authUser != null)
            {
                var token = await _tokenProvider.GetAccessTokenForUserAsync(_settings.Scopes!, user: authUser).NoContext();
                return token;
            }
            return null;
        }
        public async Task<string?> RefreshTokenAsync()
        {
            ClaimsPrincipal? authUser = null;
            if (_authenticationStateProvider != null)
            {
                var authState = await _authenticationStateProvider.GetAuthenticationStateAsync().NoContext();
                authUser = authState.User;
            }
            if (authUser != null)
            {
                var token = await _tokenProvider.GetAccessTokenForUserAsync(_settings.Scopes!, user: authUser).NoContext();
                return token;
            }
            return null;
        }
    }
}
