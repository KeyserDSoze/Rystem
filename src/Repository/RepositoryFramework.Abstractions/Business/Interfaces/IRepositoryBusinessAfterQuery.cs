namespace RepositoryFramework
{
    /// <summary>
    /// Business interface that runs after a request for QueryAsync in your repository pattern or query pattern.
    /// </summary>
    /// <typeparam name="T">Model used for your repository.</typeparam>
    /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
    public interface IRepositoryBusinessAfterQuery<T, TKey> : IRepositoryBusiness
        where TKey : notnull
    {
        Task<List<Entity<T, TKey>>> AfterQueryAsync(List<Entity<T, TKey>> entities, IFilterExpression filter, CancellationToken cancellationToken = default);
    }
}
