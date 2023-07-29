using Azure.Core;
using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Table
{
    public class TableStorageConnectionSettings : IServiceOptionsAsync<TableClientWrapper>
    {
        public Uri? EndpointUri { get; set; }
        public string? ManagedIdentityClientId { get; set; }
        public string? ConnectionString { get; set; }
        public string? TableName { get; set; }
        public TableClientOptions ClientOptions { get; set; } = null!;
        internal Type ModelType { get; set; } = null!;
        public Task<Func<IServiceProvider, TableClientWrapper>> BuildAsync()
        {
            if (ConnectionString != null)
            {
                var serviceClient = new TableServiceClient(ConnectionString, ClientOptions);
                var tableClient = new TableClient(ConnectionString, TableName ?? ModelType.Name, ClientOptions);
                return AddAsync(ModelType.Name, serviceClient, tableClient);
            }
            else if (EndpointUri != null)
            {
                TokenCredential defaultCredential = ManagedIdentityClientId == null ? new DefaultAzureCredential() : new ManagedIdentityCredential(ManagedIdentityClientId);
                var serviceClient = new TableServiceClient(EndpointUri, defaultCredential, ClientOptions);
                var tableClient = new TableClient(EndpointUri, TableName ?? ModelType.Name, defaultCredential, ClientOptions);
                return AddAsync(ModelType.Name, serviceClient, tableClient);
            }
            throw new ArgumentException($"Wrong installation for {ModelType.Name} model in your repository table storage. Use managed identity or a connection string.");
        }
        private static async Task<Func<IServiceProvider, TableClientWrapper>> AddAsync(string name, TableServiceClient serviceClient, TableClient tableClient)
        {
            _ = await serviceClient
                .CreateTableIfNotExistsAsync(name)
                .NoContext();
            var wrapper = new TableClientWrapper
            {
                Client = tableClient
            };
            return (serviceProvider) => wrapper;
        }
    }
}
