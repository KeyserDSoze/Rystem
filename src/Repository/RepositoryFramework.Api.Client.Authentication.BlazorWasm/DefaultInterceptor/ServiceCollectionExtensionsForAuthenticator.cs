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
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddDefaultAuthorizationInterceptorForApiHttpClient(
            this IServiceCollection services,
            Action<AuthenticatorSettings>? authenticatorSettings = null)
        {
            return services.AddDefaultAuthorizationInterceptorForApiHttpClient<TokenManager>(
                authenticatorSettings, ServiceLifetime.Scoped);
        }
        /// <summary>
        /// Add JWT specific interceptor for your <typeparamref name="T"/> client. Interceptor runs before every request.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <param name="authenticatorSettings">Settings.</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddDefaultAuthorizationInterceptorForApiHttpClient<T>(
            this IServiceCollection services,
            Action<AuthenticatorSettings<T>>? authenticatorSettings = null)
        {
            return services.AddDefaultAuthorizationInterceptorForApiHttpClient<T, TokenManager>(
                authenticatorSettings, ServiceLifetime.Scoped);
        }
        /// <summary>
        /// Add JWT specific interceptor for your <typeparamref name="T"/> client. Interceptor runs before every request.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="authenticatorSettings">Settings.</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddDefaultAuthorizationInterceptorForApiHttpClient<T, TKey>(
            this IServiceCollection services,
            Action<AuthenticatorSettings<T, TKey>>? authenticatorSettings = null)
            where TKey : notnull
        {
            return services.AddDefaultAuthorizationInterceptorForApiHttpClient<T, TKey, TokenManager>(
                authenticatorSettings, ServiceLifetime.Scoped);
        }
    }
}
