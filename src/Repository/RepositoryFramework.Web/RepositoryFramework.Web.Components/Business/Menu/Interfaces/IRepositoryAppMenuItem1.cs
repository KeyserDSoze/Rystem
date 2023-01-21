namespace RepositoryFramework.Web.Components
{
    public interface IRepositoryAppMenuItem
    {
        int Index { get; }
        /// <summary>
        /// Select icon from https://fonts.google.com/icons?selected=Material+Icons&icon.style=Outlined
        /// </summary>
        string Icon { get; }
        string Name { get; }
        List<string> Policies { get; }
    }
}
