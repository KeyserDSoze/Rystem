using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.Dynamics.Dataverse
{
    public interface IRepositoryDataverseBuilder<T, TKey>
        where TKey : notnull
    {
        IRepositoryDataverseBuilder<T, TKey> WithColumn<TProperty>(Expression<Func<T, TProperty>> property,
             string? customPrefix = null);
        IServiceCollection Services { get; }
    }
}
