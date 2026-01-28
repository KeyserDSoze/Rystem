using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.Extensions.Options;

namespace Rystem.Authentication.Social
{
    internal sealed class DotNetTokenChecker : ITokenChecker
    {
        private readonly BearerTokenOptions _options;
        public DotNetTokenChecker(IOptionsMonitor<BearerTokenOptions> options)
        {
            _options = options.Get(BearerTokenDefaults.AuthenticationScheme);
        }

        public Task<AnyOf<TokenResponse?, string>> CheckTokenAndGetUsernameAsync(string code, TokenCheckerSettings settings, CancellationToken cancellationToken = default)
        {
            var ticket = _options.RefreshTokenProtector.Unprotect(code);
            var expiringTime = ticket?.Properties?.ExpiresUtc ?? DateTime.UtcNow.AddHours(1);
            var identity = ticket?.Principal?.Identity;
            if (identity?.IsAuthenticated == true && DateTime.UtcNow < expiringTime)
            {
                if (settings.Domain == null || ticket?.Principal?.Claims?.Any(x => x.Type == SocialClaimTypes.Domain && x.Value == settings.Domain) == true)
                {
                    var token = identity?.Name != null ? new TokenResponse
                    {
                        Username = identity.Name,
                        Claims = ticket?.Principal?.Claims.ToList() ?? []
                    } : TokenResponse.Empty;
                    return Task.FromResult(new AnyOf<TokenResponse?, string>(token));
                }
            }
            return Task.FromResult(new AnyOf<TokenResponse?, string>(TokenResponse.Empty));
        }
    }
}
