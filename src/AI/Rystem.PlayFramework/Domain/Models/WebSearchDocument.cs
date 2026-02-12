namespace Rystem.PlayFramework;

/// <summary>
/// Single web search result document.
/// </summary>
public sealed class WebSearchDocument
{
    /// <summary>
    /// Title of the web page.
    /// </summary>
    public required string Title { get; init; }
    
    /// <summary>
    /// Full URL of the web page (e.g., "https://example.com/article").
    /// </summary>
    public required string Url { get; init; }
    
    /// <summary>
    /// Text snippet/preview (typically ~200 characters).
    /// </summary>
    public required string Snippet { get; init; }
    
    /// <summary>
    /// Full description (if available from provider).
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// Relevance score from the search engine (0.0 - 1.0).
    /// Higher score = more relevant.
    /// </summary>
    public double RelevanceScore { get; init; }
    
    /// <summary>
    /// Publication date of the content (if available).
    /// </summary>
    public DateTime? PublishedDate { get; init; }
    
    /// <summary>
    /// Domain name extracted from URL (e.g., "example.com").
    /// </summary>
    public string? Domain { get; init; }
    
    /// <summary>
    /// Content language (ISO 639-1 code, e.g., "en", "it").
    /// </summary>
    public string? Language { get; init; }
    
    /// <summary>
    /// Additional metadata (image_url, author, category, etc.).
    /// Provider-specific extensions can be stored here.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}
