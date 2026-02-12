namespace Rystem.PlayFramework;

/// <summary>
/// Cost settings for RAG operations (embedding generation + search).
/// Used to calculate costs based on token usage returned by IRagService.
/// </summary>
public sealed class RagCostSettings
{
    /// <summary>
    /// Cost per 1000 embedding tokens in USD.
    /// Default: $0.0001 (OpenAI text-embedding-ada-002 pricing).
    /// </summary>
    /// <example>
    /// OpenAI text-embedding-ada-002: $0.0001 per 1K tokens
    /// OpenAI text-embedding-3-small: $0.00002 per 1K tokens
    /// OpenAI text-embedding-3-large: $0.00013 per 1K tokens
    /// Azure OpenAI: Same as OpenAI
    /// </example>
    public decimal CostPerThousandEmbeddingTokens { get; set; } = 0.0001m;

    /// <summary>
    /// Cost per 1000 search tokens in USD (if applicable).
    /// Default: $0 (most vector search engines don't charge per token).
    /// </summary>
    /// <remarks>
    /// Set this if your RAG provider charges for search operations based on tokens.
    /// Hybrid search with semantic ranking might incur additional costs.
    /// </remarks>
    public decimal CostPerThousandSearchTokens { get; set; } = 0m;

    /// <summary>
    /// Fixed cost per RAG search operation (if applicable).
    /// Default: $0.
    /// </summary>
    /// <remarks>
    /// Some providers charge a fixed fee per search (e.g., Pinecone query cost).
    /// This is added to token-based costs.
    /// </remarks>
    public decimal FixedCostPerSearch { get; set; } = 0m;

    /// <summary>
    /// Calculates the total cost for a RAG operation.
    /// </summary>
    /// <param name="tokenUsage">Token usage information from IRagService.</param>
    /// <returns>Total cost in USD.</returns>
    public decimal CalculateCost(RagTokenUsage? tokenUsage)
    {
        if (tokenUsage == null)
            return FixedCostPerSearch;

        var embeddingCost = (tokenUsage.EmbeddingTokens / 1000m) * CostPerThousandEmbeddingTokens;
        var searchCost = (tokenUsage.SearchTokens / 1000m) * CostPerThousandSearchTokens;

        return embeddingCost + searchCost + FixedCostPerSearch;
    }
}
