using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Rystem.Authentication.Social;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSocialLogin(this IServiceCollection services,
            Action<SocialLoginBuilder> settings,
            Action<BearerTokenOptions>? action = null)
        {
            services
                .AddAuthentication()
                .AddBearerToken(action!);
            services.AddFactory<ITokenChecker, GoogleTokenChecker>(ProviderType.Google.ToString());
            services.AddFactory<ITokenChecker, MicrosoftTokenChecker>(ProviderType.Microsoft.ToString());
            services.AddFactory<ITokenChecker, FacebookTokenChecker>(ProviderType.Facebook.ToString());
            services.AddFactory<ITokenChecker, DotNetTokenChecker>(ProviderType.DotNet.ToString());
            SocialLoginBuilder builder = new();
            settings(builder);
            services.AddSingleton(builder);
            if (builder.Google.HasValue)
            {
                services.AddHttpClient(Constants.GoogleAuthenticationClient, x =>
                {
                    x.BaseAddress = new Uri("https://oauth2.googleapis.com/token");
                });
            }
            if (builder.Facebook.HasValue)
            {
                services.AddHttpClient(Constants.FacebookAuthenticationClient, x =>
                {
                    x.BaseAddress = new Uri("https://graph.facebook.com/v18.0/me/");
                });
            }
            return services;
        }
        public static IServiceCollection AddSocialLoginStorage<TStorage>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TStorage : class, IClaimsCreator
        {
            services.AddService<IClaimsCreator, TStorage>(lifetime);
            return services;
        }
        public static IServiceCollection AddSocialUserProvider<TProvider>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TProvider : class, ISocialUserProvider
        {
            services.AddService<ISocialUserProvider, TProvider>(lifetime);
            return services;
        }
    }
}
