namespace Rystem.PlayFramework;

/// <summary>
/// Token bucket state for rate limiting.
/// </summary>
internal sealed class TokenBucket
{
    /// <summary>
    /// Current number of tokens available.
    /// </summary>
    public int Tokens { get; set; }

    /// <summary>
    /// Maximum capacity of the bucket.
    /// </summary>
    public int Capacity { get; init; }

    /// <summary>
    /// Tokens added per second.
    /// </summary>
    public int RefillRate { get; init; }

    /// <summary>
    /// Last time tokens were refilled.
    /// </summary>
    public DateTime LastRefillTime { get; set; } = DateTime.UtcNow;
}
