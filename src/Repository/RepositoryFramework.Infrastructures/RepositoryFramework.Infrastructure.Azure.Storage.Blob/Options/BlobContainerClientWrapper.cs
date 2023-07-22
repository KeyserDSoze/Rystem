using Azure.Storage.Blobs;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Blob
{
    public sealed class BlobContainerClientWrapper
    {
        public BlobContainerClient Client { get; set; } = null!;
        public string? Prefix { get; set; }
    }
}
