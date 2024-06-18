using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFactoryFallback<TService, TFactoryFallback>(this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TFactoryFallback : class, IFactoryFallback<TService>
        {
            services.TryAddService<IFactoryFallback<TService>, TFactoryFallback>(lifetime);
            return services;
        }
        /// <summary>
        /// Add an action that works as fallback for your factory for every name you use during Create method in IFactory<T>.
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="services"></param>
        /// <param name="fallbackBuilder"></param>
        /// <returns></returns>
        public static IServiceCollection AddActionAsFallbackWithServiceProvider<TService>(this IServiceCollection services,
            Func<FallbackBuilderForServiceProvider, TService> fallbackBuilder)
            where TService : class
        {
            services.TryAddTransient<IFactoryFallback<TService>>(x =>
            {
                return new ActionFallback<TService>(x)
                {
                    BuilderWithServiceProvider = fallbackBuilder
                };
            });
            return services;
        }
    }
}
