using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add an action that works as fallback for your factory for every name you use during Create method in IFactory<T>.
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="services"></param>
        /// <param name="fallbackBuilder"></param>
        /// <returns></returns>
        public static IServiceCollection AddActionAsFallbackWithServiceCollectionRebuilding<TService>(this IServiceCollection services,
            Func<FallbackBuilderForServiceCollection, ValueTask> fallbackBuilder)
            where TService : class
        {
            services.AddEngineFactory<TService>();
            services.AddSingleton<IFactoryFallback<TService>>(new ActionFallback<TService>
            {
                BuilderWithRebuilding = fallbackBuilder
            });
            return services;
        }
    }
}
