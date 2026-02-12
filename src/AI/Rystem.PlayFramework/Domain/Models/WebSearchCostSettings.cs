namespace Rystem.PlayFramework;

/// <summary>
/// Cost settings for web search operations.
/// Used to calculate costs when IWebSearchService returns Cost=null.
/// </summary>
public sealed class WebSearchCostSettings
{
    /// <summary>
    /// Fixed cost per search operation (API call) in USD.
    /// Default: $0.005 (typical Bing/Google pricing for 1000 queries/month tier).
    /// </summary>
    /// <example>
    /// Bing Search API v7: $3/1000 queries = $0.003 per search
    /// Google Custom Search: $5/1000 queries = $0.005 per search
    /// SerpApi: ~$0.01 per search (varies by plan)
    /// Brave Search: $0.002 per search
    /// </example>
    public decimal CostPerSearch { get; set; } = 0.005m;
    
    /// <summary>
    /// Optional cost per result returned (if provider charges per result).
    /// Default: $0 (most APIs charge per search, not per result).
    /// </summary>
    public decimal CostPerResult { get; set; } = 0m;
    
    /// <summary>
    /// Monthly quota limit for API calls (if applicable).
    /// Used for tracking and alerting.
    /// Default: null (unlimited).
    /// </summary>
    public int? MonthlyQuota { get; set; }
    
    /// <summary>
    /// Calculates the total cost for a search operation.
    /// </summary>
    /// <param name="resultCount">Number of results returned.</param>
    /// <returns>Total cost in USD.</returns>
    public decimal CalculateCost(int resultCount)
    {
        return CostPerSearch + (resultCount * CostPerResult);
    }
}
