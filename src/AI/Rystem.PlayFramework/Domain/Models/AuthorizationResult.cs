namespace Rystem.PlayFramework;

public sealed class AuthorizationResult
{
    public bool IsAuthorized { get; set; }
    public string? Reason { get; set; }
}
