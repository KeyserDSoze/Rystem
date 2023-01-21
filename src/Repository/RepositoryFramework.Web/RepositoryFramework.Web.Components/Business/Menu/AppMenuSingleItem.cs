namespace RepositoryFramework.Web.Components
{
    internal sealed class AppMenuSingleItem : IRepositoryAppMenuSingleItem
    {
        public required string Uri { get; init; }
        public required string Name { get; init; }
        public int Index { get; set; }
        public string Icon { get; set; }
        public List<string> Policies { get; } = new();
    }
}
