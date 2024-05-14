using Microsoft.Identity.Abstractions;
using Rystem.Authentication.Social.Blazor;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSocialLoginUI(this IServiceCollection services,
            Action<SocialLoginAppSettings> settings)
        {
            var options = new SocialLoginAppSettings()
            {
            };
            settings.Invoke(options);
            services.AddSingleton(options);
            services.AddScoped<SocialLoginLocalStorageService>();
            services.AddHttpClient(nameof(SocialLoginManager), x =>
            {
                x.BaseAddress = new Uri(options.ApiUrl!);
            });
            services.AddTransient<SocialLoginManager>();
            services.AddScoped<IAuthorizationHeaderProvider, SocialLoginAuthorizationHeaderProvider>();
            return services;
        }
    }
}
