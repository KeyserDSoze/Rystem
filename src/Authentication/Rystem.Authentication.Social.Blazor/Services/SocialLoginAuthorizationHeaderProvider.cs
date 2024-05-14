using System.Security.Claims;
using Microsoft.Identity.Abstractions;

namespace Rystem.Authentication.Social.Blazor
{
    public sealed class SocialLoginAuthorizationHeaderProvider : IAuthorizationHeaderProvider
    {
        private string? _token;
        internal void SetToken(string token)
        {
            _token = $"Bearer {token}";
        }
        public Task<string> CreateAuthorizationHeaderForAppAsync(string scopes, AuthorizationHeaderProviderOptions? downstreamApiOptions = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_token ?? string.Empty);
        }

        public Task<string> CreateAuthorizationHeaderForUserAsync(IEnumerable<string> scopes, AuthorizationHeaderProviderOptions? authorizationHeaderProviderOptions = null, ClaimsPrincipal? claimsPrincipal = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_token ?? string.Empty);
        }
    }
}
