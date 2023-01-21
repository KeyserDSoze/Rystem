using Microsoft.EntityFrameworkCore;
using RepositoryFramework;
using RepositoryFramework.Infrastructure.EntityFramework;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositorySettingsExtensions
    {
        /// <summary>
        /// Add a default Entity Framework service for your repository or CQRS pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <typeparam name="TContext">Specify DB context to use. Please remember to configure it in DI.</typeparam>
        /// <param name="settings">IRepositorySettings<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="options">Settings for your Entity Framework connection.</param>
        /// <returns>IQueryTranslationBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>, <typeparamref name="T"/>>></returns>
        public static IQueryTranslationBuilder<T, TKey, T> WithEntityFramework<T, TKey, TContext>(
           this RepositorySettings<T, TKey> settings,
                Action<EntityFrameworkOptions<T, TKey, T, TContext>> options)
            where TKey : notnull
            where T : class
            where TContext : DbContext
        {
            _ = settings.WithEntityFramework<T, TKey, T, TContext>(options);
            return settings.Translate<T>().WithSamePorpertiesName();
        }
    }
}
