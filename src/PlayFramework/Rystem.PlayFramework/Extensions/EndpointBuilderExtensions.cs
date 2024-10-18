using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Rystem.PlayFramework;

namespace Microsoft.AspNetCore.Builder
{
    public static class EndpointBuilderExtensions
    {
        //todo: migliorare codice per filtro aggiungendo la nuova integrazione openapi
        public static Task<IApplicationBuilder> UseAiEndpoints(this IApplicationBuilder app, params string[] policies)
        {
            return app.UseAiEndpoints(false, policies);
        }
        public static Task<IApplicationBuilder> UseAiEndpoints(this IApplicationBuilder app, bool isAuthorized)
        {
            return app.UseAiEndpoints(isAuthorized, Array.Empty<string>());
        }
        private static async Task<IApplicationBuilder> UseAiEndpoints(this IApplicationBuilder app, bool authorization, params string[] policies)
        {
            var actorsOpenAiFilter = app.ApplicationServices.GetRequiredService<ActorsOpenAiEndpointReader>();
            await actorsOpenAiFilter.MapOpenAiAsync(app.ApplicationServices);
            app.UseEndpoints(x =>
            {
                var mapped = x.MapGet("api/ai/message", ([FromQuery(Name = "m")] string message,
                    [FromServices] ISceneManager sceneManager,
                    CancellationToken cancellationToken) =>
                {
                    return sceneManager.ExecuteAsync(message, cancellationToken);
                });
                if (policies.Length > 0)
                    mapped.RequireAuthorization(policies);
                else if (authorization)
                    mapped.RequireAuthorization();
            });
            return app;
        }
    }
}
