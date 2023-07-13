using Microsoft.Extensions.DependencyInjection;

namespace Rystem.Content.Infrastructure
{
    /// <summary>
    /// Please use an App Registration with Permission Type: Application and Permissions: Files.ReadWrite.All or Sites.ReadWrite.All
    /// </summary>
    public class SharepointConnectionSettings : IServiceOptions<SharepointClientWrapper>
    {
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? TenantId { get; set; }
        internal string? SiteId { get; set; }
        internal string? DocumentLibraryId { get; set; }
        internal string? SiteName { get; set; }
        internal string? DocumentLibraryName { get; set; }
        internal bool OnlyDocumentLibrary { get; set; }
        public void MapWithSiteIdAndDocumentLibraryId(string siteId, string documentLibraryId)
        {
            SiteName = null;
            DocumentLibraryName = null;
            SiteId = siteId;
            DocumentLibraryId = documentLibraryId;
            OnlyDocumentLibrary = false;
        }
        public void MapWithSiteIdAndDocumentLibraryName(string siteId, string documentLibraryName)
        {
            SiteName = null;
            DocumentLibraryId = null;
            SiteId = siteId;
            DocumentLibraryName = documentLibraryName;
            OnlyDocumentLibrary = false;
        }
        public void MapWithSiteNameAndDocumentLibraryName(string siteName, string documentLibraryName)
        {
            SiteId = null;
            DocumentLibraryId = null;
            SiteName = siteName;
            DocumentLibraryName = documentLibraryName;
            OnlyDocumentLibrary = false;
        }
        public void MapWithRootSiteAndDocumentLibraryName(string documentLibraryName)
        {
            SiteId = null;
            DocumentLibraryId = null;
            SiteName = null;
            DocumentLibraryName = documentLibraryName;
            OnlyDocumentLibrary = false;
        }
        public void MapOnlyDocumentLibraryId(string documentLibraryId)
        {
            SiteId = null;
            DocumentLibraryName = null;
            SiteName = null;
            DocumentLibraryId = documentLibraryId;
            OnlyDocumentLibrary = true;
        }
        public void MapOnlyDocumentLibraryName(string documentLibraryName)
        {
            SiteId = null;
            DocumentLibraryId = null;
            SiteName = null;
            DocumentLibraryName = documentLibraryName;
            OnlyDocumentLibrary = true;
        }

        public Task<Func<SharepointClientWrapper>> BuildAsync()
            => SharepointServiceClientFactory.GetFunctionAsync(this);
    }
}
