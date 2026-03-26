namespace Rystem.PlayFramework;

public sealed class AuthenticationResult
{
    /// <summary>
    /// User identifier resolved from the current request. Null means the layer could not resolve one.
    /// </summary>
    public string? UserId { get; set; }
}
