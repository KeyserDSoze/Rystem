using Microsoft.Extensions.DependencyInjection;
using RepositoryFramework;

namespace Rystem.Localization
{
    public static class RepositoryFrameworkLocalizationServiceCollectionExtensions
    {
        /// <summary>
        /// Add localization with repository framework.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <param name="repositoryBuilder"></param>
        /// <param name="name"></param>
        /// <param name="storageWarmup"></param>
        /// <returns></returns>
        public static IServiceCollection AddLocalizationWithRepositoryFramework<T>(this IServiceCollection services,
                Action<IRepositoryBuilder<T, string>> repositoryBuilder,
                AnyOf<string?, Enum>? name = null,
                Func<IServiceProvider, Task>? storageWarmup = null)
        {
            services.AddRepository(repositoryBuilder);
            if (storageWarmup != null)
            {
                services.AddWarmUp(storageWarmup);
            }
            services.AddFactory<IRystemLocalizer<T>, RystemLocalizer<T>>(name, ServiceLifetime.Singleton);
            services.AddFactory<ILanguages<T>, Languages<T>>(name, ServiceLifetime.Singleton);
            services.AddWarmUp(async (serviceProvider) =>
            {
                var localizer = (serviceProvider.GetRequiredService<IFactory<ILanguages<T>>>()).Create(name);
                if (localizer is Languages<T> languages)
                    await languages.WarmUpAsync(serviceProvider);
            });
            return services;
        }
        /// <summary>
        /// Add localization with query framework.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <param name="repositoryBuilder"></param>
        /// <param name="name"></param>
        /// <param name="storageWarmup"></param>
        /// <returns></returns>
        public static IServiceCollection AddLocalizationWithQueryFramework<T>(this IServiceCollection services,
                Action<IQueryBuilder<T, string>> repositoryBuilder,
                AnyOf<string?, Enum>? name = null,
                Func<IServiceProvider, Task>? storageWarmup = null)
        {
            services.AddQuery(repositoryBuilder);
            if (storageWarmup != null)
            {
                services.AddWarmUp(storageWarmup);
            }
            services.AddFactory<IRystemLocalizer<T>, RystemLocalizer<T>>(name, ServiceLifetime.Singleton);
            services.AddFactory<ILanguages<T>, Languages<T>>(name, ServiceLifetime.Singleton);
            services.AddWarmUp(async (serviceProvider) =>
            {
                var localizer = (serviceProvider.GetRequiredService<IFactory<ILanguages<T>>>()).Create(name);
                if (localizer is Languages<T> languages)
                    await languages.WarmUpAsync(serviceProvider);
            });
            return services;
        }
    }
}
