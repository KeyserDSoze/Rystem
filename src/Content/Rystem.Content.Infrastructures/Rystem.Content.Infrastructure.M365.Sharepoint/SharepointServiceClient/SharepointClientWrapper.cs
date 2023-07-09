using Microsoft.Graph;
using Microsoft.Graph.Sites.Item.Lists.Item;

namespace Rystem.Content.Infrastructure
{
    internal sealed class SharepointClientWrapper
    {
        public Func<GraphServiceClient> Creator { get; init; }
        public string SiteId { get; init; }
        public string DocumentLibraryId { get; init; }
    }
}
