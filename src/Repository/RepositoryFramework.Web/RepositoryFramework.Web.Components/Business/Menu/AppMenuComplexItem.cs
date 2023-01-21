namespace RepositoryFramework.Web.Components
{
    internal sealed class AppMenuComplexItem : IRepositoryAppMenuComplexItem
    {
        public int Index { get; set; }
        /// <summary>
        /// Select icon from https://fonts.google.com/icons?selected=Material+Icons&icon.style=Outlined
        /// </summary>
        public string Icon { get; set; } = "hexagon";
        public string Name { get; set; }
        public List<IRepositoryAppMenuSingleItem> SubMenu { get; set; }
        public List<string> Policies { get; } = new();
    }
}
