using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.Api.Client
{
    internal class TokenManager : IRequestEnhancer
    {
        private protected virtual string ClientNameForAll => s_clientNameForAll;
        private static readonly string s_clientNameForAll = $"ApiHttpClient";
        private readonly IAccessTokenProvider _tokenProvider;
        private readonly AuthorizationSettings _settings;
        private AccessToken? _lastToken;
        public TokenManager(
        IAccessTokenProvider tokenProvider,
        IFactory<AuthorizationSettings> settings)
        {
            _tokenProvider = tokenProvider;
            _settings = settings.Create()!;
        }
        public async ValueTask EnhanceAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var tokenResponse = await Try.WithDefaultOnCatchAsync(() => GetTokenAsync()).NoContext();
            if (tokenResponse?.Exception == null && tokenResponse?.Entity != null)
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResponse.Entity);
        }
        private Task<string?> GetTokenAsync()
        {
            var now = DateTimeOffset.Now;
            if (_lastToken == null || now >= _lastToken.Expires.AddMinutes(-5))
            {
                return RefreshTokenAsync();
            }
            return Task.FromResult(_lastToken?.Value);
        }
        private async Task<string?> RefreshTokenAsync()
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
