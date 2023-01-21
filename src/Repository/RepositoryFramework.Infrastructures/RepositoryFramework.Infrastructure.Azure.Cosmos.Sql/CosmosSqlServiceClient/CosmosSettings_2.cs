namespace RepositoryFramework.Infrastructure.Azure.Cosmos.Sql
{
    public sealed class CosmosSettings<T, TKey>
    {
        public string ContainerName { get; }
        public string ExistQuery { get; }
        public CosmosSettings(string containerName)
        {
            ContainerName = containerName;
            ExistQuery = $"SELECT * FROM {containerName} x WHERE x.id = @id";
        }
    }
}
