using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Migration
{
    public interface IMigrationManager<T, TKey>
        where TKey : notnull
    {
        Task<bool> MigrateAsync(Expression<Func<T, TKey>> navigationKey, bool checkIfExists = false, bool deleteEverythingBeforeStart = false, CancellationToken cancellationToken = default);
    }
}
