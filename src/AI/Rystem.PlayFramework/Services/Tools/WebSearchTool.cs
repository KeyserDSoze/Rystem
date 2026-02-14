using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rystem.PlayFramework.Telemetry;
using System.Diagnostics;
using System.Text.Json;

namespace Rystem.PlayFramework;

/// <summary>
/// Tool for performing web searches.
/// Automatically searches the internet for current information.
/// </summary>
internal sealed class WebSearchTool : ISceneTool
{
    private readonly IFactory<IWebSearchService> _webSearchServiceFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly PlayFrameworkSettings _settings;
    private readonly ILogger<WebSearchTool> _logger;
    private readonly string _sceneName;
    private readonly string _factoryKey;

    public WebSearchTool(
        IFactory<IWebSearchService> webSearchServiceFactory,
        IServiceProvider serviceProvider,
        PlayFrameworkSettings settings,
        ILogger<WebSearchTool> logger,
        string sceneName,
        string factoryKey)
    {
        _webSearchServiceFactory = webSearchServiceFactory;
        _serviceProvider = serviceProvider;
        _settings = settings;
        _logger = logger;
        _sceneName = sceneName;
        _factoryKey = factoryKey;
    }

    public string Name => "search_internet";

    public string Description => 
        "Search the web for current information, news, articles, and resources. " +
        "Use this when you need real-time or external information not available in the knowledge base.";

    public AIFunction ToAIFunction()
    {
        // Create a simple function that accepts a search query
        return AIFunctionFactory.Create(
            (string query) => ExecuteAsync(query, default!, default),
            name: Name,
            description: Description);
    }

    public async Task<object?> ExecuteAsync(
        string arguments,
        SceneContext context,
        CancellationToken cancellationToken = default)
    {
        // Create activity for web search
        using var activity = Activity.Current?.Source.Name == PlayFrameworkActivitySource.SourceName
            ? PlayFrameworkActivitySource.Instance.StartActivity(
                "WebSearch.Search",
                ActivityKind.Internal)
            : null;

        var startTime = DateTime.UtcNow;
        var success = false;

        try
        {
            // Resolve settings (Scene > Global)
            var webSearchSettings = ResolveSettings();

            if (!webSearchSettings.Enabled)
            {
                _logger.LogDebug("Web search is disabled for scene {SceneName} with key {FactoryKey}", _sceneName, _factoryKey);
                return "Web search is disabled for this scene";
            }

            // Add telemetry tags
            activity?.SetTag("web_search.factory_key", _factoryKey);
            activity?.SetTag("web_search.max_results", webSearchSettings.MaxResults);
            activity?.SetTag("web_search.market", webSearchSettings.Market);
            activity?.SetTag("web_search.freshness", webSearchSettings.Freshness.ToString());
            activity?.SetTag("web_search.safe_search", webSearchSettings.SafeSearch);

            // Get IWebSearchService from factory or DI
            var webSearchService = GetWebSearchService(webSearchSettings);

            // Build request
            var request = new WebSearchRequest
            {
                Query = arguments,
                Settings = webSearchSettings,
                Offset = 0
            };

            _logger.LogInformation("Executing web search for scene {SceneName}: {Query}", _sceneName, arguments);

            // Execute search
            var result = await webSearchService.SearchAsync(request, cancellationToken);

            // Calculate cost if not provided by IWebSearchService
            decimal calculatedCost = result.Cost ?? CalculateCost(result.Documents.Count);

            success = true;
            activity?.SetStatus(ActivityStatusCode.Ok);
            activity?.SetTag("web_search.results_found", result.Documents.Count);
            activity?.SetTag("web_search.duration_ms", result.DurationMs);
            activity?.SetTag("web_search.cost", (double)calculatedCost);

            if (result.TotalCount.HasValue)
            {
                activity?.SetTag("web_search.total_available", result.TotalCount.Value);
            }

            // Add web search cost to scene's total cost (automatically included in final response)
            if (calculatedCost > 0 && context != null)
            {
                var newTotalCost = context.AddCost(calculatedCost);
                _logger.LogDebug("Added web search cost ${WebSearchCost:F6} to scene {SceneName}. New total: ${TotalCost:F6}",
                    calculatedCost, _sceneName, newTotalCost);
            }

            // Record metrics for observability
            PlayFrameworkMetrics.RecordWebSearch(
                provider: webSearchSettings.FactoryKey ?? "default",
                resultsFound: result.Documents.Count,
                cost: (double)calculatedCost,
                durationMs: result.DurationMs
            );

            _logger.LogInformation(
                "Web search completed for scene {SceneName}. Found {ResultCount} results in {Duration}ms. Cost: ${Cost:F6}",
                _sceneName, result.Documents.Count, result.DurationMs, calculatedCost);

            // Format results for LLM
            return FormatResultsForLlm(result);
        }
        catch (Exception ex)
        {
            success = false;
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            _logger.LogError(ex, "Web search failed for scene {SceneName}", _sceneName);
            throw;
        }
        finally
        {
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

            // Record tool call metric for overall tool tracking
            PlayFrameworkMetrics.RecordToolCall(
                toolName: Name,
                toolType: "WebSearch",
                success: success,
                durationMs: duration
            );
        }
    }

