namespace Rystem.PlayFramework;

/// <summary>
/// Statistics for a rate limit key.
/// </summary>
public sealed class RateLimitStats
{
    /// <summary>
    /// Current usage (tokens consumed, requests made, or concurrent count).
    /// </summary>
    public int CurrentUsage { get; init; }

    /// <summary>
    /// Maximum limit (capacity, max requests, or max concurrent).
    /// </summary>
    public int Limit { get; init; }

    /// <summary>
    /// When the rate limit will reset.
    /// </summary>
    public DateTime ResetTime { get; init; }

    /// <summary>
    /// Refresh interval for the rate limit.
    /// </summary>
    public TimeSpan RefreshInterval { get; init; }

    /// <summary>
    /// Rate limit key.
    /// </summary>
    public string? Key { get; init; }
}
