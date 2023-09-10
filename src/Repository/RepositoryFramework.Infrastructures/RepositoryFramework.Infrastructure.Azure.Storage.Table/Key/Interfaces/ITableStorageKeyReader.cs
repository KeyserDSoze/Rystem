namespace RepositoryFramework.Infrastructure.Azure.Storage.Table
{
    public interface ITableStorageKeyReader<T, TKey>
        where TKey : notnull
    {
        (string PartitionKey, string RowKey) Read(TKey key, TableStorageSettings<T, TKey> settings);
        TKey Read(T entity, TableStorageSettings<T, TKey> settings);
    }
}
