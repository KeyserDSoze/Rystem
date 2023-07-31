using Azure.Data.Tables;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Table
{
    internal sealed class TableClientWrapper<T, TKey>
        where TKey : notnull
    {
        public TableClient Client { get; set; } = null!;
        public TableStorageSettings<T, TKey> Settings { get; set; } = null!;
    }
}
