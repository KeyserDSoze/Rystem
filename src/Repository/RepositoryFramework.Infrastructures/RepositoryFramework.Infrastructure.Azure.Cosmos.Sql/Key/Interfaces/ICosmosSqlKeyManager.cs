namespace RepositoryFramework.Infrastructure.Azure.Cosmos.Sql
{
    public interface ICosmosSqlKeyManager<in T, TKey>
        where TKey : notnull
    {
        TKey Read(T entity);
        string AsString(TKey key);
    }
}
