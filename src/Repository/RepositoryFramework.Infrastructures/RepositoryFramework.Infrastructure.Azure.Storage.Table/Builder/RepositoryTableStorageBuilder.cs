using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Table
{
    internal sealed class RepositoryTableStorageBuilder<T, TKey> : IRepositoryTableStorageBuilder<T, TKey>
        where TKey : notnull
    {
        private readonly TableStorageSettings<T, TKey> _settings;
        public IServiceCollection Services { get; }
        public RepositoryTableStorageBuilder(IServiceCollection services, string name)
        {
            Services = services;
            _settings = new TableStorageSettings<T, TKey>();
            WithTableStorageKeyReader<DefaultTableStorageKeyReader<T, TKey>>();
            Services.TryAddFactory(_settings, name, ServiceLifetime.Singleton);
        }

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
                _settings.PartitionKeyFunction = x => typeof(T).FetchProperties().First().GetValue(x)!.ToString()!;
                _settings.RowKeyFunction = x => typeof(T).FetchProperties().Skip(1).First().GetValue(x)!.ToString()!;
                _settings.TimestampFunction = x => (DateTime)(typeof(T).FetchProperties().FirstOrDefault(x => x.PropertyType == typeof(DateTime))?.GetValue(x) ?? DateTime.MinValue);
                _settings.PartitionKey = typeof(T).FetchProperties().First().Name;
                _settings.RowKey = typeof(T).FetchProperties().Skip(1).First().Name;
                _settings.Timestamp = typeof(T).FetchProperties().FirstOrDefault(x => x.PropertyType == typeof(DateTime))?.Name;
            }
            else
            {
                var name = property.Body.ToString().Split('.').Last();
                var compiledProperty = property.Compile();
                var compiledKeyProperty = keyProperty?.Compile();
                if (propertyName == nameof(WithPartitionKey))
                {
                    _settings.PartitionKeyFunction = x => compiledProperty(x)!.ToString()!;
                    _settings.PartitionKey = name;
                    if (compiledKeyProperty != null)
                        _settings.PartitionKeyFromKeyFunction = x => compiledKeyProperty(x)!.ToString()!;
                }
                else if (propertyName == nameof(WithRowKey))
                {
                    _settings.RowKeyFunction = x => compiledProperty(x)!.ToString()!;
                    if (compiledKeyProperty != null)
                    {
                        _settings.RowKey = name;
                        _settings.RowKeyFromKeyFunction = x => compiledKeyProperty(x)!.ToString()!;
                    }
                }
                else
                {
                    _settings.TimestampFunction = x => Convert.ToDateTime(compiledProperty(x)!);
                    _settings.Timestamp = name;
                }
            }
        }
    }
}
