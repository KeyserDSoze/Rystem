using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Table
{
    internal sealed class DefaultTableStorageKeyReader<T, TKey> : ITableStorageKeyReader<T, TKey>, IServiceForFactory
        where TKey : notnull
    {

        //todo: this is some that I don't want happens, When i use a factory of an options, I want to have the factory of my options
        //and now it doesn't work
        //private TableStorageSettings<T, TKey> _options = null!;
        //private readonly IFactory<TableClientWrapper<T, TKey>> _factory;

        //public DefaultTableStorageKeyReader(IFactory<TableClientWrapper<T, TKey>> factory)
        //{
        //    _factory = factory;
        //}
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
