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
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <param name="fallbackBuilder"></param>
        /// <returns></returns>
        public static IServiceCollection AddActionAsFallbackWithServiceProvider<T>(this IServiceCollection services,
            Func<FallbackBuilderForServiceProvider, T> fallbackBuilder)
            where T : class
        {
            services.TryAddTransient<IFactoryFallback<T>>(x =>
            {
                return new ActionFallback<T>(x)
                {
                    BuilderWithServiceProvider = fallbackBuilder
                };
            });
            return services;
        }
    }
}
