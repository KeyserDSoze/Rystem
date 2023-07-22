using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.InMemory
{
    public static class RepositoryBuilderExtensions
    {
        /// <summary>
        /// Add an in memory storage to your repository pattern.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="builder"></param>
        /// <param name="behaviorSettings"></param>
        /// <returns>IRepositoryInMemoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IRepositoryInMemoryBuilder<T, TKey> WithInMemory<T, TKey>(
            this IRepositoryBuilder<T, TKey> builder,
            Action<RepositoryBehaviorSettings<T, TKey>>? behaviorSettings = default,
            string? name = null)
            where TKey : notnull
        {
            builder.SetStorageWithOptions<InMemoryStorage<T, TKey>, RepositoryBehaviorSettings<T, TKey>>(
                options =>
                {
                    behaviorSettings?.Invoke(options);
                    CheckSettings(options);
                },
                name,
                ServiceLifetime.Singleton);
            return new RepositoryInMemoryBuilder<T, TKey>(builder.Services);
        }
        /// <summary>
        /// Add an in memory storage to your command pattern.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="builder"></param>
        /// <param name="behaviorSettings"></param>
        /// <returns>IRepositoryInMemoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IRepositoryInMemoryBuilder<T, TKey> WithInMemory<T, TKey>(
            this ICommandBuilder<T, TKey> builder,
            Action<RepositoryBehaviorSettings<T, TKey>>? behaviorSettings = default,
            string? name = null)
            where TKey : notnull
        {
            builder.SetStorageWithOptions<InMemoryStorage<T, TKey>, RepositoryBehaviorSettings<T, TKey>>(
                options =>
                {
                    behaviorSettings?.Invoke(options);
                    CheckSettings(options);
                },
                name,
                ServiceLifetime.Singleton);
            return new RepositoryInMemoryBuilder<T, TKey>(builder.Services);
        }
        /// <summary>
        /// Add an in memory storage to your query pattern.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="builder"></param>
        /// <param name="behaviorSettings"></param>
        /// <returns>IRepositoryInMemoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IRepositoryInMemoryBuilder<T, TKey> WithInMemory<T, TKey>(
            this IQueryBuilder<T, TKey> builder,
            Action<RepositoryBehaviorSettings<T, TKey>>? behaviorSettings = default,
            string? name = null)
            where TKey : notnull
        {
            builder.SetStorageWithOptions<InMemoryStorage<T, TKey>, RepositoryBehaviorSettings<T, TKey>>(
                options =>
                {
                    behaviorSettings?.Invoke(options);
                    CheckSettings(options);
                },
                name,
                ServiceLifetime.Singleton);
            return new RepositoryInMemoryBuilder<T, TKey>(builder.Services);
        }
        private static void CheckSettings<T, TKey>(RepositoryBehaviorSettings<T, TKey> settings)
             where TKey : notnull
        {
            Check(settings.Get(RepositoryMethods.Insert).ExceptionOdds);
            Check(settings.Get(RepositoryMethods.Update).ExceptionOdds);
            Check(settings.Get(RepositoryMethods.Delete).ExceptionOdds);
            Check(settings.Get(RepositoryMethods.Batch).ExceptionOdds);
            Check(settings.Get(RepositoryMethods.Get).ExceptionOdds);
            Check(settings.Get(RepositoryMethods.Query).ExceptionOdds);
            Check(settings.Get(RepositoryMethods.Exist).ExceptionOdds);
            Check(settings.Get(RepositoryMethods.Operation).ExceptionOdds);
            Check(settings.Get(RepositoryMethods.All).ExceptionOdds);

            static void Check(List<ExceptionOdds> odds)
            {
                var total = odds.Sum(x => x.Percentage);
                if (odds.Any(x => x.Percentage <= 0 || x.Percentage > 100))
                {
                    throw new ArgumentException("Some percentages are wrong, greater than 100% or lesser than 0.");
                }
                if (total > 100)
                    throw new ArgumentException("Your total percentage is greater than 100.");
            }
        }
    }
}
