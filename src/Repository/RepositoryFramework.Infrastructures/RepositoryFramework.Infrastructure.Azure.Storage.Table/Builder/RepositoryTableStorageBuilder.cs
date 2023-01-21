using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Table
{
    internal sealed class RepositoryTableStorageBuilder<T, TKey> : IRepositoryTableStorageBuilder<T, TKey>
        where TKey : notnull
    {
        public IServiceCollection Services { get; }
        public RepositoryTableStorageBuilder(IServiceCollection services)
            => Services = services;
        public IRepositoryTableStorageBuilder<T, TKey> WithPartitionKey<TProperty, TKeyProperty>(
            Expression<Func<T, TProperty>> property,
            Expression<Func<TKey, TKeyProperty>> keyProperty)
            => WithProperty(nameof(WithPartitionKey), property, keyProperty);
        public IRepositoryTableStorageBuilder<T, TKey> WithRowKey<TProperty, TKeyProperty>(
            Expression<Func<T, TProperty>> property,
            Expression<Func<TKey, TKeyProperty>> keyProperty)
            => WithProperty(nameof(WithRowKey), property, keyProperty);
        public IRepositoryTableStorageBuilder<T, TKey> WithRowKey<TProperty>(
           Expression<Func<T, TProperty>> property)
           => WithProperty<TProperty, object>(nameof(WithRowKey), property, null);
        public IRepositoryTableStorageBuilder<T, TKey> WithTimestamp(Expression<Func<T, DateTime>> property)
            => WithProperty<DateTime, object>(nameof(WithTimestamp), property, null!);

        public IRepositoryTableStorageBuilder<T, TKey> WithTableStorageKeyReader<TKeyReader>()
            where TKeyReader : class, ITableStorageKeyReader<T, TKey>
        {
            Services
                .AddSingleton<ITableStorageKeyReader<T, TKey>, TKeyReader>();
            return this;
        }
        private IRepositoryTableStorageBuilder<T, TKey> WithProperty<TProperty, TKeyProperty>(
           string propertyName,
           Expression<Func<T, TProperty>> property,
           Expression<Func<TKey, TKeyProperty>>? keyProperty)
        {
            AddPropertyForTableStorageBaseProperties(propertyName, property, keyProperty);
            return this;
        }
        private void AddPropertyForTableStorageBaseProperties<TProperty, TKeyProperty>(string propertyName,
            Expression<Func<T, TProperty>>? property,
            Expression<Func<TKey, TKeyProperty>>? keyProperty)
        {
            if (property == null)
            {
                TableStorageSettings<T, TKey>.Instance.PartitionKeyFunction = x => typeof(T).FetchProperties().First().GetValue(x)!.ToString()!;
                TableStorageSettings<T, TKey>.Instance.RowKeyFunction = x => typeof(T).FetchProperties().Skip(1).First().GetValue(x)!.ToString()!;
                TableStorageSettings<T, TKey>.Instance.TimestampFunction = x => (DateTime)(typeof(T).FetchProperties().FirstOrDefault(x => x.PropertyType == typeof(DateTime))?.GetValue(x) ?? DateTime.MinValue);
                TableStorageSettings<T, TKey>.Instance.PartitionKey = typeof(T).FetchProperties().First().Name;
                TableStorageSettings<T, TKey>.Instance.RowKey = typeof(T).FetchProperties().Skip(1).First().Name;
                TableStorageSettings<T, TKey>.Instance.Timestamp = typeof(T).FetchProperties().FirstOrDefault(x => x.PropertyType == typeof(DateTime))?.Name;
            }
            else
            {
                var name = property.Body.ToString().Split('.').Last();
                var compiledProperty = property.Compile();
                var compiledKeyProperty = keyProperty?.Compile();
                if (propertyName == nameof(WithPartitionKey))
                {
                    TableStorageSettings<T, TKey>.Instance.PartitionKeyFunction = x => compiledProperty(x)!.ToString()!;
                    TableStorageSettings<T, TKey>.Instance.PartitionKey = name;
                    if (compiledKeyProperty != null)
                        TableStorageSettings<T, TKey>.Instance.PartitionKeyFromKeyFunction = x => compiledKeyProperty(x)!.ToString()!;
                }
                else if (propertyName == nameof(WithRowKey))
                {
                    TableStorageSettings<T, TKey>.Instance.RowKeyFunction = x => compiledProperty(x)!.ToString()!;
                    if (compiledKeyProperty != null)
                    {
                        TableStorageSettings<T, TKey>.Instance.RowKey = name;
                        TableStorageSettings<T, TKey>.Instance.RowKeyFromKeyFunction = x => compiledKeyProperty(x)!.ToString()!;
                    }
                }
                else
                {
                    TableStorageSettings<T, TKey>.Instance.TimestampFunction = x => Convert.ToDateTime(compiledProperty(x)!);
                    TableStorageSettings<T, TKey>.Instance.Timestamp = name;
                }
            }
            Services.AddSingleton(TableStorageSettings<T, TKey>.Instance);
        }
    }
}
