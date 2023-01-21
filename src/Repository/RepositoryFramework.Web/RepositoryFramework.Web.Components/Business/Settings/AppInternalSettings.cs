namespace RepositoryFramework.Web.Components
{
    internal sealed class AppInternalSettings
    {
        public static AppInternalSettings Instance { get; } = new();
        private AppInternalSettings() { }
        public string? RootName { get; set; }
        public bool IsAuthenticated { get; set; }
        public List<string> NotExposableRepositories { get; set; } = new();
        public Dictionary<string, RepositoryAppMenuItem> RepositoryAppMenuItems { get; } = new();
    }
}
