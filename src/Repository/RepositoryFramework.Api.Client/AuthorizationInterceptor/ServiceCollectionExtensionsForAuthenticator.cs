using Microsoft.Extensions.DependencyInjection.Extensions;
using RepositoryFramework.Api.Client;
using RepositoryFramework.Api.Client.Authorization;
using RepositoryFramework.Api.Client.DefaultInterceptor;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositoryBuilderExtensions
    {
        /// <summary>
        /// Add global JWT interceptor for all repository clients. Interceptor runs before every request.
        /// </summary>
        /// <param name="services">IServiceCollection</param>
        /// <param name="serviceLifetime">Service Lifetime</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddDefaultAuthorizationInterceptorForApiHttpClient<TTokenManager>(
            this IServiceCollection services,
            Action<AuthenticatorSettings>? authenticatorSettings = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TTokenManager : class, ITokenManager
        {
            var options = new AuthenticatorSettings();
            authenticatorSettings?.Invoke(options);
            services.TryAddSingleton(options);
            services.TryAddService<ITokenManager, TTokenManager>(serviceLifetime);
            return services
                    .TryAddService<IRepositoryClientInterceptor, BearerAuthenticator>(serviceLifetime)
                    .TryAddService<IRepositoryResponseClientInterceptor, BearerAuthenticator>(serviceLifetime);
        }
        /// <summary>
        /// Add JWT specific interceptor for your <typeparamref name="T"/> client. Interceptor runs before every request.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <param name="authenticatorSettings">Settings.</param>
        /// <param name="serviceLifetime">Service Lifetime.</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddDefaultAuthorizationInterceptorForApiHttpClient<T, TTokenManager>(
            this IServiceCollection services,
            Action<AuthenticatorSettings<T>>? authenticatorSettings = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TTokenManager : class, ITokenManager
        {
            var options = new AuthenticatorSettings<T>();
            authenticatorSettings?.Invoke(options);
            services.TryAddSingleton(options);
            services.TryAddService<ITokenManager, TTokenManager>(serviceLifetime);
            return services
               .TryAddService<IRepositoryClientInterceptor<T>, BearerAuthenticator<T>>(serviceLifetime)
               .TryAddService<IRepositoryClientResponseInterceptor<T>, BearerAuthenticator<T>>(serviceLifetime);
        }
        /// <summary>
        /// Add JWT specific interceptor for your <typeparamref name="T"/> client. Interceptor runs before every request.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="authenticatorSettings">Settings.</param>
        /// <param name="serviceLifetime">Service Lifetime.</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddDefaultAuthorizationInterceptorForApiHttpClient<T, TKey, TTokenManager>(
            this IServiceCollection services,
            Action<AuthenticatorSettings<T, TKey>>? authenticatorSettings = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TTokenManager : class, ITokenManager
            where TKey : notnull
        {
            var options = new AuthenticatorSettings<T, TKey>();
            authenticatorSettings?.Invoke(options);
            services.TryAddSingleton(options);
            services.TryAddService<ITokenManager, TTokenManager>(serviceLifetime);
            return services
                .TryAddService<IRepositoryClientInterceptor<T, TKey>, BearerAuthenticator<T, TKey>>(serviceLifetime);
        }
    }
}
