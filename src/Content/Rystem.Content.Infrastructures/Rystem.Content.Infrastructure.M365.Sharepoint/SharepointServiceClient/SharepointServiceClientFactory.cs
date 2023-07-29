using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;

namespace Rystem.Content.Infrastructure
{
    internal static class SharepointServiceClientFactory
    {
        private static readonly string[] s_scopes = new string[1] { "https://graph.microsoft.com/.default" };
        public static async Task<Func<IServiceProvider, SharepointClientWrapper>> GetFunctionAsync(SharepointConnectionSettings settings)
        {
            if (settings.TenantId == null)
                throw new ArgumentException(nameof(settings.TenantId));
            if (settings.ClientId == null)
                throw new ArgumentException(nameof(settings.ClientId));
            if (settings.ClientSecret == null)
                throw new ArgumentException(nameof(settings.ClientSecret));
            var clientSecretCredential = new ClientSecretCredential(settings.TenantId, settings.ClientId, settings.ClientSecret);
            var graphClient = new GraphServiceClient(clientSecretCredential, s_scopes);
            if (!settings.OnlyDocumentLibrary)
            {
                settings.SiteId = await GetSiteIdAsync(graphClient, settings).NoContext();
                settings.DocumentLibraryId = await GetDocumentLibraryIdAsync(graphClient, settings).NoContext();
            }
            else
                settings.DocumentLibraryId = await GetDocumentLibraryIdWithoutSiteIdAsync(graphClient, settings).NoContext();
            if (settings.SiteId == null && !settings.OnlyDocumentLibrary)
                throw new ArgumentException(nameof(settings.SiteId));
            if (settings.DocumentLibraryId == null)
                throw new ArgumentException(nameof(settings.DocumentLibraryId));
            return (serviceProvider) => new SharepointClientWrapper
            {
                Creator = () =>
                {
                    var clientSecretCredential = new ClientSecretCredential(settings.TenantId, settings.ClientId, settings.ClientSecret);
                    var graphClient = new GraphServiceClient(clientSecretCredential, s_scopes);
                    return graphClient;
                },
                SiteId = settings.SiteId!,
                DocumentLibraryId = settings.DocumentLibraryId!,
            };
        }
        private static async Task<string?> GetSiteIdAsync(GraphServiceClient graphClient, SharepointConnectionSettings settings)
        {
            if (settings.SiteId == null)
            {
                if (settings.SiteName != null)
                {
                    //it has to create a new sharepoint site
                    //https://learn.microsoft.com/en-us/sharepoint/dev/apis/site-creation-rest#create-a-modern-site
                    var sites = await graphClient.Sites.GetAsync((requestConfiguration) =>
                    {
                        requestConfiguration.QueryParameters.Search = $"\"{settings.SiteName}\"";
                    }).NoContext();
                    if (sites?.Value?.Count > 0)
                        return sites.Value.First().Id;
                }
                else if (settings.SiteId == null)
                {
                    var rootSite = await graphClient.Sites["root"].GetAsync().NoContext();
                    return rootSite?.Id;
                }
            }
            return settings.SiteId;
        }
        private static async Task<string?> GetDocumentLibraryIdWithoutSiteIdAsync(GraphServiceClient graphClient, SharepointConnectionSettings settings)
        {
            if (settings.DocumentLibraryId == null && settings.DocumentLibraryName != null)
            {
                var drives = await graphClient.Drives.GetAsync(x =>
                {
                    x.QueryParameters.Filter = $"name eq '{settings.DocumentLibraryName}'";
                }).NoContext();
                if (drives?.Value?.Count > 0)
                    return drives.Value.FirstOrDefault(x => x.Name == settings.DocumentLibraryName)?.Id;
            }
            return settings.DocumentLibraryId;
        }
        private static async Task<string?> GetDocumentLibraryIdAsync(GraphServiceClient graphClient, SharepointConnectionSettings settings)
        {
            if (settings.DocumentLibraryId == null && settings.DocumentLibraryName != null)
            {
                try
                {
                    var id = await GetIdAsync().NoContext();
                    if (id == null)
                        return await CreateAsync().NoContext();
                    else
                        return id;
                }
                catch (ODataError)
                {
                    return await CreateAsync().NoContext();
                }
            }
            return settings.DocumentLibraryId;

            async Task<string?> CreateAsync()
            {
                _ = await graphClient.Sites[settings.SiteId].Lists.PostAsync(new Microsoft.Graph.Models.List
                {
                    DisplayName = settings.DocumentLibraryName,
                    ListProp = new Microsoft.Graph.Models.ListInfo
                    {
                        Template = "documentLibrary"
                    }
                }).NoContext();
                return await GetIdAsync().NoContext();
            }
            async Task<string?> GetIdAsync()
            {
                var drives = await graphClient.Sites[settings.SiteId].Drives.GetAsync(x =>
                {
                    x.QueryParameters.Filter = $"name eq '{settings.DocumentLibraryName}'";
                }).NoContext();
                if (drives?.Value?.Count > 0)
                    return drives.Value.FirstOrDefault(x => x.Name == settings.DocumentLibraryName)?.Id;
                return null;
            }
        }
    }
}
