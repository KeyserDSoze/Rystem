using Microsoft.Extensions.DependencyInjection;
using RepositoryFramework.Api.Client;

namespace RepositoryFramework
{
    public sealed class RepositoryApiBuilder<T, TKey> : IServiceOptions<ApiClientSettings<T, TKey>>
        where TKey : notnull
    {
        internal IServiceCollection Services { get; set; }
        private string? _version;
        private string? _name;
        private string? _factoryName;
        private string? _path;
        public RepositoryApiBuilder<T, TKey> WithVersion(string version)
        {
            _version = version;
            return this;
        }
        public RepositoryApiBuilder<T, TKey> WithName(string name)
        {
            _name = name;
            return this;
        }
        public RepositoryApiBuilder<T, TKey> WithStartingPath(string path)
        {
            _path = path;
            return this;
        }
        public RepositoryApiBuilder<T, TKey> WithServerFactoryName(string name)
        {
            _factoryName = name;
            return this;
        }
        public RepositoryHttpClientBuilder<T, TKey> WithHttpClient(string domain)
        {
            var httpClientService = Services.AddHttpClient($"{typeof(T).Name}{Const.HttpClientName}", options =>
            {
                options.BaseAddress = new Uri($"https://{domain}");
            });
            return new RepositoryHttpClientBuilder<T, TKey>(this, httpClientService);
        }
        public RepositoryHttpClientBuilder<T, TKey> WithHttpClient(Action<HttpClient> configuredClient)
        {
            var httpClientService = Services.AddHttpClient($"{typeof(T).Name}{Const.HttpClientName}", options =>
            {
                configuredClient?.Invoke(options);
            });
            return new RepositoryHttpClientBuilder<T, TKey>(this, httpClientService);
        }

        public Func<ApiClientSettings<T, TKey>> Build()
        {
            var settings = new ApiClientSettings<T, TKey>(_path, _version, _name, _factoryName);
            return () => settings;
        }
    }
}
