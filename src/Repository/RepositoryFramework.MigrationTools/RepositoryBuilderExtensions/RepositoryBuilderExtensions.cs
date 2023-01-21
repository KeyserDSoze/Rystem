using RepositoryFramework;
using RepositoryFramework.Migration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositoryBuilderExtensions
    {
        /// <summary>
        /// Add migration service, inject the IMigrationManager<<typeparamref name="T"/>, <typeparamref name="TKey"/>>
        /// to set up the data migration methods.
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to retrieve, update or delete your data from repository.</typeparam>
        /// <typeparam name="TMigrationSource">Repository pattern for storage that you have to migrate.</typeparam>
        /// <param name="settings">IServiceCollection.</param>
        /// <param name="options">Settings for migration.</param>
        /// <param name="serviceLifetime">Service Lifetime.</param>
        /// <returns>IServiceCollection</returns>
        public static RepositorySettings<T, TKey> AddMigrationSource<T, TKey, TMigrationSource>(
            this RepositorySettings<T, TKey> settings,
            Action<MigrationOptions<T, TKey>> options,
          ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
          where TMigrationSource : class, IMigrationSource<T, TKey>
          where TKey : notnull
        {
            var defaultOptions = new MigrationOptions<T, TKey>();
            options?.Invoke(defaultOptions);
            settings.Services
                .AddSingleton(defaultOptions)
                .AddService<IMigrationSource<T, TKey>, TMigrationSource>(serviceLifetime)
                .AddService<IMigrationManager<T, TKey>, MigrationManager<T, TKey>>(serviceLifetime);
            return settings;
        }
    }
}
