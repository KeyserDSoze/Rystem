using Microsoft.AspNetCore.Components;
using Rystem.Authentication.Social.Blazor;

namespace Rystem.Api.Client
{
    internal class SocialTokenManager : IRequestEnhancer
    {
        private readonly SocialLoginManager _socialLoginManager;
        private readonly NavigationManager? _navigationManager;

        public SocialTokenManager(SocialLoginManager socialLoginManager,
            NavigationManager? navigationManager)
        {
            _socialLoginManager = socialLoginManager;
            _navigationManager = navigationManager;
        }

        public async ValueTask EnhanceAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var tokenResponse = await Try.WithDefaultOnCatchAsync(() => GetTokenAsync()).NoContext();
            if (tokenResponse?.Exception == null && tokenResponse?.Entity != null)
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResponse);
            }
        }
        private Task<string?> GetTokenAsync()
            => RefreshTokenAsync();

        private async Task<string?> RefreshTokenAsync()
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
