using System.Net;
using RepositoryFramework.Api.Client.Authorization;

namespace RepositoryFramework.Api.Client.DefaultInterceptor
{
    internal class BearerAuthenticator : IRepositoryClientInterceptor, IRepositoryResponseClientInterceptor
    {
        private const string Scheme = "Bearer";
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

        public async Task<HttpResponseMessage> CheckResponseAsync(HttpClient client, HttpResponseMessage response, Func<HttpClient, Task<HttpResponseMessage>> request)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var token = await _tokenManager.RefreshTokenAsync();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(Scheme, token);
                var newResponse = await request(client);
                return newResponse;
            }
            return response;
        }

        public async Task<HttpClient> EnrichAsync(HttpClient client, RepositoryMethods path)
        {
            try
            {
                await _tokenManager.EnrichWithAuthorizationAsync(client).NoContext();
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
