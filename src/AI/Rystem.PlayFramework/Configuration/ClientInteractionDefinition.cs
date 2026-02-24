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
    /// For standard tools: timeout for client response.
    /// For commands: timeout for client-side execution (protects against client crashes/hangs).
    /// </summary>
    public int TimeoutSeconds { get; init; }

    /// <summary>
    /// JSON Schema of arguments (null for tools without arguments).
    /// Sent to LLM so it knows exact parameter structure.
    /// </summary>
    public string? JsonSchema { get; init; }
    /// <summary>
    /// JSON Schema of arguments (null for tools without arguments).
    /// Sent to LLM so it knows exact parameter structure.
    /// </summary>
    public Type? ArgumentType { get; init; }

    /// <summary>
    /// Indicates if this is a command (fire-and-forget tool).
    /// Commands don't require immediate response from client.
    /// </summary>
    public bool IsCommand { get; init; } = false;
}
