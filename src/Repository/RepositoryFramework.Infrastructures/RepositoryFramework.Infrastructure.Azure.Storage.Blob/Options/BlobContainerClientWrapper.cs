using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Blob
{
    public sealed class BlobContainerClientWrapper : IFactoryOptions
    {
        public BlobContainerClient Client { get; set; } = null!;
        public string? Prefix { get; set; }
    }
}
