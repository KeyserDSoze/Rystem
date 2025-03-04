using Rystem.Api.Client;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RystemApiServiceCollectionExtensions
    {
        public static IServiceCollection AddAuthenticationForAllEndpoints(this IServiceCollection services, Action<AuthorizationSettings> settings)
        {
            var options = new AuthorizationSettings();
            settings.Invoke(options);
            services.AddFactory(options, "ApiHttpClient", ServiceLifetime.Singleton);
            services
                .AddEnhancerForAllEndpoints<TokenManager>();
            return services;
        }
        public static IServiceCollection AddAuthenticationForEndpoint<T>(this IServiceCollection services, Action<AuthorizationSettings> settings)
            where T : class
        {
            var options = new AuthorizationSettings();
            settings.Invoke(options);
            services.AddFactory(options, $"ApiHttpClient_{typeof(T).FullName}", ServiceLifetime.Singleton);
            services
                .AddEnhancerForEndpoint<TokenManager<T>, T>();
            return services;
        }
        public static IServiceCollection AddSocialAuthenticationForAllEndpoints(this IServiceCollection services, Action<AuthorizationSettings> settings)
        {
            var options = new AuthorizationSettings();
            settings.Invoke(options);
            services
                .AddEnhancerForAllEndpoints<SocialTokenManager>();
            return services;
        }
        public static IServiceCollection AddSocialAuthenticationForEndpoint<T>(this IServiceCollection services, Action<AuthorizationSettings> settings)
            where T : class
        {
            var options = new AuthorizationSettings();
            settings.Invoke(options);
            services
                .AddEnhancerForEndpoint<SocialTokenManager<T>, T>();
            return services;
        }
    }
}
