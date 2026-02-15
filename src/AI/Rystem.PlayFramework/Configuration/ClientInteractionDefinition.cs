namespace Rystem.PlayFramework.Configuration;

/// <summary>
/// Definition of a client-side tool registered via AddTool().
/// Stored in scene configuration to check and generate ClientInteractionRequest.
/// </summary>
public sealed class ClientInteractionDefinition
{
    /// <summary>
    /// Unique tool name.
    /// </summary>
    public required string ToolName { get; init; }

    /// <summary>
    /// Human-readable description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Maximum execution time in seconds.
    /// </summary>
    public int TimeoutSeconds { get; init; }

    /// <summary>
    /// CLR type of arguments (null for tools without arguments).
    /// Used for validation and deserialization.
    /// </summary>
    public Type? ArgumentsType { get; init; }

    /// <summary>
    /// JSON Schema of arguments (null for tools without arguments).
    /// Sent to LLM so it knows exact parameter structure.
    /// </summary>
    public string? ArgumentsSchema { get; init; }
}
