using Azure.Data.Tables;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Table
{
    public class TableStorageConnectionSettings
    {
        public Uri? EndpointUri { get; set; }
        public string? ManagedIdentityClientId { get; set; }
        public string? ConnectionString { get; set; }
        public string? TableName { get; set; }
        public TableClientOptions ClientOptions { get; set; } = null!;
    }
}
