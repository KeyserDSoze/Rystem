using RepositoryFramework.Api.Client.Authorization;

namespace RepositoryFramework.Api.Client.DefaultInterceptor
{
    internal class BearerAuthenticator<T> : BearerAuthenticator, IRepositoryClientInterceptor<T>
    {
        public BearerAuthenticator(ITokenManager tokenManager,
            AuthenticatorSettings<T> settings,
            IServiceProvider provider) : base(tokenManager, settings, provider)
        {
        }
    }
}
