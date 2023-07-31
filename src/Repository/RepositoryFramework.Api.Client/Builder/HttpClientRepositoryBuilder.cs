using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace RepositoryFramework
{
    public sealed class HttpClientRepositoryBuilder<T, TKey>
        where TKey : notnull
    {
        public IApiRepositoryBuilder<T, TKey> ApiBuilder { get; }
        public IHttpClientBuilder ClientBuilder { get; }
        public HttpClientRepositoryBuilder(IApiRepositoryBuilder<T, TKey> apiBuilder, IHttpClientBuilder clientBuilder)
        {
            ApiBuilder = apiBuilder;
            ClientBuilder = clientBuilder;
        }
        public HttpClientRepositoryBuilder<T, TKey> WithDefaultRetryPolicy()
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
