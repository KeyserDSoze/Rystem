using Microsoft.Identity.Web;
using RepositoryFramework.Api.Client.Authorization;
using System.Net.Http.Headers;

namespace RepositoryFramework.Api.Client.DefaultInterceptor
{
    internal class BearerAuthenticator : IRepositoryClientInterceptor
    {
        private readonly ITokenManager _tokenManager;
        private readonly AuthenticatorSettings _settings;
        private readonly IServiceProvider _provider;

        public BearerAuthenticator(ITokenManager tokenManager,
            AuthenticatorSettings settings,
            IServiceProvider provider)
        {
            _tokenManager = tokenManager;
            _settings = settings;
            _provider = provider;
        }
        public async Task<HttpClient> EnrichAsync(HttpClient client, RepositoryMethods path)
        {
            try
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                    await _tokenManager.GetTokenAsync());
            }
            catch (Exception exception)
            {
                if (_settings.ExceptionHandler != null)
                    await _settings.ExceptionHandler.Invoke(exception, _provider).NoContext();
            }
            return client;
        }
    }
}
