using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework
{
    public interface IRepositoryApiBuilder<T, TKey>
        where TKey : notnull
    {
        IServiceCollection Services { get; }
        IRepositoryApiBuilder<T, TKey> WithName(string name);
        IRepositoryApiBuilder<T, TKey> WithStartingPath(string path);
        IRepositoryApiBuilder<T, TKey> WithVersion(string version);
        IRepositoryHttpClientBuilder<T, TKey> WithHttpClient(Action<HttpClient> configuredClient);
        IRepositoryHttpClientBuilder<T, TKey> WithHttpClient(string domain);
    }
}
