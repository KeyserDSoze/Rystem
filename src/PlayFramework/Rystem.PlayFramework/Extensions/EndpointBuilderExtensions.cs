using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Rystem.PlayFramework;

namespace Microsoft.AspNetCore.Builder
{
    public static class EndpointBuilderExtensions
    {
        private static bool s_firstRequest = true;
        public static IApplicationBuilder UseAiEndpoints(this IApplicationBuilder app)
        {
            app.UseMiddleware<ActorsOpenAiFilterCaller>();
            app.UseEndpoints(x => x.MapGet("api/ai/message",
                      ([FromQuery(Name = "m")] string message,
                       [FromServices] ISceneManager sceneManager,
                       CancellationToken cancellationToken) =>
                {
                    return sceneManager.ExecuteAsync(message, cancellationToken);
                }));
            return app;
        }
    }
}
