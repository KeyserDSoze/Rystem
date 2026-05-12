namespace Rystem.PlayFramework;

/// <summary>
/// Carries the HTTP error information when a <see cref="IPlayFrameworkBeforeExecution"/> hook
/// returns <see cref="PlayFrameworkGuardResult.Deny(int, object?)"/>.
/// </summary>
public sealed class PlayFrameworkDenyResult
{
    /// <summary>HTTP status code to return to the client (e.g. 401, 403, 429).</summary>
    public int StatusCode { get; init; }

    /// <summary>Error detail included in the response body. Can be a string or any serializable object.</summary>
    public object? ErrorDetail { get; init; }
}
