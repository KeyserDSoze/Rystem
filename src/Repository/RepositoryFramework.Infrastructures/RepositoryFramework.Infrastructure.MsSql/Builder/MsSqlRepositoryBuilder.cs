using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.MsSql
{
    internal sealed class MsSqlRepositoryBuilder<T, TKey> : IMsSqlRepositoryBuilder<T, TKey>, IOptionsToBuild<MsSqlOptions<T, TKey>>
        where TKey : notnull
    {
        public string Schema { get; set; } = "dbo";
        public string TableName { get; set; } = typeof(T).Name;
        public string ConnectionString { get; set; } = null!;
        private readonly MsSqlOptions<T, TKey> _options = new();
        public List<Action<MsSqlOptions<T, TKey>>> ActionsToDoDuringSettingsSetup { get; } = new();
        public IMsSqlRepositoryBuilder<T, TKey> WithPrimaryKey<TProperty>(Expression<Func<T, TProperty>> property, Action<PropertyHelper<T>> value)
        {
            var propertyName = property.Body.ToString().Split('.').Last();
            var prop = _options.Properties.First(x => x.PropertyInfo.Name == propertyName);
            value.Invoke(prop);
            _options.PrimaryKey = prop.ColumnName;
            _options.RefreshColumnNames();
            return this;
        }
        public IMsSqlRepositoryBuilder<T, TKey> WithColumn<TProperty>(Expression<Func<T, TProperty>> property, Action<PropertyHelper<T>> value)
        {
            var propertyName = property.Body.ToString().Split('.').Last();
            var prop = _options.Properties.First(x => x.PropertyInfo.Name == propertyName);
            value.Invoke(prop);
            _options.RefreshColumnNames();
            return this;
        }

        public Func<IServiceProvider, MsSqlOptions<T, TKey>> Build()
        {
            _options.ConnectionString = ConnectionString;
            _options.Schema = Schema;
            _options.TableName = TableName;
            _options.RefreshColumnNames();
            return serviceProvider => _options;
        }
    }
}
