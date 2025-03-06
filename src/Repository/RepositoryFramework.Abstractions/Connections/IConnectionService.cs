namespace RepositoryFramework
{
    public interface IConnectionService<T, TKey, TConnection>
        where TKey : notnull
    {
        TConnection GetConnection(string entityName, string? factoryName = null);
    }
}
