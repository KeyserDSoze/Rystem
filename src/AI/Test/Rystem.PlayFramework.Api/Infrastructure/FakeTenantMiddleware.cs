using System.Security.Claims;
using Rystem.PlayFramework.Api.Services;

namespace Rystem.PlayFramework.Api.Infrastructure;

/// <summary>
/// Middleware that simulates a real multi-tenant middleware populating a scoped service.
///
/// Reads X-Tenant-Id and X-User-Id from request headers (or uses defaults).
/// Sets WasPopulatedByMiddleware = true so FakeTenantContext can prove it ran first.
///
/// Also sets a fake ClaimsPrincipal on HttpContext.User so that the repository endpoints
/// (list/get/delete/update conversations) resolve a non-null userId via GetCurrentUserId.
///
/// This middleware MUST be registered BEFORE UseEndpoints / MapPlayFramework.
/// </summary>
public sealed class FakeTenantMiddleware
{
    private const string FakeUserId = "alessandro.rapiti44@gmail.com";

    private readonly RequestDelegate _next;

    public FakeTenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext, IFakeTenantData tenantData)
    {
        // Populate the scoped service — simulates what a real tenant middleware would do
        tenantData.TenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault() ?? "tenant-demo-001";
        tenantData.UserId   = httpContext.Request.Headers["X-User-Id"].FirstOrDefault()   ?? FakeUserId;
        tenantData.WasPopulatedByMiddleware = true;

        // Set a fake authenticated ClaimsPrincipal so that repository endpoints
        // (which call httpContext.User.Identity.Name) resolve the fake user.
        // In production this is replaced by real JWT/cookie auth middleware.
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, tenantData.UserId)],
                authenticationType: "FakeAuth");
            httpContext.User = new ClaimsPrincipal(identity);
        }

        await _next(httpContext);
    }
}
