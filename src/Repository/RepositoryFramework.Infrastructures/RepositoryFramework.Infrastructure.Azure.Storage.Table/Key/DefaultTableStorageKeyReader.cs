using System.Reflection;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Table
{
    internal sealed class DefaultTableStorageKeyReader<T, TKey> : ITableStorageKeyReader<T, TKey>
        where TKey : notnull
    {
        private readonly TableStorageSettings<T, TKey> _options;
        public DefaultTableStorageKeyReader(TableStorageSettings<T, TKey> options)
        {
            _options = options;
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
    }
}
