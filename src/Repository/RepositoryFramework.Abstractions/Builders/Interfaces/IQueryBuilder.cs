namespace RepositoryFramework
{
    public interface IQueryBuilder<T, TKey> : IRepositoryBaseBuilder<T, TKey, IQueryPattern<T, TKey>, IQueryBuilder<T, TKey>>
        where TKey : notnull
    {

    }
}
