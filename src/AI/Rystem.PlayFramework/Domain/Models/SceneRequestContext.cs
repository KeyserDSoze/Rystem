namespace Rystem.PlayFramework;

/// <summary>
/// Context for tracking which scenes and tools have been executed.
/// Used to prevent infinite loops.
/// </summary>
public sealed class SceneRequestContext
{
    /// <summary>
    /// Tool name that was executed.
    /// </summary>
    public required string ToolName { get; init; }

    /// <summary>
    /// Serialized arguments passed to the tool.
    /// </summary>
    public string? Arguments { get; init; }

    /// <summary>
    /// Timestamp of execution.
    /// </summary>
    public DateTimeOffset ExecutedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Creates a unique key for this execution.
    /// </summary>
    public string GetKey() => $"{ToolName}:{Arguments ?? "null"}";
}
