namespace Rystem.PlayFramework;

/// <summary>
/// Response from an actor.
/// </summary>
public sealed class ActorResponse
{
    /// <summary>
    /// System message to add to chat.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Whether this actor should be cached for subsequent calls.
    /// </summary>
    public bool CacheForSubsequentCalls { get; set; }

    /// <summary>
    /// Metadata about this actor's execution.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = [];
}
