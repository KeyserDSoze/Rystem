using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.Content.Infrastructure.Storage
{
    public sealed class BlobServiceClientWrapper : IFactoryOptions
    {
        public BlobContainerClient? ContainerClient { get; set; }
        public BlobUploadOptions? UploadOptions { get; set; }
        public string? Prefix { get; set; }
    }
}
