﻿using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework
{
    /// <summary>
    /// Business interface that runs before a request for InsertAsync in your repository pattern or command pattern.
    /// </summary>
    /// <typeparam name="T">Model used for your repository.</typeparam>
    /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
    public interface IRepositoryBusinessBeforeInsert<T, TKey> : IRepositoryBusiness, IScannable<IRepositoryBusinessBeforeInsert<T, TKey>>
        where TKey : notnull
    {
        Task<State<T, TKey>> BeforeInsertAsync(Entity<T, TKey> entity, CancellationToken cancellationToken = default);
    }
}
