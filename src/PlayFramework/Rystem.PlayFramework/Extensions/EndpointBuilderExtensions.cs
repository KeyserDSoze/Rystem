using Microsoft.AspNetCore.Mvc;
using Rystem.OpenAi.Actors;

namespace Microsoft.AspNetCore.Builder
{
    public static class EndpointBuilderExtensions
    {
        public static IApplicationBuilder UseAiEndpoints(this IApplicationBuilder app)
        {
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
