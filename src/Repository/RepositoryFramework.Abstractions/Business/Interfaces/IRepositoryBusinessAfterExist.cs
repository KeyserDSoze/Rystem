namespace RepositoryFramework
{
    /// <summary>
    /// Business interface that runs after a request for ExistAsync in your repository pattern or query pattern.
    /// </summary>
    /// <typeparam name="T">Model used for your repository.</typeparam>
    /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
    public interface IRepositoryBusinessAfterExist<T, TKey> : IRepositoryBusiness
        where TKey : notnull
    {
        Task<State<T, TKey>> AfterExistAsync(State<T, TKey> response, TKey key, CancellationToken cancellationToken = default);
    }
}
