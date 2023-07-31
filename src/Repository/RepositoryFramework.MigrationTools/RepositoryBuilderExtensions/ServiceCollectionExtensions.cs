using Microsoft.Extensions.DependencyInjection.Extensions;
using RepositoryFramework;
using RepositoryFramework.Migration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add migration service, inject the IMigrationManager<<typeparamref name="T"/>, <typeparamref name="TKey"/>>
        /// to set up the data migration methods.
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to retrieve, update or delete your data from repository.</typeparam>
        /// <param name="services">IServiceCollection.</param>
        /// <param name="options">Settings for migration.</param>
        /// <param name="name">Factory name.</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddMigrationManager<T, TKey>(
            this IServiceCollection services,
            Action<MigrationOptions<T, TKey>> options,
            string? name = null)
          where TKey : notnull
        {
            var migrationOptions = new MigrationOptions<T, TKey>();
            options.Invoke(migrationOptions);
            migrationOptions.SourceFactoryName ??= string.Empty;
            migrationOptions.DestinationFactoryName ??= string.Empty;
            if (migrationOptions.SourceFactoryName == migrationOptions.DestinationFactoryName)
            {
                throw new ArgumentException("It's not possibile to migrate the same source. You have to set SourceFactoryName and DestinationFactoryName with different values.");
            }
            services
                .AddFactory<IMigrationManager<T, TKey>, MigrationManager<T, TKey>, MigrationOptions<T, TKey>>(
                options,
                name);
            return services;
        }
    }
}
