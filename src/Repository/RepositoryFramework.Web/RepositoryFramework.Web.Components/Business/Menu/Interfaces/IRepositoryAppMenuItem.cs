namespace RepositoryFramework.Web.Components
{
    public interface IRepositoryModelAppMenuItem : IRepositoryAppMenuComplexItem
    {
        Type KeyType { get; }
        Type ModelType { get; }
    }
}
