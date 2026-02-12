using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rystem.PlayFramework.Telemetry;
using System.Diagnostics;
using System.Text.Json;

namespace Rystem.PlayFramework;

/// <summary>
/// Tool for performing RAG (Retrieval-Augmented Generation) searches.
/// Automatically retrieves relevant documents from knowledge base.
/// </summary>
internal sealed class RagTool : ISceneTool
{
    private readonly IFactory<IRagService> _ragServiceFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly PlayFrameworkSettings _settings;
    private readonly ILogger<RagTool> _logger;
    private readonly string _sceneName;
    private readonly string _factoryKey;

    public RagTool(
        IFactory<IRagService> ragServiceFactory,
        IServiceProvider serviceProvider,
        PlayFrameworkSettings settings,
        ILogger<RagTool> logger,
        string sceneName,
        string factoryKey)
    {
        _ragServiceFactory = ragServiceFactory;
        _serviceProvider = serviceProvider;
        _settings = settings;
        _logger = logger;
        _sceneName = sceneName;
        _factoryKey = factoryKey;
    }

    public string Name => "search_knowledge_base";

    public string Description => 
        "Search for relevant information from the knowledge base to answer user questions. " +
        "Use this tool when you need factual information, documentation, or context to provide accurate answers.";

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
        // Create activity for RAG search
        using var activity = Activity.Current?.Source.Name == PlayFrameworkActivitySource.SourceName
            ? PlayFrameworkActivitySource.Instance.StartActivity(
                "RAG.Search",
                ActivityKind.Internal)
            : null;

        var startTime = DateTime.UtcNow;
        var success = false;

