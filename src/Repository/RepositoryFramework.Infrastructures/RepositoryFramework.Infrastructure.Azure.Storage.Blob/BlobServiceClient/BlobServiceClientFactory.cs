using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Blob
{
    public class BlobServiceClientFactory
    {
        public static BlobServiceClientFactory Instance { get; } = new BlobServiceClientFactory();
        private BlobServiceClientFactory() { }
        private readonly Dictionary<string, BlobContainerClient> _containerClientFactories = new();
        public BlobContainerClient Get(string name)
            => _containerClientFactories[name];
        internal BlobServiceClientFactory Add<T>(BlobStorageConnectionSettings settings)
        {
            if (settings.ConnectionString != null)
            {
                var containerClient = new BlobContainerClient(settings.ConnectionString,
                    settings.ContainerName?.ToLower() ?? typeof(T).Name.ToLower(), settings.ClientOptions);
                return Add(typeof(T).Name, containerClient);
            }
            else if (settings.EndpointUri != null)
            {
                TokenCredential defaultCredential = settings.ManagedIdentityClientId == null ? new DefaultAzureCredential() : new ManagedIdentityCredential(settings.ManagedIdentityClientId);
                var containerClient = new BlobContainerClient(settings.EndpointUri, defaultCredential, settings.ClientOptions);
                return Add(typeof(T).Name, containerClient);
            }
            throw new ArgumentException($"Wrong installation for {typeof(T).Name} model in your repository blob storage. Use managed identity or a connection string.");
        }
        private BlobServiceClientFactory Add(string name, BlobContainerClient containerClient)
        {
            if (!_containerClientFactories.ContainsKey(name))
            {
                _ = containerClient.CreateIfNotExistsAsync().ToResult();
                _containerClientFactories.Add(name, containerClient);
            }
            return this;
        }
    }
}
