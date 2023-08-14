using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework
{
    /// <summary>
    /// Business interface that runs before a request for OperationAsync in your repository pattern or query pattern.
    /// </summary>
    /// <typeparam name="T">Model used for your repository.</typeparam>
    /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
    public interface IRepositoryBusinessBeforeOperation<T, TKey> : IRepositoryBusiness, IScannable<IRepositoryBusinessBeforeOperation<T, TKey>>
        where TKey : notnull
    {
        ValueTask<(OperationType<TProperty> Operation, IFilterExpression filter)> BeforeOperationAsync<TProperty>(OperationType<TProperty> operation, IFilterExpression filter, CancellationToken cancellationToken = default);
    }
}
