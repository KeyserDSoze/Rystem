using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.Content.Infrastructure.Storage
{
    public class BlobStorageConnectionSettings : IServiceOptionsAsync<BlobServiceClientWrapper>
    {
        public Uri? EndpointUri { get; set; }
        public string? ManagedIdentityClientId { get; set; }
        public string? ConnectionString { get; set; }
        public string? ContainerName { get; set; }
        public string? Prefix { get; set; }
        public bool IsPublic { get; set; }
        public BlobClientOptions? ClientOptions { get; set; }

        public Task<Func<IServiceProvider, BlobServiceClientWrapper>> BuildAsync()
            => BlobServiceClientFactory.GetClientAsync(this);
    }
}
