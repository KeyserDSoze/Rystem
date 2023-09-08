using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.Content.Infrastructure.Storage
{
    public sealed class BlobServiceClientWrapper : IFactoryOptions
    {
        public BlobContainerClient? ContainerClient { get; set; }
        public string? Prefix { get; set; }
    }
}
