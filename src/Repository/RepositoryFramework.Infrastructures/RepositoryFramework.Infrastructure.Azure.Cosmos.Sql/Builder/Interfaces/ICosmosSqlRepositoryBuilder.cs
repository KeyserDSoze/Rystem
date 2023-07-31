using System.Linq.Expressions;

namespace RepositoryFramework.Infrastructure.Azure.Cosmos.Sql
{
    public interface ICosmosSqlRepositoryBuilder<T, TKey>
        where TKey : notnull
    {
        CosmosSqlConnectionSettings Settings { get; }
        ICosmosSqlRepositoryBuilder<T, TKey> WithKeyManager<TKeyReader>()
            where TKeyReader : class, ICosmosSqlKeyManager<T, TKey>;
        ICosmosSqlRepositoryBuilder<T, TKey> WithId(Expression<Func<T, TKey>> property);
    }
}
