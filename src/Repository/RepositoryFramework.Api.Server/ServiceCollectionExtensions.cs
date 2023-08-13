using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;
using RepositoryFramework;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add api interfaces from repository framework. You can add configuration for Swagger, Identity in swagger and documentation.
        /// </summary>
        /// <param name="services">IServiceCollection</param>
        /// <returns>IServiceCollection</returns>
        public static IApiBuilder AddApiFromRepositoryFramework(this IServiceCollection services)
        {
            services
                .AddPopulationService();
            return new ApiBuilder(services);
        }
        /// <summary>
        /// Add examples for your repository or CQRS pattern based on your models.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="services"></param>
        /// <param name="entity"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static IServiceCollection AddRepositoryExample<T, TKey>(
            this IServiceCollection services,
            Entity<T, TKey>? defaultEntity = null)
            where TKey : notnull
        {
            //if (defaultEntity == null)
            //{
            //    services
            //        .TryAddSingleton<IExamplesProvider<Entity<T, TKey>>, ExamplesProvider<Entity<T, TKey>>>();
            //    services
            //        .TryAddSingleton<IExamplesProvider<T>, ExamplesProvider<T>>();
            //    services
            //        .TryAddSingleton<IExamplesProvider<TKey>, ExamplesProvider<TKey>>();
            //    services
            //        .TryAddSingleton<IExamplesProvider<IAsyncEnumerable<Entity<T, TKey>>>,
            //        ExamplesProvider<IEnumerable<Entity<T, TKey>>>>();
            //}
            //else
            //{
            //    services
            //        .TryAddSingleton<IExamplesProvider<Entity<T, TKey>>>(new ExamplesProvider<Entity<T, TKey>>(defaultEntity));
            //    services
            //        .TryAddSingleton<IExamplesProvider<T>>(new ExamplesProvider<T>(defaultEntity.Value));
            //    services
            //        .TryAddSingleton<IExamplesProvider<TKey>>(new ExamplesProvider<TKey>(defaultEntity.Key));
            //}
            
            //services
            //    .TryAddSingleton<IExamplesProvider<T>>(new ExamplesProvider<T>(entity));
            //services
            //    .TryAddSingleton<IExamplesProvider<TKey>>(new ExamplesProvider<TKey>(key));
            return services;
        }
    }
}
