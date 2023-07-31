using Azure.Identity;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.Azure.Cosmos.Sql
{
    /// <summary>
    /// Settings for your cosmos db and container.
    /// </summary>
    public sealed class CosmosSqlConnectionSettings : IOptionsToBuildAsync<CosmosSqlClient>
    {
        public Uri? EndpointUri { get; set; }
        public string? ManagedIdentityClientId { get; set; }
        public string? ConnectionString { get; set; }
        public string DatabaseName { get; set; } = null!;
        public string? ContainerName { get; set; }
        public CosmosClientOptions? ClientOptions { get; set; }
        public CosmosSettings? DatabaseOptions { get; set; }
        public CosmosSettings? ContainerOptions { get; set; }
        internal Type ModelType { get; set; } = null!;
        public Task<Func<IServiceProvider, CosmosSqlClient>> BuildAsync()
        {
            if (ConnectionString == null && EndpointUri != null)
            {
                CosmosClient cosmosClient = new(EndpointUri.AbsoluteUri,
                    ManagedIdentityClientId == null ? new DefaultAzureCredential() : new ManagedIdentityCredential(ManagedIdentityClientId),
                    ClientOptions);
                return AddAsync(DatabaseName, ContainerName ?? ModelType.Name, "id", cosmosClient, DatabaseOptions, ContainerOptions);
            }
            else if (ConnectionString != null)
            {
                CosmosClient cosmosClient = new(ConnectionString, ClientOptions);
                return AddAsync(DatabaseName, ContainerName ?? ModelType.Name, "id", cosmosClient, DatabaseOptions, ContainerOptions);
            }
            throw new ArgumentException($"Wrong installation for {ModelType.Name} model in your repository cosmos sql database. Use managed identity or a connection string.");
        }
        private async Task<Func<IServiceProvider, CosmosSqlClient>> AddAsync(string databaseName, string name, string keyName, CosmosClient cosmosClient, CosmosSettings? databaseOptions, CosmosSettings? containerOptions)
        {
            var databaseResponse = await cosmosClient
                .CreateDatabaseIfNotExistsAsync(databaseName,
                databaseOptions?.ThroughputProperties,
                databaseOptions?.RequestOptions)
                .NoContext();
            if (databaseResponse.StatusCode == HttpStatusCode.OK || databaseResponse.StatusCode == HttpStatusCode.Created)
            {
                var containerResponse = await databaseResponse.Database
                    .CreateContainerIfNotExistsAsync(
                        new ContainerProperties
                        {
                            Id = name,
                            PartitionKeyPath = $"/{keyName}"
                        },
                        containerOptions?.ThroughputProperties,
                        containerOptions?.RequestOptions)
                    .NoContext();
                if (containerResponse.StatusCode == HttpStatusCode.OK || containerResponse.StatusCode == HttpStatusCode.Created)
                {
                    var client = new CosmosSqlClient
                    {
                        Container = containerResponse.Container,
                        Properties = ModelType.GetProperties(),
                        ExistsQuery = $"SELECT * FROM {name} x WHERE x.id = @id"
                    };
                    return (serviceProvider) => client;
                }
                else
                    throw new ArgumentException($"It's not possible to create a container with name {name} and key path {keyName}.");
            }
            else
                throw new ArgumentException($"It's not possible to create a database with name {databaseName}.");
        }
    }
}
