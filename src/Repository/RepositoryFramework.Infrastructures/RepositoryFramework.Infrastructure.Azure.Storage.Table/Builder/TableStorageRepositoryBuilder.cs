using System.Linq.Expressions;
using System.Reflection;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Table
{
    internal sealed class TableStorageRepositoryBuilder<T, TKey> : ITableStorageRepositoryBuilder<T, TKey>, IOptionsBuilderAsync<TableClientWrapper<T, TKey>>
        where TKey : notnull
    {
        private readonly TableStorageSettings<T, TKey> _settings = new();
        internal IServiceCollection Services { get; set; } = null!;
        internal string FactoryName { get; set; } = null!;
        public TableStorageConnectionSettings Settings { get; } = new();
        public TableStorageRepositoryBuilder()
        {
            WithTableStorageKeyReader<DefaultTableStorageKeyReader<T, TKey>>();
            AddPropertyForTableStorageBaseProperties<int, int>(string.Empty, null, null);
        }

        public ITableStorageRepositoryBuilder<T, TKey> WithPartitionKey<TProperty, TKeyProperty>(
            Expression<Func<T, TProperty>> property,
            Expression<Func<TKey, TKeyProperty>> keyProperty)
            => WithProperty(nameof(WithPartitionKey), property, keyProperty);
        public ITableStorageRepositoryBuilder<T, TKey> WithRowKey<TProperty, TKeyProperty>(
            Expression<Func<T, TProperty>> property,
            Expression<Func<TKey, TKeyProperty>> keyProperty)
            => WithProperty(nameof(WithRowKey), property, keyProperty);
        public ITableStorageRepositoryBuilder<T, TKey> WithRowKey<TProperty>(
           Expression<Func<T, TProperty>> property)
           => WithProperty<TProperty, object>(nameof(WithRowKey), property, null);
        public ITableStorageRepositoryBuilder<T, TKey> WithTimestamp(Expression<Func<T, DateTime>> property)
            => WithProperty<DateTime, object>(nameof(WithTimestamp), property, null!);
        public ITableStorageRepositoryBuilder<T, TKey> WithTableStorageKeyReader<TKeyReader>()
            where TKeyReader : class, ITableStorageKeyReader<T, TKey>
        {
            Services
                .AddOrOverrideFactory<ITableStorageKeyReader<T, TKey>, TKeyReader>(FactoryName);
            return this;
        }
        private ITableStorageRepositoryBuilder<T, TKey> WithProperty<TProperty, TKeyProperty>(
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
        public Task<Func<IServiceProvider, TableClientWrapper<T, TKey>>> BuildAsync()
        {
            if (Settings.ConnectionString != null)
            {
                var serviceClient = new TableServiceClient(Settings.ConnectionString, Settings.ClientOptions);
                var tableClient = new TableClient(Settings.ConnectionString, Settings.TableName ?? Settings.ModelType.Name, Settings.ClientOptions);
                return AddAsync(Settings.ModelType.Name, serviceClient, tableClient);
            }
            else if (Settings.EndpointUri != null)
            {
                TokenCredential defaultCredential = Settings.ManagedIdentityClientId == null ? new DefaultAzureCredential() : new ManagedIdentityCredential(Settings.ManagedIdentityClientId);
                var serviceClient = new TableServiceClient(Settings.EndpointUri, defaultCredential, Settings.ClientOptions);
                var tableClient = new TableClient(Settings.EndpointUri, Settings.TableName ?? Settings.ModelType.Name, defaultCredential, Settings.ClientOptions);
                return AddAsync(Settings.ModelType.Name, serviceClient, tableClient);
            }
            throw new ArgumentException($"Wrong installation for {Settings.ModelType.Name} model in your repository table storage. Use managed identity or a connection string.");
        }
        private async Task<Func<IServiceProvider, TableClientWrapper<T, TKey>>> AddAsync(string name, TableServiceClient serviceClient, TableClient tableClient)
        {
            _ = await serviceClient
                .CreateTableIfNotExistsAsync(name)
                .NoContext();
            var wrapper = new TableClientWrapper<T, TKey>
            {
                Client = tableClient,
                Settings = _settings
            };
            return (serviceProvider) => wrapper;
        }
    }
}