        try
        {
            // Resolve settings (Scene > Global)
            var ragSettings = ResolveSettings();

            if (!ragSettings.Enabled)
            {
                _logger.LogDebug("RAG is disabled for scene {SceneName} with key {FactoryKey}", _sceneName, _factoryKey);
                return "RAG is disabled for this scene";
            }

            // Add telemetry tags
            activity?.SetTag("rag.factory_key", _factoryKey);
            activity?.SetTag("rag.top_k", ragSettings.TopK);
            activity?.SetTag("rag.algorithm", ragSettings.SearchAlgorithm.ToString());

            // Get IRagService from factory or DI
            var ragService = GetRagService(ragSettings);

            // Build request
            var request = new RagRequest
            {
                Query = arguments,
                ConversationHistory = null, // Could be extracted from context.Responses if needed
                Settings = ragSettings
            };

            _logger.LogInformation("Executing RAG search for scene {SceneName}: {Query}", _sceneName, arguments);

            // Execute search
            var result = await ragService.SearchAsync(request, cancellationToken);

            // Calculate cost if not provided by IRagService
            decimal calculatedCost = result.Cost ?? CalculateCost(result.TokenUsage);

            success = true;
            activity?.SetStatus(ActivityStatusCode.Ok);
            activity?.SetTag("rag.documents_found", result.Documents.Count);
            activity?.SetTag("rag.duration_ms", result.DurationMs);

            // Add token usage to telemetry if available
            if (result.TokenUsage != null)
            {
                activity?.SetTag("rag.tokens.embedding", result.TokenUsage.EmbeddingTokens);
                activity?.SetTag("rag.tokens.search", result.TokenUsage.SearchTokens);
                activity?.SetTag("rag.tokens.total", result.TokenUsage.Total);
            }

            // Add cost to telemetry
            activity?.SetTag("rag.cost", (double)calculatedCost);

            // Add RAG cost to scene's total cost (automatically included in final response)
            if (calculatedCost > 0 && context != null)
            {
                var newTotalCost = context.AddCost(calculatedCost);
                _logger.LogDebug("Added RAG cost ${RagCost:F6} to scene {SceneName}. New total: ${TotalCost:F6}",
                    calculatedCost, _sceneName, newTotalCost);
            }

            // Record metrics for observability
            PlayFrameworkMetrics.RecordRagSearch(
                provider: ragSettings.FactoryKey ?? "default",
                documentsFound: result.Documents.Count,
                tokens: result.TokenUsage?.Total ?? 0,
                cost: (double)calculatedCost,
                durationMs: result.DurationMs
            );

            _logger.LogInformation(
                "RAG search completed for scene {SceneName}. Found {DocumentCount} documents in {Duration}ms. Tokens: {Tokens}, Cost: ${Cost:F6}",
                _sceneName, result.Documents.Count, result.DurationMs, result.TokenUsage?.Total ?? 0, calculatedCost);

            // Format results for LLM
            return FormatResultsForLlm(result);
        }
        catch (Exception ex)
        {
            success = false;
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            _logger.LogError(ex, "RAG search failed for scene {SceneName}", _sceneName);
            throw;
        }
        finally
        {
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

            // Record tool call metric for overall tool tracking
            PlayFrameworkMetrics.RecordToolCall(
                toolName: Name,
                toolType: "RAG",
                success: success,
                durationMs: duration
            );
        }
    }

    private RagSettings ResolveSettings()
    {
        // Priority: Scene > Global > Default

        // Try scene-specific settings
        var sceneKey = $"scene:{_sceneName}";
        if (_settings.GlobalRagSettings.TryGetValue(sceneKey, out var sceneSettings))
        {
            if (sceneSettings.FactoryKey == _factoryKey || 
                (string.IsNullOrEmpty(sceneSettings.FactoryKey) && string.IsNullOrEmpty(_factoryKey)))
            {
                return sceneSettings;
            }
        }

        // Try global settings
        if (_settings.GlobalRagSettings.TryGetValue(_factoryKey, out var globalSettings))
        {
            return globalSettings;
        }

        // Default
        return new RagSettings { FactoryKey = _factoryKey };
    }

    private IRagService GetRagService(RagSettings settings)
    {
        try
        {
            // Try factory with key
            if (!string.IsNullOrEmpty(settings.FactoryKey))
            {
                return _ragServiceFactory.Create(settings.FactoryKey);
            }

            // Try factory without key (default)
            return _ragServiceFactory.Create();
        }
        catch
        {
            // Fallback to DI (no factory registered)
            _logger.LogWarning(
                "No IRagService registered in factory. Attempting to resolve from DI. " +
                "Consider using services.AddRagService<TService>() for factory registration.");

            var ragService = _serviceProvider.GetService(typeof(IRagService)) as IRagService;

            if (ragService == null)
            {
                throw new InvalidOperationException(
                    "No IRagService registered. Please register using services.AddRagService<TService>() " +
                    "or services.AddSingleton<IRagService, TImplementation>().");
            }

            return ragService;
        }
    }

    private static string FormatResultsForLlm(RagResult result)
    {
        if (result.Documents.Count == 0)
        {
            return "No relevant documents found in the knowledge base.";
        }

        var formatted = $"Found {result.Documents.Count} relevant documents:\n\n";

        for (int i = 0; i < result.Documents.Count; i++)
        {
            var doc = result.Documents[i];
            formatted += $"[Document {i + 1}] (Relevance: {doc.Score:F2})\n";

            if (!string.IsNullOrEmpty(doc.Title))
                formatted += $"Title: {doc.Title}\n";

            if (!string.IsNullOrEmpty(doc.Source))
                formatted += $"Source: {doc.Source}\n";

            formatted += $"Content: {doc.Content}\n\n";
        }

        return formatted;
    }

    private decimal CalculateCost(RagTokenUsage? tokenUsage)
    {
        // Try to get cost settings for this factory key
        if (_settings.RagCostSettings.TryGetValue(_factoryKey, out var costSettings))
        {
            return costSettings.CalculateCost(tokenUsage);
        }

        // Fallback to default cost settings (empty key)
        if (_settings.RagCostSettings.TryGetValue(string.Empty, out var defaultCostSettings))
        {
            return defaultCostSettings.CalculateCost(tokenUsage);
        }

        // No cost settings configured - return 0
        return 0m;
    }
}