    private WebSearchSettings ResolveSettings()
    {
        // Priority: Scene > Global > Default

        // Try scene-specific settings
        var sceneKey = $"scene:{_sceneName}";
        if (_settings.GlobalWebSearchSettings.TryGetValue(sceneKey, out var sceneSettings))
        {
            if (sceneSettings.FactoryKey == _factoryKey || 
                (string.IsNullOrEmpty(sceneSettings.FactoryKey) && string.IsNullOrEmpty(_factoryKey)))
            {
                return sceneSettings;
            }
        }

        // Try global settings
        if (_settings.GlobalWebSearchSettings.TryGetValue(_factoryKey, out var globalSettings))
        {
            return globalSettings;
        }

        // Default
        return new WebSearchSettings { FactoryKey = _factoryKey };
    }

    private IWebSearchService GetWebSearchService(WebSearchSettings settings)
    {
        var factoryKey = settings.FactoryKey ?? string.Empty;

        try
        {
            // Try factory with key
            var webSearchService = !string.IsNullOrEmpty(factoryKey)
                ? _webSearchServiceFactory.Create(factoryKey)
                : _webSearchServiceFactory.Create(string.Empty);

            if (webSearchService == null)
            {
                ThrowWebSearchServiceNotFoundException(factoryKey);
            }

            return webSearchService!;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            // Factory threw an exception (service not registered)
            ThrowWebSearchServiceNotFoundException(factoryKey, ex);
            return null!; // Never reached
        }
    }

    private void ThrowWebSearchServiceNotFoundException(string factoryKey, Exception? innerException = null)
    {
        var keyDescription = string.IsNullOrEmpty(factoryKey) ? "default (empty key)" : $"'{factoryKey}'";

        var errorMessage = $$"""
            No IWebSearchService registered for factory key {{keyDescription}}.

            🔧 How to fix:

            1️⃣ Register web search service with factory key:

               services.AddWebSearchService<YourWebSearchService>(cost => 
               {
                   cost.CostPerSearch = 0.005m;      // $0.005 per search
                   cost.CostPerResult = 0.0001m;     // $0.0001 per result
               }, name: "{{factoryKey}}");

            2️⃣ Example with Bing Search API:

               services.AddWebSearchService<BingSearchService>(cost =>
               {
                   cost.CostPerSearch = 0.003m;       // Bing pricing
                   cost.MonthlyQuota = 1000;
               }, name: "bing");

            3️⃣ Example with Google Custom Search (using enum):

               services.AddWebSearchService<GoogleSearchService>(cost =>
               {
                   cost.CostPerSearch = 0.005m;       // Google pricing
                   cost.MonthlyQuota = 10000;
               }, name: WebSearchProvider.Google);

            4️⃣ Example with default (no key):

               services.AddWebSearchService<YourWebSearchService>(cost =>
               {
                   cost.CostPerSearch = 0.005m;
               });  // No name parameter = default

            📖 Documentation: https://rystem.net/mcp/tools/content-repository.md
            """;

        _logger.LogError(innerException, 
            "Web search service with factory key {FactoryKey} not found. Scene: {SceneName}", 
            factoryKey, _sceneName);

        throw new InvalidOperationException(errorMessage, innerException);
    }

    private decimal CalculateCost(int resultCount)
    {
        // Get cost settings from configuration
        if (_settings.WebSearchCostSettings.TryGetValue(_factoryKey, out var costSettings))
        {
            return costSettings.CalculateCost(resultCount);
        }

        // Default: $0.005 per search (typical Bing/Google pricing)
        return new WebSearchCostSettings().CalculateCost(resultCount);
    }

    private static string FormatResultsForLlm(WebSearchResult result)
    {
        if (result.Documents.Count == 0)
        {
            return "No web results found for this query.";
        }

        var formatted = $"Found {result.Documents.Count} results from the web";
        
        if (result.TotalCount.HasValue && result.TotalCount.Value > result.Documents.Count)
        {
            formatted += $" (total available: {result.TotalCount.Value:N0})";
        }
        
        formatted += ":\n\n";

        for (int i = 0; i < result.Documents.Count; i++)
        {
            var doc = result.Documents[i];
            
            formatted += $"[Result {i + 1}]";
            
            if (doc.RelevanceScore > 0)
            {
                formatted += $" (Relevance: {doc.RelevanceScore:F2})";
            }
            
            formatted += $"\nTitle: {doc.Title}\n";
            formatted += $"URL: {doc.Url}\n";
            
            if (doc.Domain != null)
            {
                formatted += $"Domain: {doc.Domain}\n";
            }
            
            if (doc.PublishedDate.HasValue)
            {
                formatted += $"Published: {doc.PublishedDate.Value:yyyy-MM-dd}\n";
            }
            
            formatted += $"Snippet: {doc.Snippet}\n";
            
            if (doc.Description != null && doc.Description != doc.Snippet)
            {
                formatted += $"Description: {doc.Description}\n";
            }
            
            formatted += "\n";
        }

        return formatted.TrimEnd();
    }
}
