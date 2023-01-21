using System.Reflection;
using Microsoft.AspNetCore.Builder;
using RepositoryFramework;

namespace Microsoft.Extensions.DependencyInjection
{
    internal static class ApplicationBuilderExtensions
    {
        private const string DefaultVersion = "v1";
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "It's a not configurable uri, it's correct to hardcode it.")]
        public static IApplicationBuilder UseSwaggerUiForRepository<TBuilder>(this TBuilder app,
            ApiSettings settings)
            where TBuilder : IApplicationBuilder
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint($"/swagger/{settings.Version ?? DefaultVersion}/swagger.json", $"{settings.DescriptiveName} {settings.Version ?? DefaultVersion}");
                if (settings.HasOpenIdAuthentication)
                {
                    options.OAuthClientId(settings.OpenIdIdentity.ClientId);
                    options.OAuthAppName(settings.DescriptiveName);
                    options.OAuthScopeSeparator(" ");
                    options.OAuthUsePkce();
                }
                if (settings.HasDocumentation)
                {
                    options.InjectJavascript("https://unpkg.com/rapipdf/dist/rapipdf-min.js");
                    options.HeadContent +=
                        @$"<style type='text/css'>
                            .sidebar{{
                                margin: 0;
                                padding: 0;
                                width: 100%;
                                background-color: #000000;
                                position: fixed;
                                top: 0px;
                                height: 40px;
                                overflow: auto;
                            }}

                           #swagger-ui {{
                                margin-top: 40px;
                                width: 100%;
                            }}
                            </style>
                            <div class='sidebar'>
                                <rapi-pdf id='pdf'
                                    style='width: 200px;
                                    height: 40px;
                                    font - size:18px;'
                                    spec-url = '/swagger/v1/swagger.json'
                                    hide-input = 'true'
                                    button-bg = '#b44646'>
                                </rapi-pdf>
                            </div>";
                }
            });
            return app;
        }
    }
}
