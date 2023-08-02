using System.Text;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Files.Shares;

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
                StringBuilder pathBuilder = new();
                foreach (var directory in settings.Prefix.Split('/'))
                {
                    if (string.IsNullOrWhiteSpace(directory))
                        continue;
                    pathBuilder.Append($"{directory}/");
                    var directoryClient = shareClient.GetDirectoryClient(pathBuilder.ToString());
                    await directoryClient.CreateIfNotExistsAsync().NoContext();
                }
            }
            return (serviceProvider) => new FileServiceClientWrapper
            {
                ShareClient = shareClient,
                Prefix = settings.Prefix
            };
        }
    }
}
