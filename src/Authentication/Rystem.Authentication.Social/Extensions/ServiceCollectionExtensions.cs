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
            services.AddFactory<ITokenChecker, GithubTokenChecker>(ProviderType.GitHub.ToString());
            services.AddFactory<ITokenChecker, AmazonTokenChecker>(ProviderType.Amazon.ToString());
            services.AddFactory<ITokenChecker, LinkedinTokenChecker>(ProviderType.Linkedin.ToString());
            services.AddFactory<ITokenChecker, XTokenChecker>(ProviderType.X.ToString());
            services.AddFactory<ITokenChecker, InstagreamTokenChecker>(ProviderType.X.ToString());
            services.AddFactory<ITokenChecker, DotNetTokenChecker>(ProviderType.DotNet.ToString());
            SocialLoginBuilder builder = new();
            settings(builder);
            services.AddSingleton(builder);
            if (builder.Google.IsActive)
            {
                services.AddHttpClient(Constants.GoogleAuthenticationClient, x =>
                {
                    x.BaseAddress = new Uri("https://oauth2.googleapis.com/token");
                });
            }
            if (builder.Facebook.IsActive)
            {
                services.AddHttpClient(Constants.FacebookAuthenticationClient, x =>
                {
                    x.BaseAddress = new Uri("https://graph.facebook.com/v18.0/me/");
                });
            }
            if (builder.GitHub.IsActive)
            {
                services.AddHttpClient(Constants.GitHubAuthenticationClient, x =>
                {
                    x.BaseAddress = new Uri("https://github.com/");
                    x.DefaultRequestHeaders.Add("Accept", "application/json");
                });
                services.AddHttpClient(Constants.GitHubAuthenticationClientUser, x =>
                {
                    x.BaseAddress = new Uri("https://api.github.com/user/emails");
                    x.DefaultRequestHeaders.Add("Accept", "application/json");
                    x.DefaultRequestHeaders.Add("User-Agent", "Rystem");
                });
            }
            if (builder.Amazon.IsActive)
            {
                services.AddHttpClient(Constants.AmazonAuthenticationClient, x =>
                {
                    x.BaseAddress = new Uri("https://api.amazon.com/user/profile");
                });
            }
            if (builder.Linkedin.IsActive)
            {
                services.AddHttpClient(Constants.LinkedinAuthenticationClient, x =>
                {
                    x.BaseAddress = new Uri("https://www.linkedin.com/oauth/v2/accessToken");
                });
                services.AddHttpClient(Constants.LinkedinAuthenticationClientUser, x =>
                {
                    x.BaseAddress = new Uri("https://api.linkedin.com/v2/userinfo");
                });
            }
            if (builder.X.IsActive)
            {
                services.AddHttpClient(Constants.XAuthenticationClient, x =>
                {
                    x.BaseAddress = new Uri("https://api.twitter.com/2/");
                });
            }
            if (builder.Instagram.IsActive)
            {
                services.AddHttpClient(Constants.InstagramAuthenticationClient, x =>
                {
                    x.BaseAddress = new Uri("https://api.instagram.com/oauth/access_token");
                });
                services.AddHttpClient(Constants.InstagramAuthenticationClientUser, x =>
                {
                    x.BaseAddress = new Uri("https://graph.instagram.com/me");
                });
            }
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
