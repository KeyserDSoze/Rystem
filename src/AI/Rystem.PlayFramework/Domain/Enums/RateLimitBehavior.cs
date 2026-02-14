namespace Rystem.PlayFramework;

/// <summary>
/// Behavior when rate limit is exceeded.
/// </summary>
public enum RateLimitBehavior
{
    /// <summary>
    /// Wait (with timeout) until rate limit resets.
    /// Request blocks until tokens/slots are available.
    /// </summary>
    Wait = 0,

    /// <summary>
    /// Reject immediately with RateLimitExceededException.
    /// Client must retry later.
    /// </summary>
    Reject = 1,

    /// <summary>
    /// Try fallback client chain if available.
    /// Falls back to FallbackChatClientNames.
    /// </summary>
    Fallback = 2
}
