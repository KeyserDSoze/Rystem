using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using System.Net;
using System.Reflection;

namespace RepositoryFramework.Infrastructure.Azure.Cosmos.Sql
{
    public class CosmosSqlServiceClientFactory
    {
        public static CosmosSqlServiceClientFactory Instance { get; } = new CosmosSqlServiceClientFactory();
        private CosmosSqlServiceClientFactory() { }
        private readonly Dictionary<string, (Container Container, PropertyInfo[] Properties)> _containerServices = new();
        public (Container Container, PropertyInfo[] Properties) Get(string name)
            => _containerServices[name];
        internal CosmosSqlServiceClientFactory Add<T>(CosmosSqlConnectionSettings settings)
        {
            if (settings.ConnectionString == null && settings.EndpointUri != null)
            {
                CosmosClient cosmosClient = new(settings.EndpointUri.AbsoluteUri,
                    settings.ManagedIdentityClientId == null ? new DefaultAzureCredential() : new ManagedIdentityCredential(settings.ManagedIdentityClientId),
                    settings.ClientOptions);
                return Add<T>(settings.DatabaseName, settings.ContainerName ?? typeof(T).Name, "id", cosmosClient, settings.DatabaseOptions, settings.ContainerOptions);
            }
            else if (settings.ConnectionString != null)
            {
                CosmosClient cosmosClient = new(settings.ConnectionString, settings.ClientOptions);
                return Add<T>(settings.DatabaseName, settings.ContainerName ?? typeof(T).Name, "id", cosmosClient, settings.DatabaseOptions, settings.ContainerOptions);
            }
            throw new ArgumentException($"Wrong installation for {typeof(T).Name} model in your repository cosmos sql database. Use managed identity or a connection string.");
        }
        internal CosmosSqlServiceClientFactory Add<T>(string databaseName, string name, string keyName, string connectionString, CosmosClientOptions? clientOptions, CosmosSettings? databaseOptions, CosmosSettings? containerOptions)
        {
            CosmosClient cosmosClient = new(connectionString, clientOptions);
            return Add<T>(databaseName, name, keyName, cosmosClient, databaseOptions, containerOptions);
        }
        private CosmosSqlServiceClientFactory Add<T>(string databaseName, string name, string keyName, CosmosClient cosmosClient, CosmosSettings? databaseOptions, CosmosSettings? containerOptions)
        {
            if (!_containerServices.ContainsKey(name))
            {
                var databaseResponse = cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName,
                    databaseOptions?.ThroughputProperties,
                    databaseOptions?.RequestOptions).ToResult();
                if (databaseResponse.StatusCode == HttpStatusCode.OK || databaseResponse.StatusCode == HttpStatusCode.Created)
                {
                    var containerResponse = databaseResponse.Database.CreateContainerIfNotExistsAsync(
                            new ContainerProperties
                            {
                                Id = name,
                                PartitionKeyPath = $"/{keyName}"
                            },
                            containerOptions?.ThroughputProperties,
                            containerOptions?.RequestOptions)
                                .ConfigureAwait(false).GetAwaiter().GetResult();
                    if (containerResponse.StatusCode == HttpStatusCode.OK || containerResponse.StatusCode == HttpStatusCode.Created)
                        _containerServices.Add(name, (containerResponse.Container, typeof(T).GetProperties()));
                    else
                        throw new ArgumentException($"It's not possible to create a container with name {name} and key path {keyName}.");
                }
                else
                    throw new ArgumentException($"It's not possible to create a database with name {databaseName}.");
            }
            return this;
        }
    }
}
