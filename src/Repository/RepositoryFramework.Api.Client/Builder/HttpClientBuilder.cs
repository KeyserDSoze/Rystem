using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace RepositoryFramework
{
    public sealed class RepositoryHttpClientBuilder<T, TKey>
        where TKey : notnull
    {
        public RepositoryApiBuilder<T, TKey> ApiBuilder { get; }
        public IHttpClientBuilder ClientBuilder { get; }
        public RepositoryHttpClientBuilder(RepositoryApiBuilder<T, TKey> apiBuilder, IHttpClientBuilder clientBuilder)
        {
            ApiBuilder = apiBuilder;
            ClientBuilder = clientBuilder;
        }
        public RepositoryHttpClientBuilder<T, TKey> WithDefaultRetryPolicy()
        {
            var defaultPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrTransientHttpError()
                .AdvancedCircuitBreakerAsync(0.5, TimeSpan.FromSeconds(10), 10, TimeSpan.FromSeconds(15));
            ClientBuilder
                .AddPolicyHandler(defaultPolicy);
            return this;
        }
    }
}
