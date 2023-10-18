using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;

namespace Rystem.Api.Client
{
    internal sealed class TokenManager<T> : TokenManager
    {
        private static readonly string s_clientName = $"ApiHttpClient_{typeof(T).FullName}";
        private protected override string ClientNameForAll => s_clientName;
        public TokenManager(IFactory<AuthorizationSettings> settings,
            MicrosoftIdentityConsentAndConditionalAccessHandler consentHandler,
            IAuthorizationHeaderProvider authorizationHeaderProvider) :
            base(settings, consentHandler, authorizationHeaderProvider)
        { }
    }
}
