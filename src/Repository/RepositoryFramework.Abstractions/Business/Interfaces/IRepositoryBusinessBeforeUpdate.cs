namespace RepositoryFramework
{
    /// <summary>
    /// Business interface that runs before a request for UpdateAsync in your repository pattern or command pattern.
    /// </summary>
    /// <typeparam name="T">Model used for your repository.</typeparam>
    /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
    public interface IRepositoryBusinessBeforeUpdate<T, TKey> : IRepositoryBusiness
        where TKey : notnull
    {
        Task<State<T, TKey>> BeforeUpdateAsync(Entity<T, TKey> entity, CancellationToken cancellationToken = default);
    }
}
