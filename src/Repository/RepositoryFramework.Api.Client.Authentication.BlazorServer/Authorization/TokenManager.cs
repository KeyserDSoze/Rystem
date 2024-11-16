using Microsoft.Identity.Abstractions;
using RepositoryFramework.Api.Client.DefaultInterceptor;

namespace RepositoryFramework.Api.Client.Authorization
{
    internal sealed class TokenManager : ITokenManager
    {
        private readonly AuthenticatorSettings _settings;
        private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider;

        public TokenManager(
            AuthenticatorSettings settings,
            IAuthorizationHeaderProvider authorizationHeaderProvider)
        {
            _settings = settings;
            _authorizationHeaderProvider = authorizationHeaderProvider;
        }
        public async Task EnrichWithAuthorizationAsync(HttpClient client)
        {
            var tokenResponse = await Try.WithDefaultOnCatchAsync(() => GetTokenAsync()).NoContext();
            if (tokenResponse?.Exception == null && tokenResponse?.Entity != null)
            {
                var splitted = tokenResponse.Entity.Split(' ');
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(splitted[0], splitted[1]);
            }
        }
        public Task<string?> GetTokenAsync()
        {
            return RefreshTokenAsync();
        }
        public async Task<string?> RefreshTokenAsync()
        {
            try
            {
                var token = await _authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(_settings.Scopes!);
                return token;
            }
            catch
            {
                return default;
            }
        }
    }
}
