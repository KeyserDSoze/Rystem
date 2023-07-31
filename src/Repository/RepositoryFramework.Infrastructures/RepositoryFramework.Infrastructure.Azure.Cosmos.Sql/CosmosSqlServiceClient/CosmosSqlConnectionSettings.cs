using Azure.Identity;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

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
        internal Type ModelType { get; set; } = null!;
    }
}
