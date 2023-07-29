using Azure.Data.Tables;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Table
{
    public class TableClientWrapper
    {
        public TableClient Client { get; set; } = null!;
    }
}
