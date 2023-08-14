using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework
{
    /// <summary>
    /// Business interface that runs after a request for BatchAsync in your repository pattern or command pattern.
    /// </summary>
    /// <typeparam name="T">Model used for your repository.</typeparam>
    /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
    public interface IRepositoryBusinessAfterBatch<T, TKey> : IRepositoryBusiness, IScannable<IRepositoryBusinessAfterBatch<T, TKey>>
        where TKey : notnull
    {
        Task<BatchResult<T, TKey>> AfterBatchAsync(BatchResult<T, TKey> result, BatchOperations<T, TKey> operations, CancellationToken cancellationToken = default);
    }
}
