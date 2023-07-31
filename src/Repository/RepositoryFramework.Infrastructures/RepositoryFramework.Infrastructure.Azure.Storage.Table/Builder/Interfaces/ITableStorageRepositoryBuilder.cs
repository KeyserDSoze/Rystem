using System.Linq.Expressions;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Table
{
    public interface ITableStorageRepositoryBuilder<T, TKey>
        where TKey : notnull
    {
        TableStorageConnectionSettings Settings { get; }
        ITableStorageRepositoryBuilder<T, TKey> WithTableStorageKeyReader<TKeyReader>()
            where TKeyReader : class, ITableStorageKeyReader<T, TKey>;
        ITableStorageRepositoryBuilder<T, TKey> WithPartitionKey<TProperty, TKeyProperty>(
           Expression<Func<T, TProperty>> property, Expression<Func<TKey, TKeyProperty>> keyProperty);
        ITableStorageRepositoryBuilder<T, TKey> WithRowKey<TProperty, TKeyProperty>(
           Expression<Func<T, TProperty>> property, Expression<Func<TKey, TKeyProperty>> keyProperty);
        ITableStorageRepositoryBuilder<T, TKey> WithRowKey<TProperty>(Expression<Func<T, TProperty>> property);
        ITableStorageRepositoryBuilder<T, TKey> WithTimestamp(
           Expression<Func<T, DateTime>> property);
    }
}
