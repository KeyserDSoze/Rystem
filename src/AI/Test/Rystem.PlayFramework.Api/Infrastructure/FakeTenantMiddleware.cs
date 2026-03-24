using Rystem.PlayFramework.Api.Services;

namespace Rystem.PlayFramework.Api.Infrastructure;

/// <summary>
/// Middleware that simulates a real multi-tenant middleware populating a scoped service.
/// 
/// Reads X-Tenant-Id and X-User-Id from request headers (or uses defaults).
/// Sets WasPopulatedByMiddleware = true so FakeTenantContext can prove it ran first.
/// 
/// This middleware MUST be registered BEFORE UseEndpoints / MapPlayFramework.
/// </summary>
public sealed class FakeTenantMiddleware
{
    private readonly RequestDelegate _next;

    public FakeTenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext, IFakeTenantData tenantData)
    {
        // Populate the scoped service — simulates what a real tenant middleware would do
        tenantData.TenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault() ?? "tenant-demo-001";
        tenantData.UserId   = httpContext.Request.Headers["X-User-Id"].FirstOrDefault()   ?? "user-demo-42";
        tenantData.WasPopulatedByMiddleware = true;

        await _next(httpContext);
    }
}
