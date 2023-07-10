namespace Rystem.Content.Infrastructure
{
    public class SharepointConnectionSettings
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string TenantId { get; set; }
        internal string? SiteId { get; set; }
        internal string? DocumentLibraryId { get; set; }
        internal string? SiteName { get; set; }
        internal string? DocumentLibraryName { get; set; }
        public void MapWithSiteIdAndDocumentLibraryId(string siteId, string documentLibraryId)
        {
            SiteName = null;
            DocumentLibraryName = null;
            SiteId = siteId;
            DocumentLibraryId = documentLibraryId;
        }
        public void MapWithSiteNameAndDocumentLibraryName(string siteName, string documentLibraryName)
        {
            SiteId = null;
            DocumentLibraryId = null;
            SiteName = siteName;
            DocumentLibraryName = documentLibraryName;
        }
        public void MapWithRootSiteAndDocumentLibraryName(string documentLibraryName)
        {
            SiteId = null;
            DocumentLibraryId = null;
            SiteName = null;
            DocumentLibraryName = documentLibraryName;
        }
    }
}
