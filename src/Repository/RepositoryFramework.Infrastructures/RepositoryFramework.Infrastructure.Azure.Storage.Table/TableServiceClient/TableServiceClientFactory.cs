using Azure.Core;
using Azure.Data.Tables;
using Azure.Identity;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Table
{
    public class TableServiceClientFactory
    {
        public static TableServiceClientFactory Instance { get; } = new TableServiceClientFactory();
        private TableServiceClientFactory() { }
        private readonly Dictionary<string, TableClient> _tableServiceClientFactories = new();
        public TableClient Get(string name)
            => _tableServiceClientFactories[name];
        internal TableServiceClientFactory Add<T>(TableStorageConnectionSettings settings)
        {
            if (settings.ConnectionString != null)
            {
                var serviceClient = new TableServiceClient(settings.ConnectionString, settings.ClientOptions);
                var tableClient = new TableClient(settings.ConnectionString, settings.TableName ?? typeof(T).Name, settings.ClientOptions);
                return Add(typeof(T).Name, serviceClient, tableClient);
            }
            else if (settings.EndpointUri != null)
            {
                TokenCredential defaultCredential = settings.ManagedIdentityClientId == null ? new DefaultAzureCredential() : new ManagedIdentityCredential(settings.ManagedIdentityClientId);
                var serviceClient = new TableServiceClient(settings.EndpointUri, defaultCredential, settings.ClientOptions);
                var tableClient = new TableClient(settings.EndpointUri, settings.TableName ?? typeof(T).Name, defaultCredential, settings.ClientOptions);
                return Add(typeof(T).Name, serviceClient, tableClient);
            }
            throw new ArgumentException($"Wrong installation for {typeof(T).Name} model in your repository table storage. Use managed identity or a connection string.");
        }
        private TableServiceClientFactory Add(string name, TableServiceClient serviceClient, TableClient tableClient)
        {
            if (!_tableServiceClientFactories.ContainsKey(name))
            {
                _ = serviceClient.CreateTableIfNotExistsAsync(name).ToResult();
                _tableServiceClientFactories.Add(name, tableClient);
            }
            return this;
        }
    }
}
