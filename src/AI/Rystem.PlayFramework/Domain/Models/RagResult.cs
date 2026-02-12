namespace Rystem.PlayFramework;

/// <summary>
/// Result of a RAG search operation.
/// </summary>
public sealed class RagResult
{
    /// <summary>
    /// Documents found by the search, ordered by relevance.
    /// </summary>
    public required List<RagDocument> Documents { get; init; }

    /// <summary>
    /// Total time taken for the search operation (milliseconds).
    /// </summary>
    public double DurationMs { get; init; }

    /// <summary>
    /// Query that was executed (may be transformed from original).
    /// </summary>
    public string? ExecutedQuery { get; init; }

    /// <summary>
    /// Optional: Total number of documents found before TopK filtering.
    /// </summary>
    public int? TotalCount { get; init; }

    /// <summary>
    /// Token usage information for this RAG operation (embedding generation + search).
    /// Populate this from your embedding provider (e.g., Azure OpenAI, OpenAI).
    /// </summary>
    public RagTokenUsage? TokenUsage { get; init; }

    /// <summary>
    /// Estimated cost for this RAG operation in USD.
    /// Calculate based on TokenUsage and your provider's pricing.
    /// </summary>
    public decimal? Cost { get; init; }
}
