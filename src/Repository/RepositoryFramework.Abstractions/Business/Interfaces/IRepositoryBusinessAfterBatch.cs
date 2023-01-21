namespace RepositoryFramework
{
    /// <summary>
    /// Business interface that runs after a request for BatchAsync in your repository pattern or command pattern.
    /// </summary>
    /// <typeparam name="T">Model used for your repository.</typeparam>
    /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
    public interface IRepositoryBusinessAfterBatch<T, TKey> : IRepositoryBusiness
        where TKey : notnull
    {
        Task<BatchResults<T, TKey>> AfterBatchAsync(BatchResults<T, TKey> results, BatchOperations<T, TKey> operations, CancellationToken cancellationToken = default);
    }
}
