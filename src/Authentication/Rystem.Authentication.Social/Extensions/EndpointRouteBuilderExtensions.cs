using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Rystem.Authentication.Social;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class EndpointRouteBuilderExtensions
    {
        private const string Origin = nameof(Origin);
        private const string Referer = nameof(Referer);
        public static IApplicationBuilder UseSocialLoginEndpoints(this IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseAuthorization();
            if (app is IEndpointRouteBuilder endpointBuilder)
            {
                endpointBuilder.Map("api/Authentication/Social/Token", async (
                    [FromServices] IHttpContextAccessor httpContextAccessor,
                    [FromServices] ISocialUserProvider? claimProvider,
                    [FromServices] IFactory<ITokenChecker> tokenCheckerFactory,
                    [FromServices] ILogger<ITokenChecker>? logger,
                    [FromQuery] ProviderType provider,
                    [FromQuery] string code,
                    CancellationToken cancellationToken = default) =>
                {
                    TokenResponse? response = null;
                    var tokenChecker = tokenCheckerFactory.Create(provider.ToString());
                    var request = httpContextAccessor?.HttpContext?.Request;
                    Primitives.StringValues possibleDomain = default;
                    if (request?.Headers?.TryGetValue(Origin, out possibleDomain) == false)
                    {
                        _ = request?.Headers?.TryGetValue(Referer, out possibleDomain);
                    }
                    var domain = possibleDomain.ToString().Trim('/');
                    if (tokenChecker != null && !string.IsNullOrWhiteSpace(domain))
                    {
                        try
                        {
                            response = await tokenChecker.CheckTokenAndGetUsernameAsync(code, domain, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            logger?.LogError(ex, "Error checking token.");
                        }
                    }
                    if (response != null && !string.IsNullOrWhiteSpace(response.Username))
                    {
                        var claims = new List<Claim>();
                        if (claimProvider != null)
                        {
                            await foreach (var claim in claimProvider.GetClaimsAsync(response, cancellationToken))
                            {
                                claims.Add(claim);
                            }
                        }
                        else
                            claims.Add(new Claim(ClaimTypes.Name, response.Username));
                        claims.Add(new Claim(SocialClaimTypes.Domain, possibleDomain));
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
                            [FromServices] ILogger<ITokenChecker>? logger,
                            CancellationToken cancellationToken) =>
                    {
                        if (context?.User?.Identity?.IsAuthenticated == true)
                        {
                            try
                            {
                                if (socialUserProvider != null)
                                    return Results.Json(await socialUserProvider.GetAsync(context.User.Identity.Name!, context.User.Claims, cancellationToken));
                                else
                                    return Results.Json(ISocialUser.OnlyUsername(context.User.Identity?.Name));
                            }
                            catch (Exception ex)
                            {
                                logger?.LogError(ex, "Error getting user.");
                            }
                        }
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
