using Microsoft.Graph;

namespace Rystem.Content.Infrastructure
{
    public sealed class SharepointClientWrapper
    {
        public Func<GraphServiceClient> Creator { get; init; }
        public string SiteId { get; init; }
        public string DocumentLibraryId { get; init; }
    }
}
