using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Rystem.PlayFramework;

namespace Microsoft.AspNetCore.Builder
{
    public static class EndpointBuilderExtensions
    {
        public static IApplicationBuilder UseAiEndpoints(this IApplicationBuilder app, params string[] policies)
        {
            return app.UseAiEndpoints(false, policies);
        }
        public static IApplicationBuilder UseAiEndpoints(this IApplicationBuilder app, bool isAuthorized)
        {
            return app.UseAiEndpoints(isAuthorized, Array.Empty<string>());
        }
        private static IApplicationBuilder UseAiEndpoints(this IApplicationBuilder app, bool authorization, params string[] policies)
        {
            var actorsOpenAiFilter = app.ApplicationServices.GetRequiredService<ActorsOpenAiEndpointParser>();
            actorsOpenAiFilter.MapOpenAi(app.ApplicationServices);
            app.UseEndpoints(x =>
            {
                var mapped = x.MapGet("api/ai/message",
                    ([FromQuery(Name = "m")] string message,
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
