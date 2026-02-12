namespace Rystem.PlayFramework;

/// <summary>
/// Token usage information for RAG operations.
/// Populated by IRagService implementations based on embedding provider usage data.
/// </summary>
/// <example>
/// // Azure OpenAI example
/// var embeddingResponse = await client.GetEmbeddingsAsync(query);
/// var tokenUsage = new RagTokenUsage
/// {
///     EmbeddingTokens = embeddingResponse.Usage.TotalTokens,
///     SearchTokens = 0 // Vector search typically doesn't consume additional tokens
/// };
/// </example>
public sealed class RagTokenUsage
{
    /// <summary>
    /// Tokens consumed for embedding generation (query text → vector).
    /// Example: OpenAI text-embedding-ada-002 charges $0.0001 per 1K tokens.
    /// </summary>
    public int EmbeddingTokens { get; init; }
    
    /// <summary>
    /// Tokens consumed for search operations.
    /// Usually 0 for vector similarity search (no LLM involved).
    /// May be non-zero for hybrid search with semantic ranking.
    /// </summary>
    public int SearchTokens { get; init; }
    
    /// <summary>
    /// Total tokens consumed (EmbeddingTokens + SearchTokens).
    /// </summary>
    public int Total => EmbeddingTokens + SearchTokens;
}
