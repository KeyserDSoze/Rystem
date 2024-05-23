using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Rystem.Content.Infrastructure.Storage
{
    internal static class BlobServiceClientFactory
    {
        internal static Task<Func<IServiceProvider, BlobServiceClientWrapper>> GetClientAsync(BlobStorageConnectionSettings settings)
        {
            if (settings.ConnectionString != null)
            {
                var containerClient = new BlobContainerClient(settings.ConnectionString,
                    settings.ContainerName?.ToLower(), settings.ClientOptions);
                return GetClientFactoryAsync(containerClient, settings.Prefix, settings.IsPublic, settings.UploadOptions);
            }
            else if (settings.EndpointUri != null)
            {
                TokenCredential defaultCredential = settings.ManagedIdentityClientId == null ? new DefaultAzureCredential() : new ManagedIdentityCredential(settings.ManagedIdentityClientId);
                var containerClient = new BlobContainerClient(settings.EndpointUri, defaultCredential, settings.ClientOptions);
                return GetClientFactoryAsync(containerClient, settings.Prefix, settings.IsPublic, settings.UploadOptions);
            }
            throw new ArgumentException($"Wrong installation. You lack for connection string or managed identity.");
        }

        private static async Task<Func<IServiceProvider, BlobServiceClientWrapper>> GetClientFactoryAsync(BlobContainerClient containerClient, string? prefix, bool isPublic, BlobUploadOptions? blobUploadOptions)
        {
            _ = await containerClient.CreateIfNotExistsAsync().NoContext();
            if (isPublic)
                await containerClient.SetAccessPolicyAsync(Azure.Storage.Blobs.Models.PublicAccessType.Blob).NoContext();
            return (serviceProvider) => new BlobServiceClientWrapper
            {
                ContainerClient = containerClient,
                Prefix = prefix,
                UploadOptions = blobUploadOptions
            };
        }
    }
}
