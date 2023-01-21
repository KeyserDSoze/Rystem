using Microsoft.Azure.Cosmos;

namespace RepositoryFramework.Infrastructure.Azure.Cosmos.Sql
{
    /// <summary>
    /// Options used for database creation and container creation.
    /// </summary>
    public sealed class CosmosSettings
    {
        /// <summary>
        /// Represents a throughput of the resources in the Azure Cosmos DB service. It is
        /// the standard pricing for the resource in the Azure Cosmos DB service.
        /// </summary>
        public ThroughputProperties? ThroughputProperties { get; set; }
        /// <summary>
        /// The default cosmos request options.
        /// </summary>
        public RequestOptions? RequestOptions { get; set; }
    }
}
