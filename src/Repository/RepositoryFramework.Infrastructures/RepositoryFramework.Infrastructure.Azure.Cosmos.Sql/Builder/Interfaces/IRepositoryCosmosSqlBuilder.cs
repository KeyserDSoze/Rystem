using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.Azure.Cosmos.Sql
{
    public interface IRepositoryCosmosSqlBuilder<T, TKey>
        where TKey : notnull
    {
        IRepositoryCosmosSqlBuilder<T, TKey> WithKeyManager<TKeyReader>()
            where TKeyReader : class, ICosmosSqlKeyManager<T, TKey>;
        IRepositoryCosmosSqlBuilder<T, TKey> WithId(Expression<Func<T, TKey>> property);
        IServiceCollection Services { get; }
    }
}
