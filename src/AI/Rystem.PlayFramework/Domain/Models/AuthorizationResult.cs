namespace Rystem.PlayFramework;

public sealed class AuthorizationResult
{
    public bool IsAuthorized { get; set; }
    public string? Reason { get; set; }

    /// <summary>
    /// User identifier. Null means anonymous/public access.
    /// </summary>
    public string? UserId { get; set; }
}
