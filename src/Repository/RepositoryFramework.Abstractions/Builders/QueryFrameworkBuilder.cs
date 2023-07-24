using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework
{
    internal sealed class QueryFrameworkBuilder<T, TKey> : RepositoryBaseBuilder<T, TKey, IQuery<T, TKey>, Query<T, TKey>, IQueryPattern<T, TKey>, IQueryBuilder<T, TKey>>, IQueryBuilder<T, TKey>
        where TKey : notnull
    {
        public QueryFrameworkBuilder(IServiceCollection services) : base(services) { }
    }
}
