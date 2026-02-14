namespace Rystem.PlayFramework;

/// <summary>
/// Storage backend for rate limit state.
/// </summary>
public enum RateLimitStorage
{
    /// <summary>
    /// In-memory storage (ConcurrentDictionary).
    /// Fast but not distributed. Lost on restart.
    /// Default implementation provided by PlayFramework.
    /// Best for: Single-instance deployments, development.
    /// </summary>
    InMemory = 0,

    /// <summary>
    /// Custom storage implementation.
    /// User must register their own IRateLimitStorage implementation.
    /// Best for: Redis, SQL Server, or any distributed storage.
    /// </summary>
    Custom = 99
}
