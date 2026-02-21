using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace Rystem.PlayFramework;

/// <summary>
/// Manages chat client operations with built-in retry, fallback, and cost calculation.
/// Replaces direct usage of IChatClient with resilience and cost tracking features.
/// </summary>
public interface IChatClientManager
{
    /// <summary>
    /// Gets the primary model ID for this chat client manager.
    /// </summary>
    string? ModelId { get; }

    /// <summary>
    /// Gets the currency used for cost calculations (e.g., "USD", "EUR").
    /// </summary>
    string Currency { get; }

    /// <summary>
    /// Sends a chat request with automatic retry, fallback, and cost calculation.
    /// </summary>
    /// <param name="chatMessages">The conversation messages to send.</param>
    /// <param name="options">Optional chat configuration (tools, temperature, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A ChatResponseWithCost containing the response and calculated costs.</returns>
    Task<ChatResponseWithCost> GetResponseAsync(
        List<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a streaming chat request with automatic retry, fallback, and cost calculation.
    /// Cost is estimated during streaming and finalized in the last update.
    /// </summary>
    /// <param name="chatMessages">The conversation messages to send.</param>
    /// <param name="options">Optional chat configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async stream of ChatUpdateWithCost containing streaming updates and cost estimates.</returns>
    IAsyncEnumerable<ChatUpdateWithCost> GetStreamingResponseAsync(
        List<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default);
}
