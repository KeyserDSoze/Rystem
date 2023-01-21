using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Population.Random;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Override the population default service.
        /// </summary>
        /// <param name="services">IServiceCollection</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddPopulationService(
          this IServiceCollection services)
        {
            services.AddSingleton<IPopulationService, PopulationService>();
            services.AddSingleton<IInstanceCreator, InstanceCreator>();
            services.AddSingleton<IRegexService, RegexService>();
            services.AddSingleton<IRandomPopulationService, AbstractPopulationService>();
            services.AddSingleton<IRandomPopulationService, ArrayPopulationService>();
            services.AddSingleton<IRandomPopulationService, BoolPopulationService>();
            services.AddSingleton<IRandomPopulationService, BytePopulationService>();
            services.AddSingleton<IRandomPopulationService, CharPopulationService>();
            services.AddSingleton<IRandomPopulationService, ObjectPopulationService>();
            services.AddSingleton<IRandomPopulationService, DictionaryPopulationService>();
            services.AddSingleton<IRandomPopulationService, EnumerablePopulationService>();
            services.AddSingleton<IRandomPopulationService, GuidPopulationService>();
            services.AddSingleton<IRandomPopulationService, NumberPopulationService>();
            services.AddSingleton<IRandomPopulationService, RangePopulationService>();
            services.AddSingleton<IRandomPopulationService, StringPopulationService>();
            services.AddSingleton<IRandomPopulationService, TimePopulationService>();
            services.TryAddSingleton(typeof(IPopulationStrategy<>), typeof(RandomPopulationStrategy<>));
            services.TryAddSingleton(typeof(IPopulation<>), typeof(RandomPopulation<>));
            return services;
        }
        /// <summary>
        /// Override the population strategy default service.
        /// </summary>
        /// <typeparam name="T">Model</typeparam>
        /// <typeparam name="TService">your IPopulationService</typeparam>
        /// <param name="services">IServiceCollection</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddPopulationService<T, TService>(
          this IServiceCollection services)
          where TService : class, IPopulation<T>
          => services.AddSingleton<IPopulation<T>, TService>();
        /// <summary>
        /// Override the population default service.
        /// </summary>
        /// <typeparam name="TService">your IPopulationService</typeparam>
        /// <param name="services">IServiceCollection</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddPopulationService<TService>(
          this IServiceCollection services)
          where TService : class, IPopulationService
          => services.AddSingleton<IPopulationService, TService>();
        /// <summary>
        /// Add specific population settings.
        /// </summary>
        /// <typeparam name="T">Model</typeparam>
        /// <param name="services">IServiceCollection</param>
        /// <returns>IPopulationBuilder<<typeparamref name="T"/>></returns>
        public static IPopulationBuilder<T> AddPopulationSettings<T>(
          this IServiceCollection services)
        {
            var settings = new PopulationSettings<T>();
            services.AddSingleton(settings);
            return new PopulationBuilder<T>(default!, settings);
        }
        /// <summary>
        /// Override the population strategy default service.
        /// </summary>
        /// <typeparam name="T">Model</typeparam>
        /// <typeparam name="TService">your IPopulationService</typeparam>
        /// <param name="services">IServiceCollection</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddPopulationStrategyService<T, TService>(
          this IServiceCollection services)
          where TService : class, IPopulationStrategy<T>
          => services.AddSingleton<IPopulationStrategy<T>, TService>();
        /// <summary>
        /// Override the default instance creator for you population service.
        /// </summary>
        /// <typeparam name="T">your IInstanceCreator</typeparam>
        /// <param name="services">IServiceCollection</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddInstanceCreatorServiceForPopulation<T>(
            this IServiceCollection services)
            where T : class, IInstanceCreator
            => services.AddSingleton<IInstanceCreator, T>();
        /// <summary>
        /// Override the default regular expression service for you population service.
        /// </summary>
        /// <typeparam name="T">your IRegexService</typeparam>
        /// <param name="services">IServiceCollection</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddRegexService<T>(
            this IServiceCollection services)
            where T : class, IRegexService
            => services.AddSingleton<IRegexService, T>();
        /// <summary>
        /// Add a random population service to your population service, you can use Priority property to override default behavior.
        /// For example a service for string random generation already exists with Priority 1,
        /// you may create another string random service with Priority = 2 or greater of 1.
        /// In IsValid method you have to check if type is a string to complete the override.
        /// </summary>
        /// <param name="services">IServiceCollection</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddRandomPopulationService<TRandomPopulationService>(this IServiceCollection services)
            where TRandomPopulationService : class, IRandomPopulationService
        {
            services.AddSingleton<IRandomPopulationService, TRandomPopulationService>();
            return services;
        }
    }
}
