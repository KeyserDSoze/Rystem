namespace Rystem.PlayFramework.Api.Services;

/// <summary>
/// IContext implementation that reads from the scoped FakeTenantData service.
/// 
/// *** PUT A BREAKPOINT ON THE RETURN STATEMENT INSIDE RetrieveAsync ***
/// 
/// If _tenantData.WasPopulatedByMiddleware == true  → FakeTenantMiddleware ran before IContext was used ✅
/// If _tenantData.WasPopulatedByMiddleware == false → middleware did NOT run yet (scope/ordering issue) ❌
/// </summary>
public sealed class FakeTenantContext : IContext
{
    private readonly IFakeTenantData _tenantData;

    public FakeTenantContext(IFakeTenantData tenantData)
    {
        _tenantData = tenantData;
    }

    public Task<object?> RetrieveAsync(
        SceneContext context,
        SceneRequestSettings settings,
        CancellationToken cancellationToken)
    {
        // *** BREAKPOINT HERE ***
        // Inspect _tenantData:
        //   _tenantData.WasPopulatedByMiddleware  → should be true if middleware ran
        //   _tenantData.TenantId                  → should be "tenant-demo-001" (or X-Tenant-Id header value)
        //   _tenantData.UserId                    → should be "user-demo-42"   (or X-User-Id header value)
        var snapshot = new
        {
            _tenantData.TenantId,
            _tenantData.UserId,
            _tenantData.WasPopulatedByMiddleware
        };

        // Inject the tenant info as a system context so the LLM is aware of it
        return Task.FromResult((object?)snapshot);
    }
}
