using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;

namespace Rystem.Api.Client
{
    internal class TokenManager : IRequestEnhancer
    {
        private protected virtual string ClientNameForAll => s_clientNameForAll;
        private static readonly string s_clientNameForAll = $"ApiHttpClient";
        private readonly AuthorizationSettings _settings;
        private readonly MicrosoftIdentityConsentAndConditionalAccessHandler _consentHandler;
        private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider;

        public TokenManager(
            IFactory<AuthorizationSettings> settings,
            MicrosoftIdentityConsentAndConditionalAccessHandler consentHandler,
            IAuthorizationHeaderProvider authorizationHeaderProvider)
        {
            _settings = settings.Create(ClientNameForAll)!;
            _consentHandler = consentHandler;
            _authorizationHeaderProvider = authorizationHeaderProvider;
        }

        public async ValueTask EnhanceAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var tokenResponse = await Try.WithDefaultOnCatchAsync(() => GetTokenAsync(cancellationToken)).NoContext();
            if (tokenResponse?.Exception == null && tokenResponse?.Entity != null)
            {
                var splitted = tokenResponse.Entity.Split(' ');
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(splitted[0], splitted[1]);
            }
        }
        private Task<string?> GetTokenAsync(CancellationToken cancellationToken)
        {
            return RefreshTokenAsync(cancellationToken);
        }
        private async Task<string?> RefreshTokenAsync(CancellationToken cancellationToken)
        {
            try
            {
                var token = await _authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(_settings.Scopes!, cancellationToken: cancellationToken);
                return token;
            }
            catch (Exception ex)
            {
                _consentHandler.HandleException(ex);
            }
            return null;
        }
    }
}
