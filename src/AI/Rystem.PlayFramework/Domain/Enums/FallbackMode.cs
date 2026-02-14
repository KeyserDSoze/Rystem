namespace Rystem.PlayFramework;

/// <summary>
/// Defines the strategy for selecting fallback chat clients when primary client fails.
/// </summary>
public enum FallbackMode
{
    /// <summary>
    /// Try clients in the order they are registered.
    /// If client A fails, try B, then C, etc.
    /// </summary>
    Sequential = 0,

    /// <summary>
    /// Distribute requests across clients in round-robin fashion.
    /// Automatically balances load across all available clients.
    /// </summary>
    RoundRobin = 1,

    /// <summary>
    /// Select a random client for each request.
    /// Useful for random load distribution and testing.
    /// </summary>
    Random = 2
}
