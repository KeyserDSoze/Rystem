namespace RepositoryFramework
{
    /// <summary>
    /// Business interface that runs after a request for OperationAsync in your repository pattern or query pattern.
    /// </summary>
    /// <typeparam name="T">Model used for your repository.</typeparam>
    /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2326:Unused type parameters should be removed", Justification = "We need T and TKey for dependency injection in the right repository.")]
    public interface IRepositoryBusinessAfterOperation<T, TKey> : IRepositoryBusiness
        where TKey : notnull
    {
        ValueTask<TProperty> AfterOperationAsync<TProperty>(TProperty result, OperationType<TProperty> operation, IFilterExpression filter, CancellationToken cancellationToken = default);
    }
}
