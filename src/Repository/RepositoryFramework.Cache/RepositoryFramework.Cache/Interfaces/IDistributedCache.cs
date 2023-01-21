namespace RepositoryFramework.Cache
{
    public interface IDistributedCache<T, TKey> : ICache<T, TKey>
        where TKey : notnull
    {
    }
}