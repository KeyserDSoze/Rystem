using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;

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
            var clientSecretCredential = new ClientSecretCredential(settings.TenantId, settings.ClientId, settings.ClientSecret);
            var graphClient = new GraphServiceClient(clientSecretCredential, s_scopes);
            settings.SiteId = GetSiteId(graphClient, settings);
            settings.DocumentLibraryId = GetDocumentLibraryId(graphClient, settings);
            if (settings.SiteId == null)
                throw new ArgumentException(nameof(settings.SiteId));
            if (settings.DocumentLibraryId == null)
                throw new ArgumentException(nameof(settings.DocumentLibraryId));
            _containerClientFactories.Add(name ?? string.Empty, new SharepointClientWrapper
            {
                Creator = () =>
                {
                    var clientSecretCredential = new ClientSecretCredential(settings.TenantId, settings.ClientId, settings.ClientSecret);
                    var graphClient = new GraphServiceClient(clientSecretCredential, s_scopes);
                    return graphClient;
                },
                SiteId = settings.SiteId!,
                DocumentLibraryId = settings.DocumentLibraryId!,
            });
            return this;
        }
        private string? GetSiteId(GraphServiceClient graphClient, SharepointConnectionSettings settings)
        {
            if (settings.SiteId == null)
            {
                if (settings.SiteName != null)
                {
                    //it has to create a new sharepoint site
                    //https://learn.microsoft.com/en-us/sharepoint/dev/apis/site-creation-rest#create-a-modern-site
                    var sites = graphClient.Sites.GetAsync((requestConfiguration) =>
                    {
                        requestConfiguration.QueryParameters.Search = settings.SiteName;
                    }).ToResult();
                    if (sites?.Value?.Count > 0)
                        return sites.Value.First().Id;
                }
                else if (settings.SiteId == null)
                {
                    var rootSite = graphClient.Sites["root"].GetAsync().ToResult();
                    return rootSite?.Id;
                }
            }
            return settings.SiteId;
        }
        private string? GetDocumentLibraryId(GraphServiceClient graphClient, SharepointConnectionSettings settings)
        {
            if (settings.DocumentLibraryId == null && settings.DocumentLibraryName != null)
            {
                try
                {
                    var id = GetId();
                    if (id == null)
                        return Create();
                    else
                        return id;
                }
                catch (ODataError)
                {
                    return Create();
                }
            }
            return settings.DocumentLibraryId;

            string? Create()
            {
                var drive = graphClient.Sites[settings.SiteId].Lists.PostAsync(new Microsoft.Graph.Models.List
                {
                    DisplayName = settings.DocumentLibraryName,
                    ListProp = new Microsoft.Graph.Models.ListInfo
                    {
                        Template = "documentLibrary"
                    }
                }).ToResult();
                return GetId();
            }
            string? GetId()
            {
                var drives = graphClient.Sites[settings.SiteId].Drives.GetAsync(x =>
                {
                    x.QueryParameters.Filter = $"name eq '{settings.DocumentLibraryName}'";
                }).ToResult();
                if (drives?.Value?.Count > 0)
                    return drives.Value.FirstOrDefault(x => x.Name == settings.DocumentLibraryName)?.Id;
                return null;
            }

        }
    }
}
