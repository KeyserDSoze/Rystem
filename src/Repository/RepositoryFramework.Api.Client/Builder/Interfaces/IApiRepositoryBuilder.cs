namespace RepositoryFramework
{
    public interface IApiRepositoryBuilder<T, TKey>
        where TKey : notnull
    {
        IApiRepositoryBuilder<T, TKey> WithVersion(string version);
        IApiRepositoryBuilder<T, TKey> WithName(string name);
        IApiRepositoryBuilder<T, TKey> WithStartingPath(string path);
        IApiRepositoryBuilder<T, TKey> WithServerFactoryName(string name);
        HttpClientRepositoryBuilder<T, TKey> WithHttpClient(string domain);
        HttpClientRepositoryBuilder<T, TKey> WithHttpClient(Action<HttpClient> configuredClient);
    }
}
