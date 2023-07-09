using Azure.Identity;
using Microsoft.Graph;

namespace Rystem.Content.Infrastructure
{
    internal sealed class SharepointServiceClientFactory
    {
        public static SharepointServiceClientFactory Instance { get; } = new();
        private SharepointServiceClientFactory() { }
        private readonly Dictionary<string, SharepointClientWrapper> _containerClientFactories = new();
        public SharepointClientWrapper Get(string name)
            => _containerClientFactories[name];
        public SharepointClientWrapper First()
            => _containerClientFactories.First().Value;
        private static readonly string[] s_scopes = new string[1] { "https://graph.microsoft.com/.default" };
        internal SharepointServiceClientFactory Add(SharepointConnectionSettings settings, string name)
        {
            _containerClientFactories.Add(name ?? string.Empty, new SharepointClientWrapper
            {
                Creator = () =>
                {
                    var clientSecretCredential = new ClientSecretCredential(settings.TenantId, settings.ClientId, settings.ClientSecret);
                    var graphClient = new GraphServiceClient(clientSecretCredential, s_scopes);
                    return graphClient;
                },
                DocumentLibraryId = settings.DocumentLibraryId,
                SiteId = settings.SiteId
            });
            return this;
        }
    }
}
