using System.Diagnostics.Contracts;
using Azure.Storage.Blobs;

namespace Rystem.Content.Infrastructure.Storage
{
    public class BlobStorageConnectionSettings
    {
        public Uri? EndpointUri { get; set; }
        public string? ManagedIdentityClientId { get; set; }
        public string? ConnectionString { get; set; }
        public string? ContainerName { get; set; }
        public string? Prefix { get; set; }
        public bool IsPublic { get; set; }
        public BlobClientOptions? ClientOptions { get; set; }
    }
}
