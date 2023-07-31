using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Table
{
    internal sealed class DefaultTableStorageKeyReader<T, TKey> : ITableStorageKeyReader<T, TKey>, IServiceForFactory
        where TKey : notnull
    {
        private TableStorageSettings<T, TKey> _options = null!;
        private readonly IFactory<TableClientWrapper<T, TKey>> _factory;

        public DefaultTableStorageKeyReader(IFactory<TableClientWrapper<T, TKey>> factory)
        {
            _factory = factory;
        }

        public (string PartitionKey, string RowKey) Read(TKey key)
            => (_options.PartitionKeyFromKeyFunction(key), _options.RowKeyFromKeyFunction?.Invoke(key) ?? string.Empty);
        public TKey Read(T entity)
        {
            if (_options.RowKeyFromKeyFunction != null)
                return Constructor.InvokeWithBestDynamicFit<TKey>(_options.PartitionKeyFunction(entity), _options.RowKeyFunction(entity))!;
            else
                return CastExtensions.Cast<TKey>(_options.PartitionKeyFunction.Invoke(entity))!;
        }

        public void SetFactoryName(string name)
        {
            _options = _factory.Create(name).Settings;
        }
    }
}
