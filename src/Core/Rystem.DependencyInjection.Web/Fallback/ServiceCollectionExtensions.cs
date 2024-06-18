using System.Reflection;
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
        /// <summary>
        /// Add an action that works as fallback for your factory for every name you use during Create method in IFactory<T>.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="fallbackBuilder"></param>
        /// <returns></returns>
        public static IServiceCollection AddActionAsFallbackWithServiceCollectionRebuilding(this IServiceCollection services,
            Type serviceType,
            Func<FallbackBuilderForServiceCollection, ValueTask> fallbackBuilder)
        {
            services.AddEngineFactory(serviceType);
            var factoryFallbackInterfaceType = typeof(IFactoryFallback<>).MakeGenericType(serviceType);
            var factoryFallbackType = typeof(ActionFallback<>).MakeGenericType(serviceType);
            var factoryFallback = Activator.CreateInstance(factoryFallbackType) as IActionFallback;
            if (factoryFallback is IActionFallback actionFallback)
            {
                actionFallback.BuilderWithRebuilding = fallbackBuilder;
                services.AddSingleton(factoryFallbackInterfaceType, actionFallback);
            }
            return services;
        }
    }
}
