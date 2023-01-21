namespace RepositoryFramework.Web.Components
{
    public interface IAppMenu
    {
        Dictionary<string, IRepositoryAppMenuItem> Navigations { get; }
    }
}
