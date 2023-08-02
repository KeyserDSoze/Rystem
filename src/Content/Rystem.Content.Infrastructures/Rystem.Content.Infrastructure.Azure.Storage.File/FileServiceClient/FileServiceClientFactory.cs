using System.IO;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;

namespace Rystem.Content.Infrastructure.Storage
{
    internal static class FileServiceClientFactory
    {
        internal static Task<Func<IServiceProvider, FileServiceClientWrapper>> GetClientAsync(FileStorageConnectionSettings settings)
        {
            if (settings.ConnectionString != null)
            {
                var containerClient = new ShareClient(settings.ConnectionString,
                    settings.ShareName, settings.ClientOptions);
                return GetClientFactoryAsync(containerClient, settings);
            }
            else if (settings.EndpointUri != null)
            {
                TokenCredential defaultCredential = settings.ManagedIdentityClientId == null ? new DefaultAzureCredential() : new ManagedIdentityCredential(settings.ManagedIdentityClientId);
                var containerClient = new ShareClient(settings.EndpointUri, defaultCredential, settings.ClientOptions);
                return GetClientFactoryAsync(containerClient, settings);
            }
            throw new ArgumentException($"Wrong installation. You lack for connection string or managed identity.");
        }

        private static async Task<Func<IServiceProvider, FileServiceClientWrapper>> GetClientFactoryAsync(ShareClient shareClient,
            FileStorageConnectionSettings settings)
        {
            _ = await shareClient.CreateIfNotExistsAsync(settings.ClientCreateOptions).NoContext();
            await shareClient.SetAccessPolicyAsync(settings.Permissions, settings.Conditions).NoContext();
            if (!string.IsNullOrWhiteSpace(settings.Prefix))
            {
                var directoryClient = shareClient.GetDirectoryClient(settings.Prefix);
                await directoryClient.CreateIfNotExistsAsync().NoContext();
            }
            return (serviceProvider) => new FileServiceClientWrapper
            {
                ShareClient = shareClient,
                Prefix = settings.Prefix
            };
        }
    }
}
