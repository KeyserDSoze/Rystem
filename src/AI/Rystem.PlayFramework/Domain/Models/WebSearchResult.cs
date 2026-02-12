namespace Rystem.PlayFramework;

/// <summary>
/// Result of a web search operation.
/// </summary>
public sealed class WebSearchResult
{
    /// <summary>
    /// Search results, ordered by relevance.
    /// </summary>
    public required List<WebSearchDocument> Documents { get; init; }
    
    /// <summary>
    /// Total time taken for the search operation (milliseconds).
    /// </summary>
    public double DurationMs { get; init; }
    
    /// <summary>
    /// Query that was executed (may be transformed from original by provider).
    /// </summary>
    public string? ExecutedQuery { get; init; }
    
    /// <summary>
    /// Total number of results available (for pagination).
    /// </summary>
    public long? TotalCount { get; init; }
    
    /// <summary>
    /// Offset used for this request (pagination).
    /// </summary>
    public int Offset { get; init; }
    
    /// <summary>
    /// Estimated cost for this search operation in USD.
    /// Calculate based on provider's pricing model (usually cost per search).
    /// </summary>
    /// <remarks>
    /// If not provided, PlayFramework will calculate cost using WebSearchCostSettings.
    /// </remarks>
    public decimal? Cost { get; init; }
}
