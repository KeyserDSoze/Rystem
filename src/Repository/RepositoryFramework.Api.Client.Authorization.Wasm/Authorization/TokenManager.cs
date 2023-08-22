using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using RepositoryFramework.Api.Client.DefaultInterceptor;

namespace RepositoryFramework.Api.Client.Authorization
{
    internal sealed class TokenManager : ITokenManager
    {
        private readonly IAccessTokenProvider _tokenProvider;
        private readonly AuthenticatorSettings _settings;
        private AccessToken? _lastToken;
        public TokenManager(
        IAccessTokenProvider tokenProvider,
        AuthenticatorSettings settings)
        {
            _tokenProvider = tokenProvider;
            _settings = settings;
        }
        public async Task EnrichWithAuthorizationAsync(HttpClient client)
        {
            var tokenResponse = await Try.WithDefaultOnCatchAsync(() => GetTokenAsync()).NoContext();
            if (tokenResponse != null)
                tokenResponse = await Try.WithDefaultOnCatchAsync(() => RefreshTokenAsync()).NoContext();
            if (tokenResponse?.Exception == null && tokenResponse?.Entity != null)
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResponse.Entity);
        }
        public Task<string?> GetTokenAsync()
        {
            var now = DateTimeOffset.Now;
            if (_lastToken == null || now >= _lastToken.Expires.AddMinutes(-5))
            {
                return RefreshTokenAsync();
            }
            return Task.FromResult(_lastToken?.Value);
        }
        public async Task<string?> RefreshTokenAsync()
        {
            var tokenResult = _settings.Scopes != null && _settings.Scopes.Length > 0 ?
                    await _tokenProvider.RequestAccessToken(
                    new AccessTokenRequestOptions
                    {
                        Scopes = _settings.Scopes,
                    }) : await _tokenProvider.RequestAccessToken();

            if (tokenResult.TryGetToken(out var token))
            {
                _lastToken = token;
                return _lastToken.Value;
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
