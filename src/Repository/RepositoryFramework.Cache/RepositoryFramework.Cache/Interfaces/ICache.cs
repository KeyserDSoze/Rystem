namespace RepositoryFramework.Cache
{
    public interface ICache<T, TKey>
        where TKey : notnull
    {
        Task<CacheResponse<TValue>> RetrieveAsync<TValue>(string key, CancellationToken cancellationToken = default);
        Task<bool> SetAsync<TValue>(string key, TValue value, CacheOptions<T, TKey> options, CancellationToken? cancellationToken = null);
        Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);
    }
}