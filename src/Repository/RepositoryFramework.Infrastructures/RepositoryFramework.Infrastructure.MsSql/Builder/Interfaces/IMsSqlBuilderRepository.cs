using System.Linq.Expressions;

namespace RepositoryFramework.Infrastructure.MsSql
{
    public interface IMsSqlRepositoryBuilder<T, TKey>
        where TKey : notnull
    {
        string Schema { get; set; }
        string TableName { get; set; }
        string ConnectionString { get; set; }
        IMsSqlRepositoryBuilder<T, TKey> WithColumn<TProperty>(Expression<Func<T, TProperty>> property, Action<PropertyHelper<T>> value);
        IMsSqlRepositoryBuilder<T, TKey> WithPrimaryKey<TProperty>(Expression<Func<T, TProperty>> property, Action<PropertyHelper<T>> value);
    }
}
