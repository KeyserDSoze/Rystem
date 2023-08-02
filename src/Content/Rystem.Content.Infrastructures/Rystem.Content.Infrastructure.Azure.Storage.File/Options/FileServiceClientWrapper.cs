using Azure.Storage.Files.Shares;

namespace Rystem.Content.Infrastructure.Storage
{
    public sealed class FileServiceClientWrapper
    {
        public ShareClient? ShareClient { get; set; }
        public string? Prefix { get; set; }
    }
}
