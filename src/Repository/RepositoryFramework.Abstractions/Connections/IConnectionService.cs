namespace RepositoryFramework
{
    public interface IConnectionService<T>
    {
        T GetConnection(string entityName, string? factoryName = null);
    }
}
