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
    public class CacheOptions<T, TKey>
        where TKey : notnull
    {
        public TimeSpan ExpiringTime { get; set; }
        public bool HasCommandPattern => Methods.HasFlag(RepositoryMethods.Insert)
            || Methods.HasFlag(RepositoryMethods.Update)
            || Methods.HasFlag(RepositoryMethods.Delete)
            || Methods.HasFlag(RepositoryMethods.All);
        public bool HasCache(RepositoryMethods method) => Methods.HasFlag(RepositoryMethods.All) || Methods.HasFlag(method);
        public RepositoryMethods Methods { get; set; } = RepositoryMethods.Query | RepositoryMethods.Get | RepositoryMethods.Exist;
        internal static CacheOptions<T, TKey> Default { get; } = new CacheOptions<T, TKey>()
        {
            ExpiringTime = TimeSpan.FromDays(365)
        };
    }
}
