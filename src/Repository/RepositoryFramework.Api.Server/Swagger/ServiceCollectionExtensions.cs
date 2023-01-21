using Microsoft.OpenApi.Models;
using RepositoryFramework;

namespace Microsoft.Extensions.DependencyInjection
{
    internal static class SwaggerServiceCollectionExtensions
    {
        public static IServiceCollection AddSwaggerConfigurations(this IServiceCollection services,
            ApiSettings settings)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(settings.Version ?? "v1", new OpenApiInfo { Title = settings.DescriptiveName, Version = settings.Version ?? "v1" });
                if (settings.HasOpenIdAuthentication)
                {
                    var openApiOAuthFlow = new OpenApiOAuthFlow()
                    {
                        AuthorizationUrl = settings.OpenIdIdentity.AuthorizationUrl,
                        TokenUrl = settings.OpenIdIdentity.TokenUrl,
                        Scopes = settings.OpenIdIdentity.Scopes.ToDictionary(x => x.Value, x => x.Description)
                    };

                    c.AddSecurityDefinition(SecuritySchemeType.OAuth2.ToString().ToLower(),
                        new OpenApiSecurityScheme
                        {
                            Type = SecuritySchemeType.OAuth2,
                            Name = SecuritySchemeType.OAuth2.ToString().ToLower(),
                            Description = "OAuth2.0 Auth Code with PKCE",
                            Flows = new OpenApiOAuthFlows()
                            {
                                AuthorizationCode = openApiOAuthFlow,
                            },
                        });
                    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
                            },
                            settings.OpenIdIdentity.Scopes.Select(x => x.Value).ToArray()
                        }
                    });
                }
            });
            return services;
        }
    }
}
