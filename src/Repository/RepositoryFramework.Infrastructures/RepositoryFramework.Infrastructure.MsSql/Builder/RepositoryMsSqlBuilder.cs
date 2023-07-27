using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.MsSql
{
    internal sealed class RepositoryMsSqlBuilder<T, TKey> : IRepositoryMsSqlBuilder<T, TKey>
        where TKey : notnull
    {
        public IServiceCollection Services { get; }
        public RepositoryMsSqlBuilder(IServiceCollection services)
            => Services = services;
        public List<Action<MsSqlOptions<T, TKey>>> ActionsToDoDuringSettingsSetup { get; } = new();
        public IRepositoryMsSqlBuilder<T, TKey> WithPrimaryKey<TProperty>(Expression<Func<T, TProperty>> property, Action<PropertyHelper<T>> value)
        {
            ActionsToDoDuringSettingsSetup.Add(options =>
            {
                var propertyName = property.Body.ToString().Split('.').Last();
                var prop = options.Properties.First(x => x.PropertyInfo.Name == propertyName);
                value.Invoke(prop);
                options.PrimaryKey = prop.ColumnName;
                options.RefreshColumnNames();
            });
            return this;
        }
        public IRepositoryMsSqlBuilder<T, TKey> WithColumn<TProperty>(Expression<Func<T, TProperty>> property, Action<PropertyHelper<T>> value)
        {
            ActionsToDoDuringSettingsSetup.Add(options =>
            {
                var propertyName = property.Body.ToString().Split('.').Last();
                var prop = options.Properties.First(x => x.PropertyInfo.Name == propertyName);
                value.Invoke(prop);
                options.RefreshColumnNames();
            });
            return this;
        }
    }
}
