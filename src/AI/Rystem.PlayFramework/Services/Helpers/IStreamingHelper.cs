using Microsoft.Extensions.AI;
using Rystem.PlayFramework;

namespace Rystem.PlayFramework.Services.Helpers;

/// <summary>
/// Helper service for processing streaming responses from LLM providers.
/// Handles optimistic streaming with progressive function call detection.
/// </summary>
internal interface IStreamingHelper
{
    /// <summary>
    /// Processes optimistic streaming: streams tokens natively until function calls are detected.
    /// Automatically switches to silent accumulation when function calls appear.
    /// </summary>
    /// <param name="context">Scene execution context.</param>
    /// <param name="conversationMessages">Conversation messages to send to LLM.</param>
    /// <param name="chatOptions">Chat options including tools.</param>
    /// <param name="sceneName">Name of the current scene.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Stream of AI scene responses with final message and accumulated costs.</returns>
    IAsyncEnumerable<StreamingResult> ProcessOptimisticStreamAsync(
        SceneContext context,
        List<ChatMessage> conversationMessages,
        ChatOptions chatOptions,
        string? sceneName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Processes a single streaming chunk from GenerateFinalResponseAsync (Planning/DynamicChaining modes).
    /// Simpler approach that accumulates in context.Properties.
    /// </summary>
    /// <param name="streamChunk">Streaming chunk from IChatClient.</param>
    /// <param name="sceneName">Scene name for context key.</param>
    /// <param name="context">Scene execution context.</param>
    /// <returns>Stream of AI scene responses.</returns>
    IAsyncEnumerable<AiSceneResponse> ProcessChunkAsync(
        ChatResponseUpdate streamChunk,
        string? sceneName,
        SceneContext context);

    /// <summary>
    /// Simulates streaming for non-streaming providers by splitting text word-by-word.
    /// </summary>
    /// <param name="responseMessage">Complete message to stream.</param>
    /// <param name="responseWithCost">Response with cost information.</param>
    /// <param name="sceneName">Name of the current scene.</param>
    /// <param name="context">Scene execution context.</param>
    /// <param name="settings">Request settings for budget checking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Stream of AI scene responses with simulated streaming.</returns>
    IAsyncEnumerable<AiSceneResponse> SimulateStreamAsync(
        ChatMessage responseMessage,
        ChatResponseWithCost responseWithCost,
        string? sceneName,
        SceneContext context,
        SceneRequestSettings settings,
        CancellationToken cancellationToken);
}

/// <summary>
/// Result from optimistic streaming processing.
/// Contains final message, accumulated costs, and streaming state.
/// </summary>
internal sealed class StreamingResult
{
    /// <summary>
    /// Final chat message with text and function calls.
    /// </summary>
    public ChatMessage? FinalMessage { get; init; }

    /// <summary>
    /// Accumulated text content from all chunks.
    /// </summary>
    public string AccumulatedText { get; init; } = string.Empty;

    /// <summary>
    /// Accumulated function calls detected during streaming.
    /// </summary>
    public List<FunctionCallContent> FunctionCalls { get; init; } = [];

    /// <summary>
    /// Whether function calls were detected during streaming.
    /// </summary>
    public bool HasFunctionCalls => FunctionCalls.Count > 0;

    /// <summary>
    /// Whether any content was streamed to the user before function calls were detected.
    /// </summary>
    public bool StreamedToUser { get; init; }

    /// <summary>
    /// Total accumulated cost from all chunks.
    /// </summary>
    public decimal? TotalCost { get; init; }

    /// <summary>
    /// Total input tokens consumed.
    /// </summary>
    public int? TotalInputTokens { get; init; }

    /// <summary>
    /// Total output tokens generated.
    /// </summary>
    public int? TotalOutputTokens { get; init; }

    /// <summary>
    /// Total cached input tokens used.
    /// </summary>
    public int? TotalCachedInputTokens { get; init; }
}
