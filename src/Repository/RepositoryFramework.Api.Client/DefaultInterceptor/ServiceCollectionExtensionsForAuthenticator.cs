using RepositoryFramework;
using RepositoryFramework.Api.Client;
using RepositoryFramework.Api.Client.Authorization;
using RepositoryFramework.Api.Client.DefaultInterceptor;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositorySettingsExtensions
    {
        /// <summary>
        /// Add global JWT interceptor for all repository clients. Interceptor runs before every request.
        /// </summary>
        /// <param name="services">IServiceCollection</param>
        /// <param name="serviceLifetime">Service Lifetime</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddDefaultAuthorizationInterceptorForApiHttpClient(this IServiceCollection services,
            Action<AuthenticatorSettings>? settings = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        {
            var options = new AuthenticatorSettings();
            settings?.Invoke(options);
            services.AddSingleton(options);
            services.AddScoped<ITokenManager, TokenManager>();
            return services.AddService<IRepositoryClientInterceptor, BearerAuthenticator>(serviceLifetime);
        }
        /// <summary>
        /// Add JWT specific interceptor for your <typeparamref name="T"/> client. Interceptor runs before every request.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="settings">IRepositorySettings<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="authenticatorSettings">Settings.</param>
        /// <param name="serviceLifetime">Service Lifetime.</param>
        /// <returns>IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static RepositorySettings<T, TKey> AddCustomAuthorizationInterceptorForApiHttpClient<T, TKey>(
            this RepositorySettings<T, TKey> settings,
            Action<AuthenticatorSettings<T>>? authenticatorSettings = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TKey : notnull
        {
            var options = new AuthenticatorSettings<T>();
            authenticatorSettings?.Invoke(options);
            settings
                .Services
                .AddService<IRepositoryClientInterceptor<T>, BearerAuthenticator<T>>(serviceLifetime);
            return settings;
        }
    }
}
