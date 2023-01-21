namespace RepositoryFramework.Web.Components
{
    public interface IRepositoryAppMenuComplexItem : IRepositoryAppMenuItem
    {
        List<IRepositoryAppMenuSingleItem> SubMenu { get; }
    }
}
