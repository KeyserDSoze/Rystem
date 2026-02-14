using Microsoft.Extensions.AI;

namespace Rystem.PlayFramework;

/// <summary>
/// Wraps a ChatResponse with pre-calculated cost and token usage information.
/// </summary>
public sealed class ChatResponseWithCost
{
    /// <summary>
    /// The original chat response from the LLM.
    /// </summary>
    public required ChatResponse Response { get; init; }

    /// <summary>
    /// The calculated cost of this request in the configured currency.
    /// </summary>
    public decimal CalculatedCost { get; init; }

    /// <summary>
    /// Number of input tokens used in the request.
    /// </summary>
    public int InputTokens { get; init; }

    /// <summary>
    /// Number of output tokens generated in the response.
    /// </summary>
    public int OutputTokens { get; init; }

    /// <summary>
    /// Number of cached input tokens (if supported by the model).
    /// Cached tokens typically cost less than regular input tokens.
    /// </summary>
    public int CachedInputTokens { get; init; }

    /// <summary>
    /// Total tokens (input + output + cached).
    /// </summary>
    public int TotalTokens => InputTokens + OutputTokens + CachedInputTokens;

    /// <summary>
    /// The model ID that generated this response.
    /// </summary>
    public string? ModelId => Response.ModelId;

    /// <summary>
    /// The name of the chat client that processed this request.
    /// Useful for tracking which fallback client was used.
    /// </summary>
    public string? ClientName { get; init; }
}
