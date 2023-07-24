using RepositoryFramework;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add repository framework
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="services">IServiceCollection.</param>
        /// <param name="builder">Builder for your repository.</param>
        /// <returns>IServiceCollection</returns>
        public static async Task<IServiceCollection> AddRepositoryAsync<T, TKey>(this IServiceCollection services,
          Func<IRepositoryBuilder<T, TKey>, ValueTask> builder)
          where TKey : notnull
        {
            var defaultSettings = new RepositoryFrameworkBuilder<T, TKey>();
            await builder.Invoke(defaultSettings).NoContext();
            return services;
        }
        /// <summary>
        /// Add command storage
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="services">IServiceCollection.</param>
        /// <param name="builder">Settings for your repository.</param>
        /// <returns>IServiceCollection</returns>
        public static async Task<IServiceCollection> AddCommandAsync<T, TKey>(this IServiceCollection services,
              Func<ICommandBuilder<T, TKey>, ValueTask> builder)
            where TKey : notnull
        {
            var defaultSettings = new CommandFrameworkBuilder<T, TKey>();
            await builder.Invoke(defaultSettings).NoContext();
            return services;
        }
        /// <summary>
        /// Add query storage
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="services">IServiceCollection.</param>
        /// <param name="builder">Settings for your repository.</param>
        /// <returns>IServiceCollection</returns>
        public static async Task<IServiceCollection> AddQueryAsync<T, TKey>(this IServiceCollection services,
              Func<IQueryBuilder<T, TKey>, ValueTask> builder)
            where TKey : notnull
        {
            var defaultSettings = new QueryFrameworkBuilder<T, TKey>();
            await builder.Invoke(defaultSettings).NoContext();
            return services;
        }
    }
}
