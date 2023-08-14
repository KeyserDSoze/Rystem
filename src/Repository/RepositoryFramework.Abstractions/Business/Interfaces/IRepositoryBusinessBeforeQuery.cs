using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework
{
    /// <summary>
    /// Business interface that runs before a request for QueryAsync in your repository pattern or query pattern.
    /// </summary>
    /// <typeparam name="T">Model used for your repository.</typeparam>
    /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
    public interface IRepositoryBusinessBeforeQuery<T, TKey> : IRepositoryBusiness, IScannable<IRepositoryBusinessBeforeQuery<T, TKey>>
        where TKey : notnull
    {
        Task<IFilterExpression> BeforeQueryAsync(IFilterExpression filter, CancellationToken cancellationToken = default);
    }
}
