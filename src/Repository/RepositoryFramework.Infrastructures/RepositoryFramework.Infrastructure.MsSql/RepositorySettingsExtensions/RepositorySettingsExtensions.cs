using RepositoryFramework;
using RepositoryFramework.Infrastructure.MsSql;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositorySettingsExtensions
    {
        /// <summary>
        /// Add a default MsSql service for your repository or CQRS pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="services">IServiceCollection</param>
        /// <param name="options">Settings for your MsSql connection.</param>
        /// <param name="settings">Settings for your repository.</param>
        /// <returns>IRepositoryMsSqlBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IRepositoryMsSqlBuilder<T, TKey> WithMsSql<T, TKey>(
           this RepositorySettings<T, TKey> settings,
                Action<MsSqlOptions<T, TKey>> options)
            where TKey : notnull
        {
            options.Invoke(MsSqlOptions<T, TKey>.Instance);
            settings.Services.AddSingleton(MsSqlOptions<T, TKey>.Instance);
            settings.Services.AddWarmUp(serviceProvider => MsSqlCreateTableOrMergeNewColumnsInExistingTableAsync(MsSqlOptions<T, TKey>.Instance));
            settings.SetStorage<SqlRepository<T, TKey>>(ServiceLifetime.Scoped);
            return new RepositoryMsSqlBuilder<T, TKey>(settings.Services);
        }
    }
}
