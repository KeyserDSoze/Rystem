using RepositoryFramework.Api.Client.Authorization;

namespace RepositoryFramework.Api.Client.DefaultInterceptor
{
    internal sealed class BearerAuthenticator<T, TKey> : BearerAuthenticator<T>, IRepositoryClientInterceptor<T, TKey>
    {
        public BearerAuthenticator(ITokenManager tokenManager,
            AuthenticatorSettings<T, TKey> settings,
            IServiceProvider provider) : base(tokenManager, settings, provider)
        {
        }
    }
}
