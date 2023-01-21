using Microsoft.Azure.Cosmos;

namespace RepositoryFramework.Infrastructure.Azure.Cosmos.Sql
{
    /// <summary>
    /// Settings for your cosmos db and container.
    /// </summary>
    public sealed class CosmosSqlConnectionSettings
    {
        public Uri? EndpointUri { get; set; }
        public string? ManagedIdentityClientId { get; set; }
        public string? ConnectionString { get; set; }
        public string DatabaseName { get; set; } = null!;
        public string? ContainerName { get; set; }
        public CosmosClientOptions? ClientOptions { get; set; }
        public CosmosSettings? DatabaseOptions { get; set; }
        public CosmosSettings? ContainerOptions { get; set; }
    }
}
