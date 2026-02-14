using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework;

/// <summary>
/// Service for summarizing conversations and managing memory.
/// </summary>
public interface IMemory : IFactoryName
{
    /// <summary>
    /// Summarizes the conversation and creates/updates memory.
    /// Uses ChatClientManager for automatic retry, fallback, and cost tracking.
    /// </summary>
    /// <param name="previousMemory">Previous memory for this conversation (if exists).</param>
    /// <param name="startingMessage">The initial user message that started this execution.</param>
    /// <param name="conversationMessages">All messages exchanged during this execution.</param>
    /// <param name="metadata">Request metadata for context.</param>
    /// <param name="settings">Request settings.</param>
    /// <param name="chatClientManager">Chat client manager to use for summarization (with retry/fallback).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated conversation memory.</returns>
    Task<ConversationMemory> SummarizeAsync(
        ConversationMemory? previousMemory,
        string startingMessage,
        IReadOnlyList<ChatMessage> conversationMessages,
        IReadOnlyDictionary<string, object>? metadata,
        SceneRequestSettings? settings,
        IChatClientManager chatClientManager,
        CancellationToken cancellationToken = default);
}
