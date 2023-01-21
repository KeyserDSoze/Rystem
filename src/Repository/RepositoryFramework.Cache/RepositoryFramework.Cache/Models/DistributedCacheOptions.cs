namespace RepositoryFramework.Cache
{
    /// <summary>
    /// Settings for your cache, Refresh time is the expiration date from DateTime.UtcNow.Add(RefreshTime).
    /// Methods is the flag to setup the method allowed to perform an update/insert, delete or get on cache.
    /// For instance if you set Methods = Query | Get | Exist | Update | Delete | Insert on the commands Update
    /// Delete, and Insert everytime will be done an update on cache; with Query, Get and Exist everytime one of those
    /// methods are called the cache will be populated.
    /// </summary>
    /// <typeparam name="T">Model used for your repository.</typeparam>
    /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
    /// <typeparam name="TState">Returning state.</typeparam>
    public class DistributedCacheOptions<T, TKey> : CacheOptions<T, TKey>
        where TKey : notnull
    {
        internal static new DistributedCacheOptions<T, TKey> Default { get; } =
            new DistributedCacheOptions<T, TKey>()
            {
                ExpiringTime = TimeSpan.FromDays(365 * 365)
            };
    }
}
