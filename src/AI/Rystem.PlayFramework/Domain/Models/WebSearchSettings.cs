namespace Rystem.PlayFramework;

/// <summary>
/// Configuration settings for web search operations.
/// </summary>
public sealed class WebSearchSettings
{
    /// <summary>
    /// Whether web search is enabled for this configuration.
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Maximum number of search results to retrieve.
    /// Default: 10.
    /// </summary>
    public int MaxResults { get; set; } = 10;
    
    /// <summary>
    /// Factory key used to resolve IWebSearchService (can be null/empty for default).
    /// </summary>
    public string? FactoryKey { get; set; }
    
    /// <summary>
    /// Enable safe search to filter explicit content.
    /// Default: true.
    /// </summary>
    public bool SafeSearch { get; set; } = true;
    
    /// <summary>
    /// Market/region for search results (e.g., "en-US", "it-IT", "fr-FR").
    /// Default: "en-US".
    /// </summary>
    public string Market { get; set; } = "en-US";
    
    /// <summary>
    /// Time range filter for search results.
    /// Default: Any (no filtering).
    /// </summary>
    public WebSearchFreshness Freshness { get; set; } = WebSearchFreshness.Any;
    
    /// <summary>
    /// Minimum relevance score threshold (0.0 to 1.0).
    /// Results below this score are filtered out.
    /// Default: null (no filtering).
    /// </summary>
    public double? MinimumScore { get; set; }
    
    /// <summary>
    /// Custom settings for specific implementations (Bing-specific, Google-specific, etc.).
    /// Use this to pass provider-specific configuration.
    /// </summary>
    /// <example>
    /// // Bing-specific: response filters
    /// settings.CustomSettings["responseFilter"] = "Webpages,News";
    /// 
    /// // Google-specific: search type
    /// settings.CustomSettings["searchType"] = "image";
    /// </example>
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}
