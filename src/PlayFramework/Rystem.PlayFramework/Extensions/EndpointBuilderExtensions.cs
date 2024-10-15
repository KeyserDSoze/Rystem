using Microsoft.AspNetCore.Mvc;
using Rystem.PlayFramework;

namespace Microsoft.AspNetCore.Builder
{
    public static class EndpointBuilderExtensions
    {
        //todo: migliorare codice per filtro aggiungendo la nuova integrazione openapi
        public static IApplicationBuilder UseAiEndpoints(this IApplicationBuilder app, params string[] policies)
        {
            app.UseAiEndpoints(false, policies);
            return app;
        }
        public static IApplicationBuilder UseAiEndpoints(this IApplicationBuilder app, bool isAuthorized)
        {
            app.UseAiEndpoints(isAuthorized, Array.Empty<string>());
            return app;
        }
        private static IApplicationBuilder UseAiEndpoints(this IApplicationBuilder app, bool authorization, params string[] policies)
        {
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
