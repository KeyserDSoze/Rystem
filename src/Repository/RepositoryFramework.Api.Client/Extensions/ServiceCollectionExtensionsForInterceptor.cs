using RepositoryFramework.Api.Client;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositoryBuilderExtensions
    {
        /// <summary>
        /// Add global interceptor for all repository clients. Interceptor runs before every request.
        /// For example you can add here your JWT retrieve for authorized requests.
        /// </summary>
        /// <typeparam name="TInterceptor">Interceptor service</typeparam>
        /// <param name="services">IServiceCollection</param>
        /// <param name="serviceLifetime">Service Lifetime</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddApiClientInterceptor<TInterceptor>(this IServiceCollection services,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TInterceptor : class, IRepositoryClientInterceptor
            => services
                .AddService<IRepositoryClientInterceptor, TInterceptor>(serviceLifetime);
        /// <summary>
        /// Add specific interceptor for your <typeparamref name="T"/> client. Interceptor runs before every request.
        /// For example you can add here your JWT retrieve for authorized requests.
        /// </summary>
        /// <typeparam name="TInterceptor">Interceptor service.</typeparam>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <param name="serviceLifetime">Service Lifetime.</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddApiClientSpecificInterceptor<T, TInterceptor>(
            this IServiceCollection services,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TInterceptor : class, IRepositoryClientInterceptor<T>
        => services
            .AddService<IRepositoryClientInterceptor<T>, TInterceptor>(serviceLifetime);
        /// <summary>
        /// Add specific interceptor for your <typeparamref name="T"/> <typeparamref name="TKey"/> client. Interceptor runs before every request.
        /// For example you can add here your JWT retrieve for authorized requests.
        /// </summary>
        /// <typeparam name="TInterceptor">Interceptor service.</typeparam>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="serviceLifetime">Service Lifetime.</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddApiClientSpecificInterceptor<T, TKey, TInterceptor>(
            this IServiceCollection services,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TInterceptor : class, IRepositoryClientInterceptor<T, TKey>
            where TKey : notnull
        => services
            .AddService<IRepositoryClientInterceptor<T, TKey>, TInterceptor>(serviceLifetime);
    }
}
