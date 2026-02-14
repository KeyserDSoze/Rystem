namespace Rystem.PlayFramework;

/// <summary>
/// Load balancing strategy for distributing requests across primary chat clients.
/// </summary>
public enum LoadBalancingMode
{
    /// <summary>
    /// No load balancing - use only the first client.
    /// </summary>
    None = 0,

    /// <summary>
    /// Distribute requests sequentially (client1 → client2 → client3 → client1...).
    /// </summary>
    Sequential = 1,

    /// <summary>
    /// Distribute requests in round-robin fashion (balanced rotation).
    /// </summary>
    RoundRobin = 2,

    /// <summary>
    /// Randomly select a client for each request.
    /// </summary>
    Random = 3
}
