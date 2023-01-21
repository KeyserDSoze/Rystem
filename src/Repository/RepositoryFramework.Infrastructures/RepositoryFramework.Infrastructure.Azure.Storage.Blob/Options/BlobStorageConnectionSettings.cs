using Azure.Storage.Blobs;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Blob
{
    public class BlobStorageConnectionSettings
    {
        public Uri? EndpointUri { get; set; }
        public string? ManagedIdentityClientId { get; set; }
        public string? ConnectionString { get; set; }
        public string? ContainerName { get; set; }
        public BlobClientOptions? ClientOptions { get; set; }
    }
}
