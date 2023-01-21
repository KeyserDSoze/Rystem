using Microsoft.Extensions.DependencyInjection;
using RepositoryFramework.Api.Client;

namespace RepositoryFramework
{
    internal sealed class ApiBuilder<T, TKey> : IRepositoryApiBuilder<T, TKey>
        where TKey : notnull
    {
        public IServiceCollection Services { get; }
        public ApiBuilder(IServiceCollection services)
        {
            Services = services;
        }
        public IRepositoryApiBuilder<T, TKey> WithVersion(string version)
        {
            ApiClientSettings<T, TKey>.Instance.RefreshPath(version: version);
            return this;
        }
        public IRepositoryApiBuilder<T, TKey> WithName(string name)
        {
            ApiClientSettings<T, TKey>.Instance.RefreshPath(name: name);
            return this;
        }
        public IRepositoryApiBuilder<T, TKey> WithStartingPath(string path)
        {
            ApiClientSettings<T, TKey>.Instance.RefreshPath(startingPath: path);
            return this;
        }
        public IRepositoryHttpClientBuilder<T, TKey> WithHttpClient(string domain)
        {
            var httpClientService = Services.AddHttpClient($"{typeof(T).Name}{Const.HttpClientName}", options =>
            {
                options.BaseAddress = new Uri($"https://{domain}");
            });
            return new HttpClientBuilder<T, TKey>(this, httpClientService);
        }
        public IRepositoryHttpClientBuilder<T, TKey> WithHttpClient(Action<HttpClient> configuredClient)
        {
            var httpClientService = Services.AddHttpClient($"{typeof(T).Name}{Const.HttpClientName}", options =>
            {
                configuredClient?.Invoke(options);
            });
            return new HttpClientBuilder<T, TKey>(this, httpClientService);
        }
    }
}
