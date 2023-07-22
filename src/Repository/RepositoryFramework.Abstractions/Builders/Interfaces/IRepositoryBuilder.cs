namespace RepositoryFramework
{
    public interface IRepositoryBuilder<T, TKey> : IRepositoryBaseBuilder<T, TKey, IRepositoryPattern<T, TKey>, IRepositoryBuilder<T, TKey>>
        where TKey : notnull
    {
    }
}
