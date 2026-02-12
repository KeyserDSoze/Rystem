namespace Rystem.PlayFramework;

/// <summary>
/// Request for web search operation.
/// </summary>
public sealed class WebSearchRequest
{
    /// <summary>
    /// Search query string.
    /// </summary>
    public required string Query { get; init; }
    
    /// <summary>
    /// Search settings (max results, filters, market, etc.).
    /// If null, uses scene-specific or global settings.
    /// </summary>
    public WebSearchSettings? Settings { get; init; }
    
    /// <summary>
    /// Optional pagination offset (for retrieving next page of results).
    /// Default: 0 (first page).
    /// </summary>
    public int Offset { get; init; } = 0;
}
