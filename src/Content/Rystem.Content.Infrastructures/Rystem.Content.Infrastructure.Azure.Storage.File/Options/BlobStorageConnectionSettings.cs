using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.Content.Infrastructure.Storage
{
    public class FileStorageConnectionSettings : IOptionsBuilderAsync<FileServiceClientWrapper>
    {
        public Uri? EndpointUri { get; set; }
        public string? ManagedIdentityClientId { get; set; }
        public string? ConnectionString { get; set; }
        public string? ShareName { get; set; }
        public string? Prefix { get; set; }
        public bool IsPublic { get; set; }
        public ShareClientOptions? ClientOptions { get; set; }
        public ShareCreateOptions? ClientCreateOptions { get; set; }
        public List<ShareSignedIdentifier>? Permissions { get; set; }
        public ShareFileRequestConditions? Conditions { get; set; }
        public Task<Func<IServiceProvider, FileServiceClientWrapper>> BuildAsync()
            => FileServiceClientFactory.GetClientAsync(this);
    }
}
