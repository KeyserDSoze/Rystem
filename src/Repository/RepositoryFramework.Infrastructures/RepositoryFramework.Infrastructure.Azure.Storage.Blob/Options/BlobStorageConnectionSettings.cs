using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Blob
{
    public class BlobStorageConnectionSettings : IOptionsBuilderAsync<BlobContainerClientWrapper>
    {
        public Uri? EndpointUri { get; set; }
        public string? ManagedIdentityClientId { get; set; }
        public string? ConnectionString { get; set; }
        public string? ContainerName { get; set; }
        public string? Prefix { get; set; }
        public BlobClientOptions? ClientOptions { get; set; }
        internal Type ModelType { get; set; } = null!;
        public Task<Func<IServiceProvider, BlobContainerClientWrapper>> BuildAsync()
        {
            if (ConnectionString != null)
            {
                var containerClient = new BlobContainerClient(ConnectionString,
                    ContainerName?.ToLower() ?? ModelType.Name.ToLower(), ClientOptions);
                return AddAsync(containerClient);
            }
            else if (EndpointUri != null)
            {
                TokenCredential defaultCredential =
                    ManagedIdentityClientId == null ?
                    new DefaultAzureCredential()
                    : new ManagedIdentityCredential(ManagedIdentityClientId);
                var containerClient = new BlobContainerClient(EndpointUri, defaultCredential, ClientOptions);
                return AddAsync(containerClient);
            }
            throw new ArgumentException($"Wrong installation for {ModelType.Name} model in your repository blob storage. Use managed identity or a connection string.");
        }
        private async Task<Func<IServiceProvider, BlobContainerClientWrapper>> AddAsync(BlobContainerClient containerClient)
        {
            _ = await containerClient.CreateIfNotExistsAsync().NoContext();
            var wrapper = new BlobContainerClientWrapper
            {
                Client = containerClient,
                Prefix = Prefix,
            };
            return serviceProvider => wrapper;
        }
    }
}
