using RepositoryFramework;
using RepositoryFramework.Infrastructure.Azure.Cosmos.Sql;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositorySettingsExtensions
    {
        /// <summary>
        /// Add a default cosmos sql service for your repository or CQRS pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="settings">IRepositorySettings<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="connectionSettings">Settings for your Cosmos database.</param>
        /// <returns>IRepositoryCosmosSqlBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static async Task<IRepositoryCosmosSqlBuilder<T, TKey>> WithCosmosSqlAsync<T, TKey>(
           this RepositorySettings<T, TKey> settings,
            Action<CosmosSqlConnectionSettings> connectionSettings,
            string? name = null)
            where TKey : notnull
        {
            await settings.SetStorageAsync<CosmosSqlRepository<T, TKey>, CosmosSqlConnectionSettings, CosmosSqlClient>(
                options =>
                {
                    connectionSettings.Invoke(options);
                    options.ModelType = typeof(T);
                }, name, ServiceLifetime.Singleton).NoContext();
            return new RepositoryCosmosSqlBuilder<T, TKey>(settings.Services);
        }
    }
}
