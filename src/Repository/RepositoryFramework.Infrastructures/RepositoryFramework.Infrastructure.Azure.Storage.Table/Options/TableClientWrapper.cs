using Azure.Data.Tables;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Table
{
    public class TableClientWrapper
    {
        public TableClient Client { get; set; } = null!;
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public string Timestamp { get; set; } = null!;
    }
}
