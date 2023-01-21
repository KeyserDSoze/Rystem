using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace RepositoryFramework
{
    internal sealed class HttpClientBuilder<T, TKey> : IRepositoryHttpClientBuilder<T, TKey>
        where TKey : notnull
    {
        public IRepositoryApiBuilder<T, TKey> ApiBuilder { get; }
        public IHttpClientBuilder ClientBuilder { get; }
        public HttpClientBuilder(IRepositoryApiBuilder<T, TKey> apiBuilder, IHttpClientBuilder clientBuilder)
        {
            ApiBuilder = apiBuilder;
            ClientBuilder = clientBuilder;
        }
        public IRepositoryHttpClientBuilder<T, TKey> WithDefaultRetryPolicy()
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
