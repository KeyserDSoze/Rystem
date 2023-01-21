namespace RepositoryFramework
{
    /// <summary>
    /// Business interface that runs before a request for BatchAsync in your repository pattern or command pattern.
    /// </summary>
    /// <typeparam name="T">Model used for your repository.</typeparam>
    /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
    public interface IRepositoryBusinessBeforeBatch<T, TKey> : IRepositoryBusiness
        where TKey : notnull
    {
        Task<BatchResults<T, TKey>> BeforeBatchAsync(BatchOperations<T, TKey> operations, CancellationToken cancellationToken = default);
    }
}
