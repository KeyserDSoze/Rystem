namespace RepositoryFramework
{
    /// <summary>
    /// Business interface that runs after a request for GetAsync in your repository pattern or query pattern.
    /// </summary>
    /// <typeparam name="T">Model used for your repository.</typeparam>
    /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
    public interface IRepositoryBusinessAfterGet<T, TKey> : IRepositoryBusiness
        where TKey : notnull
    {
        Task<T?> AfterGetAsync(T? value, TKey key, CancellationToken cancellationToken = default);
    }
}
