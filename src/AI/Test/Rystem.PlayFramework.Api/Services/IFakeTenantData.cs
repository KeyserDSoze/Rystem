namespace Rystem.PlayFramework.Api.Services;

/// <summary>
/// Scoped service populated by FakeTenantMiddleware.
/// Used to verify that IContext receives data set by upstream middleware.
/// </summary>
public interface IFakeTenantData
{
    string? TenantId { get; set; }
    string? UserId { get; set; }
    bool WasPopulatedByMiddleware { get; set; }
}
