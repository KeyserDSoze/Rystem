using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.Api.Client
{
    internal sealed class TokenManager<T> : TokenManager
    {
        private static readonly string s_clientName = $"ApiHttpClient_{typeof(T).FullName}";
        private protected override string ClientNameForAll => s_clientName;
        public TokenManager(
            IAccessTokenProvider tokenProvider,
            IFactory<AuthorizationSettings> settings) : base(tokenProvider, settings)
        { }
    }
}
