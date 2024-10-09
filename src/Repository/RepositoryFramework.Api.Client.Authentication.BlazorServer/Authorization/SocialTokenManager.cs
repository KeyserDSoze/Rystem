using Microsoft.AspNetCore.Components;
using RepositoryFramework.Api.Client.DefaultInterceptor;
using Rystem.Authentication.Social.Blazor;

namespace RepositoryFramework.Api.Client.Authorization
{
    internal sealed class SocialTokenManager : ITokenManager
    {
        private readonly SocialLoginManager _socialLoginManager;
        private readonly NavigationManager? _navigationManager;

        public SocialTokenManager(SocialLoginManager socialLoginManager,
            NavigationManager? navigationManager)
        {
            _socialLoginManager = socialLoginManager;
            _navigationManager = navigationManager;
        }
        public async Task EnrichWithAuthorizationAsync(HttpClient client)
        {
            var tokenResponse = await Try.WithDefaultOnCatchAsync(() => GetTokenAsync()).NoContext();
            if (tokenResponse?.Exception == null && tokenResponse?.Entity != null)
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResponse);
            }
        }
        public Task<string?> GetTokenAsync()
            => RefreshTokenAsync();

        public async Task<string?> RefreshTokenAsync()
        {
            var token = await _socialLoginManager.FetchTokenAsync();
            if (token != default)
            {
                return token.AccessToken;
            }
            else
            {
                await _socialLoginManager.LogoutAsync();
                if (_navigationManager != null)
                    _navigationManager.Refresh(true);
                return null;
            }
        }
    }
}
