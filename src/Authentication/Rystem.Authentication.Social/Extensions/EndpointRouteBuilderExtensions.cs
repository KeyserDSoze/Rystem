using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Rystem.Authentication.Social;
using System.Security.Claims;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class EndpointRouteBuilderExtensions
    {
        public static IApplicationBuilder UseSocialLoginEndpoint(this IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseAuthorization();
            if (app is IEndpointRouteBuilder endpointBuilder)
            {
                endpointBuilder.Map("api/Authentication/Social/Token", async (
                    [FromServices] SocialLoginBuilder socialSettings,
                    [FromServices] IHttpClientFactory clientFactory,
                    [FromServices] IClaimsCreator? claimCreator,
                    [FromServices] IFactory<ITokenChecker> tokenCheckerFactory,
                    [FromQuery] ProviderType provider,
                    [FromQuery] string code,
                    CancellationToken cancellationToken) =>
                {
                    string? username = null;
                    var tokenChecker = tokenCheckerFactory.Create(provider.ToString());
                    if (tokenChecker != null)
                    {
                        username = await tokenChecker.CheckTokenAndGetUsernameAsync(clientFactory, socialSettings, code, cancellationToken);
                    }
                    if (!string.IsNullOrWhiteSpace(username))
                    {
                        var claims = claimCreator == null ? new[] { new Claim(ClaimTypes.Name, username) } : await claimCreator.GetClaimsAsync(username, cancellationToken);
                        var claimsPrincipal = new ClaimsPrincipal(
                         new ClaimsIdentity(claims,
                           BearerTokenDefaults.AuthenticationScheme
                         )
                       );
                        return Results.SignIn(claimsPrincipal);
                    }
                    return Results.Unauthorized();
                })
                .WithName("/Social/Token")
                .WithDisplayName("/Social/Token")
                .WithGroupName("Social")
                .WithDescription("Get token from social login.");
                endpointBuilder
                    .Map("api/Authentication/Social/User", async (
                            HttpContext context,
                            [FromServices] ISocialUserProvider? socialUserProvider,
                            CancellationToken cancellationToken) =>
                    {
                        if (context?.User?.Identity?.IsAuthenticated == true)
                        {
                            if (socialUserProvider != null)
                                return Results.Json(await socialUserProvider.GetAsync(context.User.Identity.Name, context.User.Claims, cancellationToken));
                            else
                                return Results.Json(new SocialUser { Username = context.User.Identity.Name });
                        }
                        else
                            return Results.Unauthorized();
                    })
                    .WithName("/Social/User")
                    .WithDisplayName("/Social/User")
                    .WithGroupName("Social")
                    .WithDescription("Get user from social login.")
                    .RequireAuthorization();
            }
            return app;
        }
    }
}
