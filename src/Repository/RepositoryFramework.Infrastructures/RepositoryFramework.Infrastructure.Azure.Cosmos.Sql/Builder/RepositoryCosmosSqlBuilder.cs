using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.Azure.Cosmos.Sql
{
    internal sealed class RepositoryCosmosSqlBuilder<T, TKey> : IRepositoryCosmosSqlBuilder<T, TKey>
        where TKey : notnull
    {
        public RepositoryCosmosSqlBuilder(IServiceCollection services)
            => Services = services;
        public IServiceCollection Services { get; }
        public IRepositoryCosmosSqlBuilder<T, TKey> WithKeyManager<TKeyReader>()
            where TKeyReader : class, ICosmosSqlKeyManager<T, TKey>
        {
            Services
                .AddSingleton<ICosmosSqlKeyManager<T, TKey>, TKeyReader>();
            return this;
        }
        public IRepositoryCosmosSqlBuilder<T, TKey> WithId(Expression<Func<T, TKey>> property)
        {
            var compiled = property.Compile();
            Services
                .AddSingleton<ICosmosSqlKeyManager<T, TKey>>(
                new DefaultCosmosSqlKeyManager<T, TKey>(x => compiled.Invoke(x)));
            return this;
        }
    }
}
