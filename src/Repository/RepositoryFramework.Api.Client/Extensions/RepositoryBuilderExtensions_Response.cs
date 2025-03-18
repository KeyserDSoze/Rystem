using RepositoryFramework.Api.Client;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositoryBuilderExtensions
    {
        /// <summary>
        /// Add specific interceptor for your <typeparamref name="T"/> <typeparamref name="TKey"/> client. Interceptor runs after every request.
        /// For example you can add here your response check for unauthorized requests and token refresh.
        /// </summary>
        /// <typeparam name="TInterceptor">Interceptor service</typeparam>
        /// <param name="services">IServiceCollection</param>
        /// <param name="serviceLifetime">Service Lifetime</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddApiClientResponseInterceptor<TInterceptor>(this IServiceCollection services,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TInterceptor : class, IRepositoryResponseClientInterceptor
            => services
                .AddService<IRepositoryResponseClientInterceptor, TInterceptor>(serviceLifetime);
        /// <summary>
        /// Add specific interceptor for your <typeparamref name="T"/> <typeparamref name="TKey"/> client. Interceptor runs after every request.
        /// For example you can add here your response check for unauthorized requests and token refresh.
        /// </summary>
        /// <typeparam name="TInterceptor">Interceptor service.</typeparam>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <param name="serviceLifetime">Service Lifetime.</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddApiClientResponseInterceptor<T, TInterceptor>(
            this IServiceCollection services,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TInterceptor : class, IRepositoryClientResponseInterceptor<T>
        => services
            .AddService<IRepositoryClientResponseInterceptor<T>, TInterceptor>(serviceLifetime);
        /// <summary>
        /// Add specific interceptor for your <typeparamref name="T"/> <typeparamref name="TKey"/> client. Interceptor runs after every request.
        /// For example you can add here your response check for unauthorized requests and token refresh.
        /// </summary>
        /// <typeparam name="TInterceptor">Interceptor service.</typeparam>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="serviceLifetime">Service Lifetime.</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddApiClientResponseInterceptor<T, TKey, TInterceptor>(
            this IServiceCollection services,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TInterceptor : class, IRepositoryResponseClientInterceptor<T, TKey>
            where TKey : notnull
        => services
            .AddService<IRepositoryResponseClientInterceptor<T, TKey>, TInterceptor>(serviceLifetime);
    }
}
