using Microsoft.Extensions.AI;

namespace Rystem.PlayFramework;

/// <summary>
/// Wraps a streaming chat update with partial cost estimation.
/// Final cost is calculated when streaming completes.
/// </summary>
public sealed class ChatUpdateWithCost
{
    /// <summary>
    /// The streaming update from the LLM.
    /// Null if this is an error update.
    /// </summary>
    public ChatResponseUpdate? Update { get; init; }

    /// <summary>
    /// Estimated cost so far (may be incomplete until streaming finishes).
    /// </summary>
    public decimal EstimatedCost { get; init; }

    /// <summary>
    /// Number of input tokens (available at first chunk).
    /// </summary>
    public int InputTokens { get; init; }

    /// <summary>
    /// Number of output tokens generated so far.
    /// </summary>
    public int OutputTokens { get; init; }

    /// <summary>
    /// Number of cached input tokens (if supported).
    /// </summary>
    public int CachedInputTokens { get; init; }

    /// <summary>
    /// True if this is the final update in the stream.
    /// Cost calculation is complete when this is true.
    /// </summary>
    public bool IsComplete { get; init; }

    /// <summary>
    /// The name of the chat client that processed this request.
    /// </summary>
    public string? ClientName { get; init; }

    /// <summary>
    /// True if this update represents an error.
    /// </summary>
    public bool IsError { get; init; }

    /// <summary>
    /// Error message if IsError is true.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
