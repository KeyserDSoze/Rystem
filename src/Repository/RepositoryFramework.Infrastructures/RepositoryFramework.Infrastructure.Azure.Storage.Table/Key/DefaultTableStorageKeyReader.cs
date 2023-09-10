using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Table
{
    internal sealed class DefaultTableStorageKeyReader<T, TKey> : ITableStorageKeyReader<T, TKey>, IServiceForFactory
        where TKey : notnull
    {
        public (string PartitionKey, string RowKey) Read(TKey key, TableStorageSettings<T, TKey> settings)
            => (settings.PartitionKeyFromKeyFunction(key), settings.RowKeyFromKeyFunction?.Invoke(key) ?? string.Empty);
        public TKey Read(T entity, TableStorageSettings<T, TKey> settings)
        {
            if (settings.RowKeyFromKeyFunction != null)
                return Constructor.InvokeWithBestDynamicFit<TKey>(settings.PartitionKeyFunction(entity), settings.RowKeyFunction(entity))!;
            else
                return CastExtensions.Cast<TKey>(settings.PartitionKeyFunction.Invoke(entity))!;
        }

        public void SetFactoryName(string name)
        {
        }
    }
}
