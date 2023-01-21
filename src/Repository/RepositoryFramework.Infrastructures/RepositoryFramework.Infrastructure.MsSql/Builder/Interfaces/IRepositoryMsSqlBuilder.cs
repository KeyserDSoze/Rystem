using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.MsSql
{
    public interface IRepositoryMsSqlBuilder<T, TKey>
        where TKey : notnull
    {
        IRepositoryMsSqlBuilder<T, TKey> WithColumn<TProperty>(Expression<Func<T, TProperty>> property, Action<PropertyHelper<T>> value);
        IRepositoryMsSqlBuilder<T, TKey> WithPrimaryKey<TProperty>(Expression<Func<T, TProperty>> property, Action<PropertyHelper<T>> value);
        IServiceCollection Services { get; }
    }
}
