using Azure.Storage.Blobs;

namespace Rystem.Content.Infrastructure.Storage
{
    public sealed class BlobServiceClientWrapper
    {
        public BlobContainerClient? ContainerClient { get; set; }
        public string? Prefix { get; set; }
    }
}
