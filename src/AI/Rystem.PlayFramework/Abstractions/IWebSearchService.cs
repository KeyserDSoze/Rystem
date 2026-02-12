namespace Rystem.PlayFramework;

/// <summary>
/// Interface for web search service implementations.
/// Provides real-time web search capabilities (Bing, Google, DuckDuckGo, etc.).
/// </summary>
/// <example>
/// // Bing Search API implementation
/// public class BingSearchService : IWebSearchService
/// {
///     private readonly HttpClient _httpClient;
///     private readonly string _apiKey;
///     private readonly WebSearchCostSettings _costSettings;
///     
///     public async Task&lt;WebSearchResult&gt; SearchAsync(WebSearchRequest request, CancellationToken ct)
///     {
///         var sw = Stopwatch.StartNew();
///         
///         // Build Bing API URL
///         var url = $"https://api.bing.microsoft.com/v7.0/search?q={Uri.EscapeDataString(request.Query)}";
///         url += $"&amp;count={request.Settings?.MaxResults ?? 10}";
///         
///         // Execute search
///         _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _apiKey);
///         var response = await _httpClient.GetAsync(url, ct);
///         var bingResponse = await response.Content.ReadFromJsonAsync&lt;BingSearchResponse&gt;(ct);
///         
///         sw.Stop();
///         
///         // Convert to WebSearchResult
///         var documents = bingResponse.WebPages.Value.Select(page =&gt; new WebSearchDocument
///         {
///             Title = page.Name,
///             Url = page.Url,
///             Snippet = page.Snippet,
///             RelevanceScore = page.Score / 100.0,
///             PublishedDate = page.DatePublished,
///             Domain = new Uri(page.Url).Host
///         }).ToList();
///         
///         // Calculate cost
///         var cost = _costSettings.CalculateCost(documents.Count);
///         
///         return new WebSearchResult
///         {
///             Documents = documents,
///             DurationMs = sw.Elapsed.TotalMilliseconds,
///             ExecutedQuery = request.Query,
///             TotalCount = bingResponse.WebPages.TotalEstimatedMatches,
///             Offset = request.Offset,
///             Cost = cost  // PlayFramework adds this to SceneContext.TotalCost
///         };
///     }
/// }
/// </example>
public interface IWebSearchService
{
    /// <summary>
    /// Searches the web for relevant content.
    /// </summary>
    /// <param name="request">Search request with query and settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Search results with documents and cost information.</returns>
    /// <remarks>
    /// Implementation should:
    /// 1. Execute the search against the web search API (Bing, Google, etc.)
    /// 2. Transform results to WebSearchDocument format
    /// 3. Calculate cost (or let PlayFramework calculate via WebSearchCostSettings)
    /// 4. Return WebSearchResult with documents, duration, and cost
    /// 
    /// Cost tracking:
    /// - If you populate Cost in WebSearchResult, PlayFramework uses that value
    /// - If Cost is null, PlayFramework calculates it using WebSearchCostSettings
    /// - Cost is automatically added to SceneContext.TotalCost
    /// </remarks>
    Task<WebSearchResult> SearchAsync(
        WebSearchRequest request, 
        CancellationToken cancellationToken = default);
}
