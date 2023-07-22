namespace RepositoryFramework
{
    internal sealed class RepositoryFrameworkBuilder<T, TKey> : RepositoryBaseBuilder<T, TKey, IRepository<T, TKey>, Repository<T, TKey>, IRepositoryPattern<T, TKey>, IRepositoryBuilder<T, TKey>>, IRepositoryBuilder<T, TKey>
        where TKey : notnull
    {
    }
}
