using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;

namespace Rystem.Content.Infrastructure.Storage
{
    internal static class BlobServiceClientFactory
    {
        internal static Task<Func<BlobServiceClientWrapper>> GetClientAsync(BlobStorageConnectionSettings settings)
        {
            if (settings.ConnectionString != null)
            {
                var containerClient = new BlobContainerClient(settings.ConnectionString,
                    settings.ContainerName?.ToLower(), settings.ClientOptions);
                return GetClientFactoryAsync(containerClient, settings.Prefix, settings.IsPublic);
            }
            else if (settings.EndpointUri != null)
            {
                TokenCredential defaultCredential = settings.ManagedIdentityClientId == null ? new DefaultAzureCredential() : new ManagedIdentityCredential(settings.ManagedIdentityClientId);
                var containerClient = new BlobContainerClient(settings.EndpointUri, defaultCredential, settings.ClientOptions);
                return GetClientFactoryAsync(containerClient, settings.Prefix, settings.IsPublic);
            }
            throw new ArgumentException($"Wrong installation. You lack for connection string or managed identity.");
        }

        internal static Task<Func<BlobServiceClientWrapper>> GetClientAsync()
        {
            throw new NotImplementedException();
        }

        private static async Task<Func<BlobServiceClientWrapper>> GetClientFactoryAsync(BlobContainerClient containerClient, string? prefix, bool isPublic)
        {
            _ = await containerClient.CreateIfNotExistsAsync().NoContext();
            if (isPublic)
                await containerClient.SetAccessPolicyAsync(Azure.Storage.Blobs.Models.PublicAccessType.Blob).NoContext();
            return () => new BlobServiceClientWrapper
            {
                ContainerClient = containerClient,
                Prefix = prefix
            };
        }
    }
}
