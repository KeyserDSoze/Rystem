namespace Rystem.PlayFramework;

/// <summary>
/// Exception thrown when rate limit is exceeded.
/// </summary>
public sealed class RateLimitExceededException : InvalidOperationException
{
    public RateLimitExceededException(string message) : base(message)
    {
    }

    public RateLimitExceededException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Time to wait before retrying.
    /// </summary>
    public TimeSpan? RetryAfter { get; init; }

    /// <summary>
    /// Rate limit key that was exceeded.
    /// </summary>
    public string? Key { get; init; }
}
