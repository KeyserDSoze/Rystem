﻿using Microsoft.AspNetCore.Authentication.BearerToken;
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

        public Task<TokenResponse?> CheckTokenAndGetUsernameAsync(string code, CancellationToken cancellationToken)
        {
            var ticket = _options.RefreshTokenProtector.Unprotect(code);
            var expiringTime = ticket?.Properties?.ExpiresUtc ?? DateTime.UtcNow.AddHours(1);
            var identity = ticket?.Principal?.Identity;
            if (identity?.IsAuthenticated == true && DateTime.UtcNow < expiringTime)
                return Task.FromResult(identity?.Name != null ? new TokenResponse
                {
                    Username = identity.Name,
                    Claims = ticket?.Principal?.Claims.ToList() ?? new()
                } : TokenResponse.Empty);
            else
                return Task.FromResult(TokenResponse.Empty);
        }
    }
}
