namespace Rystem.PlayFramework;

/// <summary>
/// Result of rate limit check operation.
/// </summary>
public sealed class RateLimitCheckResult
{
    /// <summary>
    /// Whether the request is allowed to proceed.
    /// </summary>
    public bool IsAllowed { get; init; }

    /// <summary>
    /// Time to wait before retrying (if not allowed).
    /// Null if allowed or if behavior is Reject.
    /// </summary>
    public TimeSpan? RetryAfter { get; init; }

    /// <summary>
    /// Remaining tokens/requests in current window.
    /// </summary>
    public int RemainingTokens { get; init; }

    /// <summary>
    /// When the rate limit will reset.
    /// </summary>
    public DateTime ResetTime { get; init; }

    /// <summary>
    /// Rate limit key that was checked.
    /// </summary>
    public string? Key { get; init; }
}
