using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Table
{
    internal sealed class TableClientWrapper<T, TKey> : IFactoryOptions
        where TKey : notnull
    {
        public TableClient Client { get; set; } = null!;
        public TableStorageSettings<T, TKey> Settings { get; set; } = null!;
    }
}
