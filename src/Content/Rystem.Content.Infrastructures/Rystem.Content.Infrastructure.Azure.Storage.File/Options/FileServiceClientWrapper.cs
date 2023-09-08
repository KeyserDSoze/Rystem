using Azure.Storage.Files.Shares;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.Content.Infrastructure.Storage
{
    public sealed class FileServiceClientWrapper : IFactoryOptions
    {
        public ShareClient? ShareClient { get; set; }
        public string? Prefix { get; set; }
    }
}
