using Microsoft.Extensions.AI;

namespace Rystem.PlayFramework;

/// <summary>
/// Result of a client-side tool execution.
/// Client sends this back to server to resume execution.
/// </summary>
public sealed class ClientInteractionResult
{
    /// <summary>
    /// ID of the interaction request this result corresponds to.
    /// Must match the InteractionId from ClientInteractionRequest.
    /// </summary>
    public required string InteractionId { get; init; }

    /// <summary>
    /// Multi-modal contents returned by client tool.
    /// Uses native Microsoft.Extensions.AI types (DataContent, TextContent).
    /// These are directly added to chat history for LLM processing.
    /// </summary>
    public IList<AIContent>? Contents { get; init; }

    /// <summary>
    /// Error message if client tool execution failed.
    /// If set, Contents should be null.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Timestamp when client executed the tool.
    /// </summary>
    public DateTimeOffset ExecutedAt { get; init; } = DateTimeOffset.UtcNow;
}
