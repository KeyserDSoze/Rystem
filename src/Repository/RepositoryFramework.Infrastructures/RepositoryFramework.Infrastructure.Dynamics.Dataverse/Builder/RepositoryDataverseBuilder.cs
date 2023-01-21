using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.Dynamics.Dataverse
{
    internal sealed class RepositoryDataverseBuilder<T, TKey> : IRepositoryDataverseBuilder<T, TKey>
        where TKey : notnull
    {
        public IServiceCollection Services { get; }
        public RepositoryDataverseBuilder(IServiceCollection services)
            => Services = services;
        public IRepositoryDataverseBuilder<T, TKey> WithColumn<TProperty>(Expression<Func<T, TProperty>> property,
            string? customPrefix = null)
        {
            var name = property.Body.ToString().Split('.').Last();
            var prop = DataverseOptions<T, TKey>.Instance.Properties.First(x => x.Name == name);
            prop.Prefix = customPrefix;
            return this;
        }
    }
}
