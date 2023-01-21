using Microsoft.Extensions.Configuration;

namespace RepositoryFramework
{
    public static class ApiBuilderExtensions
    {
        /// <summary>
        /// Set configuration for Azure Active Directory based on classic configuration in appsettings
        /// "AzureAd": {
        /// "Instance": "https://login.microsoftonline.com/",
        /// "Domain": "your domain",
        /// "TenantId": "usually in secrets",
        /// "ClientId": "usually in secrets",
        /// "ClientSecret": "usually in secrets",
        /// "Scopes": "usually in secrets",
        /// "CallbackPath": "/signin-oidc"
        /// }
        /// </summary>
        /// <param name="configuration">IConfiguration instance.</param>
        public static IPolicyApiBuilder ConfigureAzureActiveDirectory(this IApiBuilder apiBuilder,
            IConfiguration configuration)
        => apiBuilder.WithOpenIdAuthentication(settings =>
            {
                settings.AuthorizationUrl = new Uri($"{configuration["AzureAd:Instance"]}{configuration["AzureAd:TenantId"]}/oauth2/v2.0/authorize");
                settings.TokenUrl = new Uri($"{configuration["AzureAd:Instance"]}{configuration["AzureAd:TenantId"]}/oauth2/v2.0/token");
                settings.ClientId = configuration["AzureAd:ClientId"];
                var scopes = configuration["AzureAd:Scopes"];
                if (!string.IsNullOrEmpty(scopes))
                    settings.Scopes.AddRange(scopes.Split(' ')
                        .Select(x => new ApiIdentityScopeSettings()
                        {
                            Value = x,
                            Description = x
                        }));
            });
    }
}
