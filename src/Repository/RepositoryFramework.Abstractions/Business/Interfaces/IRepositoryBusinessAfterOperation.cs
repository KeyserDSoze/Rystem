using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework
{
    /// <summary>
    /// Business interface that runs after a request for OperationAsync in your repository pattern or query pattern.
    /// </summary>
    /// <typeparam name="T">Model used for your repository.</typeparam>
    /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
    public interface IRepositoryBusinessAfterOperation<T, TKey> : IRepositoryBusiness, IScannable<IRepositoryBusinessAfterOperation<T, TKey>>
        where TKey : notnull
    {
        ValueTask<TProperty> AfterOperationAsync<TProperty>(TProperty result, OperationType<TProperty> operation, IFilterExpression filter, CancellationToken cancellationToken = default);
    }
}
