using System.Linq.Expressions;

namespace RepositoryFramework.Infrastructure.Dynamics.Dataverse
{
    public interface IDataverseRepositoryBuilder<T, TKey>
        where TKey : notnull
    {
        DataverseOptions<T, TKey> Settings { get; }
        IDataverseRepositoryBuilder<T, TKey> WithColumn<TProperty>(Expression<Func<T, TProperty>> property,
             string? customPrefix = null);
    }
}
