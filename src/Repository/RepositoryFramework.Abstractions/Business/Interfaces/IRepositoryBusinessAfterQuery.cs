using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework
{
    /// <summary>
    /// Business interface that runs after a request for QueryAsync in your repository pattern or query pattern.
    /// </summary>
    /// <typeparam name="T">Model used for your repository.</typeparam>
    /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
    public interface IRepositoryBusinessAfterQuery<T, TKey> : IRepositoryBusiness, IScannable<IRepositoryBusinessAfterQuery<T, TKey>>
        where TKey : notnull
    {
        Task<Entity<T, TKey>> AfterQueryAsync(Entity<T, TKey>? entity, IFilterExpression filter, CancellationToken cancellationToken = default);
    }
}
