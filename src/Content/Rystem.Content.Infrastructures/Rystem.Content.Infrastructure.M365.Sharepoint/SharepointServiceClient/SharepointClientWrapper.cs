using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;

namespace Rystem.Content.Infrastructure
{
    public sealed class SharepointClientWrapper : IFactoryOptions
    {
        public Func<GraphServiceClient> Creator { get; init; }
        public string SiteId { get; init; }
        public string DocumentLibraryId { get; init; }
    }
}
