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
        /// <typeparam name="TEntityModel">Model user for your entity framework integration</typeparam>
        /// <typeparam name="TContext">Specify DB context to use. Please remember to configure it in DI.</typeparam>
        /// <param name="settings">IRepositorySettings<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="options">Settings for your Entity Framework connection.</param>
        /// <returns>IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>, IRepository<<typeparamref name="T"/>, <typeparamref name="TKey"/>>>></returns>
        public static IRepositoryBuilder<T, TKey, IRepository<T, TKey>> WithEntityFramework<T, TKey, TEntityModel, TContext>(
           this RepositorySettings<T, TKey> settings,
                Action<EntityFrameworkOptions<T, TKey, TEntityModel, TContext>> options)
            where TKey : notnull
            where TEntityModel : class
            where TContext : DbContext
        {
            options.Invoke(EntityFrameworkOptions<T, TKey, TEntityModel, TContext>.Instance);
            settings.Services.AddSingleton(EntityFrameworkOptions<T, TKey, TEntityModel, TContext>.Instance);
            Check<T, TKey, TEntityModel, TContext>();
            return settings.SetStorage<EntityFrameworkRepository<T, TKey, TEntityModel, TContext>>(ServiceLifetime.Scoped);
        }
        private static void Check<T, TKey, TEntityModel, TContext>()
        where TKey : notnull
        where TEntityModel : class
        where TContext : DbContext
        {
            if (EntityFrameworkOptions<T, TKey, TEntityModel, TContext>.Instance.DbSet == null)
                throw new ArgumentException($"DbSet not configured in option during {nameof(WithEntityFramework)} method for {typeof(TEntityModel).Name} for model {typeof(T).Name} and key {typeof(TKey).Name}");
        }
    }
}
