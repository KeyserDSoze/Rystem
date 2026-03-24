namespace Rystem.PlayFramework.Api.Services;

/// <summary>
/// Scoped service that holds fake tenant/user data injected by FakeTenantMiddleware.
/// A new instance is created for every HTTP request.
/// </summary>
public sealed class FakeTenantData : IFakeTenantData
{
    public string? TenantId { get; set; }
    public string? UserId { get; set; }
    public bool WasPopulatedByMiddleware { get; set; }
}
