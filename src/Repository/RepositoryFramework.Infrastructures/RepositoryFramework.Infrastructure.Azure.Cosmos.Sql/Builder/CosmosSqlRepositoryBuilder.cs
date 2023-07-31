using System.Linq.Expressions;
using System.Net;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.Azure.Cosmos.Sql
{
    internal sealed class CosmosSqlRepositoryBuilder<T, TKey> : ICosmosSqlRepositoryBuilder<T, TKey>, IOptionsBuilderAsync<CosmosSqlClient>
        where TKey : notnull
    {
        public IServiceCollection Services { get; set; }
        public CosmosSqlConnectionSettings Settings { get; } = new()
        {
            ModelType = typeof(T)
        };
        public ICosmosSqlRepositoryBuilder<T, TKey> WithKeyManager<TKeyReader>()
            where TKeyReader : class, ICosmosSqlKeyManager<T, TKey>
        {
            Services
                .AddSingleton<ICosmosSqlKeyManager<T, TKey>, TKeyReader>();
            return this;
        }
        public ICosmosSqlRepositoryBuilder<T, TKey> WithId(Expression<Func<T, TKey>> property)
        {
            var compiled = property.Compile();
            Services
                .AddSingleton<ICosmosSqlKeyManager<T, TKey>>(
                new DefaultCosmosSqlKeyManager<T, TKey>(x => compiled.Invoke(x)));
            return this;
        }
        public Task<Func<IServiceProvider, CosmosSqlClient>> BuildAsync()
        {
            if (Settings.ConnectionString == null && Settings.EndpointUri != null)
            {
                CosmosClient cosmosClient = new(Settings.EndpointUri.AbsoluteUri,
                    Settings.ManagedIdentityClientId == null ? new DefaultAzureCredential() : new ManagedIdentityCredential(Settings.ManagedIdentityClientId),
                    Settings.ClientOptions);
                return AddAsync(Settings.DatabaseName, Settings.ContainerName ?? Settings.ModelType.Name,
                    "id", cosmosClient, Settings.DatabaseOptions, Settings.ContainerOptions);
            }
            else if (Settings.ConnectionString != null)
            {
                CosmosClient cosmosClient = new(Settings.ConnectionString, Settings.ClientOptions);
                return AddAsync(Settings.DatabaseName, Settings.ContainerName ?? Settings.ModelType.Name,
                    "id", cosmosClient, Settings.DatabaseOptions, Settings.ContainerOptions);
            }
            throw new ArgumentException($"Wrong installation for {Settings.ModelType.Name} model in your repository cosmos sql database. Use managed identity or a connection string.");
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
                        Properties = Settings.ModelType.GetProperties(),
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
