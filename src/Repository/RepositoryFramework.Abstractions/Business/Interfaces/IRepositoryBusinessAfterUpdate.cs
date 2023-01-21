namespace RepositoryFramework
{
    /// <summary>
    /// Business interface that runs after a request for UpdateAsync in your repository pattern or command pattern.
    /// </summary>
    /// <typeparam name="T">Model used for your repository.</typeparam>
    /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
    public interface IRepositoryBusinessAfterUpdate<T, TKey> : IRepositoryBusiness
       where TKey : notnull
    {
        Task<State<T, TKey>> AfterUpdateAsync(State<T, TKey> state, Entity<T, TKey> entity, CancellationToken cancellationToken = default);
    }
}
