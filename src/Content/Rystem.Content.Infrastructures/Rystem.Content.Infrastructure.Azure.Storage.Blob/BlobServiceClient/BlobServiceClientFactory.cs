using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;

namespace Rystem.Content.Infrastructure.Storage
{
    internal sealed class BlobServiceClientFactory
    {
        public static BlobServiceClientFactory Instance { get; } = new BlobServiceClientFactory();
        private BlobServiceClientFactory() { }
        private readonly Dictionary<string, BlobServiceClientWrapper> _containerClientFactories = new();
        public BlobServiceClientWrapper Get(string name)
            => _containerClientFactories[name];
        public BlobServiceClientWrapper First()
            => _containerClientFactories.First().Value;
        internal BlobServiceClientFactory Add(BlobStorageConnectionSettings settings, string name)
        {
            if (settings.ConnectionString != null)
            {
                var containerClient = new BlobContainerClient(settings.ConnectionString,
                    settings.ContainerName?.ToLower() ?? name.ToLower(), settings.ClientOptions);
                return Add(name, containerClient, settings.Prefix, settings.IsPublic);
            }
            else if (settings.EndpointUri != null)
            {
                TokenCredential defaultCredential = settings.ManagedIdentityClientId == null ? new DefaultAzureCredential() : new ManagedIdentityCredential(settings.ManagedIdentityClientId);
                var containerClient = new BlobContainerClient(settings.EndpointUri, defaultCredential, settings.ClientOptions);
                return Add(name, containerClient, settings.Prefix, settings.IsPublic);
            }
            throw new ArgumentException($"Wrong installation for {name} model in your repository blob storage. Use managed identity or a connection string.");
        }
        private BlobServiceClientFactory Add(string name, BlobContainerClient containerClient, string? prefix, bool isPublic)
        {
            if (!_containerClientFactories.ContainsKey(name))
            {
                _ = containerClient.CreateIfNotExistsAsync().ToResult();
                if (isPublic)
                    containerClient.SetAccessPolicyAsync(Azure.Storage.Blobs.Models.PublicAccessType.Blob).ToResult();
                _containerClientFactories.Add(name, new BlobServiceClientWrapper
                {
                    ContainerClient = containerClient,
                    Prefix = prefix
                });
            }
            return this;
        }
    }
}
