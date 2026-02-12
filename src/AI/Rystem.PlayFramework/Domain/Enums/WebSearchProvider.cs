namespace Rystem.PlayFramework;

/// <summary>
/// Web search providers.
/// </summary>
public enum WebSearchProvider
{
    /// <summary>
    /// Microsoft Bing Search API.
    /// </summary>
    Bing = 0,
    
    /// <summary>
    /// Google Custom Search API.
    /// </summary>
    Google = 1,
    
    /// <summary>
    /// DuckDuckGo API.
    /// </summary>
    DuckDuckGo = 2,
    
    /// <summary>
    /// SerpApi (multi-provider aggregator).
    /// </summary>
    SerpApi = 3,
    
    /// <summary>
    /// Brave Search API.
    /// </summary>
    Brave = 4,
    
    /// <summary>
    /// Custom or self-hosted implementation.
    /// </summary>
    Custom = 99
}
