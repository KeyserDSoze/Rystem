using Microsoft.OpenApi;
using RepositoryFramework;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.Extensions.DependencyInjection
{
    internal static class SwaggerServiceCollectionExtensions
    {
        public static IServiceCollection AddSwaggerConfigurations(this IServiceCollection services,
            ApiSettings settings)
        {
            services
                .AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                //c.OperationFilter<ExampleProvider>();
                c.SwaggerDoc(settings.Version ?? "v1", new OpenApiInfo { Title = settings.DescriptiveName, Version = settings.Version ?? "v1" });

                if (settings.HasOpenIdAuthentication)
                {
                    c.DocumentFilter<OAuth2SecurityDocumentFilter>(settings);
                }
            });

            return services;
        }
    }

    internal sealed class OAuth2SecurityDocumentFilter : IDocumentFilter
    {
        private readonly ApiSettings _settings;

        public OAuth2SecurityDocumentFilter(ApiSettings settings)
        {
            _settings = settings;
        }

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var openApiOAuthFlow = new OpenApiOAuthFlow()
            {
                AuthorizationUrl = _settings.OpenIdIdentity.AuthorizationUrl,
                TokenUrl = _settings.OpenIdIdentity.TokenUrl,
                Scopes = _settings.OpenIdIdentity.Scopes.ToDictionary(x => x.Value, x => x.Description)
            };

            var schemeId = "oauth2";

            // Add the security scheme at the document level
            swaggerDoc.Components ??= new OpenApiComponents();
            swaggerDoc.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            swaggerDoc.Components.SecuritySchemes[schemeId] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows()
                {
                    AuthorizationCode = openApiOAuthFlow,
                },
                Description = "OAuth2.0 Auth Code with PKCE"
            };

            // Apply it as a requirement for all operations
            foreach (var path in swaggerDoc.Paths.Values)
            {
                foreach (var operation in path.Operations.Values)
                {
                    operation.Security ??= [];
                    operation.Security.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference(schemeId, swaggerDoc)] = _settings.OpenIdIdentity.Scopes.Select(x => x.Value).ToList()
                    });
                }
            }
        }
    }
}
