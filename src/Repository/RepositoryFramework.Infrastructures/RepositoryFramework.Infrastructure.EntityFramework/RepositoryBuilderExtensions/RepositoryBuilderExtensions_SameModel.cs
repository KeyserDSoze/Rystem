using Microsoft.EntityFrameworkCore;
using RepositoryFramework;
using RepositoryFramework.Infrastructure.EntityFramework;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositoryBuilderExtensions
    {
        /// <summary>
        /// Add a default Entity Framework service for your repository pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <typeparam name="TContext">Specify DB context to use. Please remember to configure it in DI.</typeparam>
        /// <param name="builder">IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="options">Settings for your Entity Framework connection.</param>
        /// <returns>QueryTranslationBuilder</returns>
        public static QueryTranslationBuilder<T, TKey, T, IRepositoryBuilder<T, TKey>> WithEntityFramework<T, TKey, TContext>(
            this IRepositoryBuilder<T, TKey> builder,
            Action<EntityFrameworkOptions<T, TKey, T, TContext>> options)
            where TKey : notnull
            where T : class
            where TContext : DbContext
        {
            _ = builder.WithEntityFramework<T, TKey, T, TContext>(options);
            return builder.Translate<T>().WithSamePorpertiesName();
        }
        /// <summary>
        /// Add a default Entity Framework service for your command pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <typeparam name="TContext">Specify DB context to use. Please remember to configure it in DI.</typeparam>
        /// <param name="builder">ICommandBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="options">Settings for your Entity Framework connection.</param>
        /// <returns>QueryTranslationBuilder</returns>
        public static QueryTranslationBuilder<T, TKey, T, ICommandBuilder<T, TKey>> WithEntityFramework<T, TKey, TContext>(
           this ICommandBuilder<T, TKey> builder,
                Action<EntityFrameworkOptions<T, TKey, T, TContext>> options)
            where TKey : notnull
            where T : class
            where TContext : DbContext
        {
            _ = builder.WithEntityFramework<T, TKey, T, TContext>(options);
            return builder.Translate<T>().WithSamePorpertiesName();
        }
        /// <summary>
        /// Add a default Entity Framework service for your query pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <typeparam name="TContext">Specify DB context to use. Please remember to configure it in DI.</typeparam>
        /// <param name="builder">IQueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="options">Settings for your Entity Framework connection.</param>
        /// <returns>QueryTranslationBuilder</returns>
        public static QueryTranslationBuilder<T, TKey, T, IQueryBuilder<T, TKey>> WithEntityFramework<T, TKey, TContext>(
            this IQueryBuilder<T, TKey> builder,
            Action<EntityFrameworkOptions<T, TKey, T, TContext>> options)
            where TKey : notnull
            where T : class
            where TContext : DbContext
        {
            _ = builder.WithEntityFramework<T, TKey, T, TContext>(options);
            return builder.Translate<T>().WithSamePorpertiesName();
        }
    }
}
