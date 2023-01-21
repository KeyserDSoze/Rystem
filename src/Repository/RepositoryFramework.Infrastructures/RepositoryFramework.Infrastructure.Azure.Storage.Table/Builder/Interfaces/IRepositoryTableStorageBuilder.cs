using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Table
{
    public interface IRepositoryTableStorageBuilder<T, TKey>
        where TKey : notnull
    {
        IRepositoryTableStorageBuilder<T, TKey> WithTableStorageKeyReader<TKeyReader>()
            where TKeyReader : class, ITableStorageKeyReader<T, TKey>;
        IRepositoryTableStorageBuilder<T, TKey> WithPartitionKey<TProperty, TKeyProperty>(
           Expression<Func<T, TProperty>> property, Expression<Func<TKey, TKeyProperty>> keyProperty);
        IRepositoryTableStorageBuilder<T, TKey> WithRowKey<TProperty, TKeyProperty>(
           Expression<Func<T, TProperty>> property, Expression<Func<TKey, TKeyProperty>> keyProperty);
        IRepositoryTableStorageBuilder<T, TKey> WithRowKey<TProperty>(Expression<Func<T, TProperty>> property);
        IRepositoryTableStorageBuilder<T, TKey> WithTimestamp(
           Expression<Func<T, DateTime>> property);
        IServiceCollection Services { get; }
    }
}
