using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Blob
{
    internal sealed class BlobStorageRepositoryBuilder<T, TKey> : IBlobStorageRepositoryBuilder<T, TKey>, IOptionsBuilderAsync<BlobContainerClientWrapper>
        where TKey : notnull
    {
        public BlobStorageConnectionSettings Settings { get; } = new()
        {
            ModelType = typeof(T)
        };
        public Task<Func<IServiceProvider, BlobContainerClientWrapper>> BuildAsync()
        {
            if (Settings.ConnectionString != null)
            {
                var containerClient = new BlobContainerClient(Settings.ConnectionString,
                    Settings.ContainerName?.ToLower() ?? Settings.ModelType.Name.ToLower(), Settings.ClientOptions);
                return AddAsync(containerClient);
            }

            if (Settings.EndpointUri != null)
            {
                TokenCredential defaultCredential =
                    Settings.ManagedIdentityClientId == null
                        ? new DefaultAzureCredential()
                        : new ManagedIdentityCredential(Settings.ManagedIdentityClientId);
                var containerClient = new BlobContainerClient(Settings.EndpointUri, defaultCredential, Settings.ClientOptions);
                return AddAsync(containerClient);
            }
            
            throw new ArgumentException($"Wrong installation for {Settings.ModelType.Name} model in your repository blob storage. Use managed identity or a connection string.");
        }
        private async Task<Func<IServiceProvider, BlobContainerClientWrapper>> AddAsync(BlobContainerClient containerClient)
        {
            _ = await containerClient.CreateIfNotExistsAsync().NoContext();
            var wrapper = new BlobContainerClientWrapper
            {
                Client = containerClient,
                Prefix = Settings.Prefix,
            };
            return serviceProvider => wrapper;
        }
    }
}
