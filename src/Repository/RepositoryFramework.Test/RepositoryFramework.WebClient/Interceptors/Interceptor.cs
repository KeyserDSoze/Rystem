using RepositoryFramework.Api.Client;
using RepositoryFramework.WebClient.Data;

namespace RepositoryFramework.WebClient.Interceptors
{
    public class Interceptor : IRepositoryClientInterceptor
    {
        public Task<HttpClient> EnrichAsync(HttpClient client, RepositoryMethods path)
            => Task.FromResult(client);
    }
    public class SpecificInterceptor : IRepositoryClientInterceptor<User>
    {
        public Task<HttpClient> EnrichAsync(HttpClient client, RepositoryMethods path)
            => Task.FromResult(client);
    }
}
