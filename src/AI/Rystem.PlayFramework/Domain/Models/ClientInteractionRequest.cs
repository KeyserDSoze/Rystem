namespace Rystem.PlayFramework;

/// <summary>
/// Request for executing a tool on the client side.
/// Server sends this to client via AwaitingClient status.
/// </summary>
public sealed class ClientInteractionRequest
{
    /// <summary>
    /// Unique identifier for this interaction request.
    /// Client must return this ID in the result.
    /// </summary>
    public required string InteractionId { get; init; }

    /// <summary>
    /// Name of the tool to execute on client side.
    /// </summary>
    public required string ToolName { get; init; }

    /// <summary>
    /// Tool arguments deserialized from the model T in AddTool&lt;T&gt;().
    /// Client receives this as strongly-typed object.
    /// </summary>
    public Dictionary<string, object?>? Arguments { get; init; }

    /// <summary>
    /// JSON Schema of the arguments generated from AddTool&lt;T&gt;.
    /// Used by LLM to know exact structure of parameters to send.
    /// </summary>
    public string? ArgumentsSchema { get; init; }

    /// <summary>
    /// Human-readable description of what this tool does.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Maximum time in seconds the client has to execute and return result.
    /// After this time, continuation token may expire from cache.
    /// </summary>
    public int TimeoutSeconds { get; init; } = 30;
}
