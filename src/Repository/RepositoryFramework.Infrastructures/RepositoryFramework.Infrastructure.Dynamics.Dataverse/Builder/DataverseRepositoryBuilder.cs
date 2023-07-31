using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.Dynamics.Dataverse
{
    internal sealed class DataverseRepositoryBuilder<T, TKey> : IDataverseRepositoryBuilder<T, TKey>, IOptionsBuilder<DataverseClientWrapper<T, TKey>>
        where TKey : notnull
    {
        public DataverseOptions<T, TKey> Settings { get; } = new();
        public IDataverseRepositoryBuilder<T, TKey> WithColumn<TProperty>(Expression<Func<T, TProperty>> property,
            string? customPrefix = null)
        {
            var name = property.Body.ToString().Split('.').Last();
            var prop = Settings.Properties.First(x => x.Name == name);
            prop.Prefix = customPrefix;
            return this;
        }
        public Func<IServiceProvider, DataverseClientWrapper<T, TKey>> Build()
            => (serviceProvider) => new()
            {
                Client = Settings.GetClient(),
                Settings = Settings
            };
    }
}
