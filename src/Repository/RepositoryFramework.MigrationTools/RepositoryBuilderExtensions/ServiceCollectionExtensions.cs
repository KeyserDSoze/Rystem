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
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddMigrationManager<T, TKey>(
            this IServiceCollection services,
            Action<MigrationOptions<T, TKey>> options)
          where TKey : notnull
        {
            services
                .AddOptions<MigrationOptions<T, TKey>>(MigrationManager<T, TKey>.OptionsKey)
                .Configure(options)
                .PostConfigure(options =>
                {
                    options.SourceFactoryName ??= string.Empty;
                    options.DestinationFactoryName ??= string.Empty;
                })
                .Validate(x => x.SourceFactoryName != x.DestinationFactoryName);
            services
                .TryAddTransient<IMigrationManager<T, TKey>, MigrationManager<T, TKey>>();
            return services;
        }
    }
}
