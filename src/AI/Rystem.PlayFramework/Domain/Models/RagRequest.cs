using Microsoft.Extensions.AI;

namespace Rystem.PlayFramework;

/// <summary>
/// Request for RAG search operation.
/// </summary>
public sealed class RagRequest
{
    /// <summary>
    /// User query to search for in the knowledge base.
    /// </summary>
    public required string Query { get; init; }
    
    /// <summary>
    /// Optional: Full conversation history for context-aware retrieval.
    /// </summary>
    public List<ChatMessage>? ConversationHistory { get; init; }
    
    /// <summary>
    /// Settings for this search (merged from global + scene configuration).
    /// </summary>
    public required RagSettings Settings { get; init; }
    
    /// <summary>
    /// Optional: Additional metadata for filtering (e.g., tenant_id, category).
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}
