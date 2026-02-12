using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Rystem.PlayFramework.Telemetry;

/// <summary>
/// Metrics collection for PlayFramework observability.
/// Provides counters, histograms, and gauges for monitoring framework performance.
/// </summary>
public static class PlayFrameworkMetrics
{
    /// <summary>
    /// Meter name for PlayFramework metrics.
    /// </summary>
    public const string MeterName = "Rystem.PlayFramework";
    
    private static readonly Meter _meter = new(MeterName, PlayFrameworkActivitySource.Version);
    
    // ========== COUNTERS ==========
    
    /// <summary>
    /// Total number of scene executions.
    /// </summary>
    private static readonly Counter<long> _sceneExecutionCounter = _meter.CreateCounter<long>(
        "playframework.scene.executions",
        description: "Total number of scene executions");
    
    /// <summary>
    /// Total number of tool calls.
    /// </summary>
    private static readonly Counter<long> _toolCallCounter = _meter.CreateCounter<long>(
        "playframework.tool.calls",
        description: "Total number of tool calls");
    
    /// <summary>
    /// Total number of cache hits.
    /// </summary>
    private static readonly Counter<long> _cacheHitCounter = _meter.CreateCounter<long>(
        "playframework.cache.hits",
        description: "Number of cache hits");
    
    /// <summary>
    /// Total number of cache misses.
    /// </summary>
    private static readonly Counter<long> _cacheMissCounter = _meter.CreateCounter<long>(
        "playframework.cache.misses",
        description: "Number of cache misses");
    
    /// <summary>
    /// Total number of LLM calls.
    /// </summary>
    private static readonly Counter<long> _llmCallCounter = _meter.CreateCounter<long>(
        "playframework.llm.calls",
        description: "Total number of LLM API calls");
    
    /// <summary>
    /// Total number of tokens consumed.
    /// </summary>
    private static readonly Counter<long> _tokenCounter = _meter.CreateCounter<long>(
        "playframework.llm.tokens.total",
        description: "Total tokens consumed (prompt + completion)");
    
    /// <summary>
    /// Total number of MCP tool executions.
    /// </summary>
    private static readonly Counter<long> _mcpToolExecutionCounter = _meter.CreateCounter<long>(
        "playframework.mcp.tool_executions",
        description: "Total number of MCP tool executions");

    /// <summary>
    /// Total number of RAG searches performed.
    /// </summary>
    private static readonly Counter<long> _ragSearchCounter = _meter.CreateCounter<long>(
        "playframework.rag.searches",
        description: "Total number of RAG searches performed");

    /// <summary>
    /// Total number of embedding tokens consumed by RAG operations.
    /// </summary>
    private static readonly Counter<long> _ragTokenCounter = _meter.CreateCounter<long>(
        "playframework.rag.tokens",
        description: "Total embedding tokens consumed by RAG");

    /// <summary>
    /// Total number of web searches performed.
    /// </summary>
    private static readonly Counter<long> _webSearchCounter = _meter.CreateCounter<long>(
        "playframework.web_search.searches",
        description: "Total number of web searches performed");

    // ========== HISTOGRAMS ==========
    
    /// <summary>
    /// Scene execution duration distribution.
    /// </summary>
    private static readonly Histogram<double> _sceneExecutionDuration = _meter.CreateHistogram<double>(
        "playframework.scene.duration",
        unit: "ms",
        description: "Scene execution duration in milliseconds");
    
    /// <summary>
    /// Tool execution duration distribution.
    /// </summary>
    private static readonly Histogram<double> _toolExecutionDuration = _meter.CreateHistogram<double>(
        "playframework.tool.duration",
        unit: "ms",
        description: "Tool execution duration in milliseconds");
    
    /// <summary>
    /// LLM call duration distribution.
    /// </summary>
    private static readonly Histogram<double> _llmCallDuration = _meter.CreateHistogram<double>(
        "playframework.llm.duration",
        unit: "ms",
        description: "LLM API call duration in milliseconds");
    
    /// <summary>
    /// Token usage per request distribution.
    /// </summary>
    private static readonly Histogram<long> _tokenUsage = _meter.CreateHistogram<long>(
        "playframework.llm.tokens.per_request",
        description: "LLM token usage per request");
    
    /// <summary>
    /// Cost per scene execution distribution.
    /// </summary>
    private static readonly Histogram<double> _costPerExecution = _meter.CreateHistogram<double>(
        "playframework.cost.per_execution",
        unit: "USD",
        description: "Cost per scene execution in USD");
    
    /// <summary>
    /// Cache access duration distribution.
    /// </summary>
    private static readonly Histogram<double> _cacheAccessDuration = _meter.CreateHistogram<double>(
        "playframework.cache.duration",
        unit: "ms",
        description: "Cache access duration in milliseconds");

    /// <summary>
    /// RAG search duration distribution.
    /// </summary>
    private static readonly Histogram<double> _ragDurationHistogram = _meter.CreateHistogram<double>(
        "playframework.rag.duration",
        unit: "ms",
        description: "RAG search duration in milliseconds");

    /// <summary>
    /// RAG search cost distribution.
    /// </summary>
    private static readonly Histogram<double> _ragCostHistogram = _meter.CreateHistogram<double>(
        "playframework.rag.cost",
        unit: "USD",
        description: "Cost per RAG search in USD");

    /// <summary>
    /// Web search duration distribution.
    /// </summary>
    private static readonly Histogram<double> _webSearchDurationHistogram = _meter.CreateHistogram<double>(
        "playframework.web_search.duration",
        unit: "ms",
        description: "Web search duration in milliseconds");

    /// <summary>
    /// Web search cost distribution.
    /// </summary>
    private static readonly Histogram<double> _webSearchCostHistogram = _meter.CreateHistogram<double>(
        "playframework.web_search.cost",
        unit: "USD",
        description: "Cost per web search in USD");

    /// <summary>
    /// Number of web search results returned per search.
    /// </summary>
    private static readonly Histogram<int> _webSearchResultsHistogram = _meter.CreateHistogram<int>(
        "playframework.web_search.results",
        description: "Number of results returned per web search");

    // ========== GAUGES (Observable) ==========

    private static long _activeScenes = 0;
    private static long _activeLlmCalls = 0;
    private static long _activeToolCalls = 0;

    /// <summary>
    /// Current number of active scene executions.
    /// </summary>
    private static readonly ObservableGauge<long> _activeSceneGauge = _meter.CreateObservableGauge(
        "playframework.scene.active",
        () => Interlocked.Read(ref _activeScenes),
        description: "Number of currently active scene executions");

    /// <summary>
    /// Current number of active LLM calls.
    /// </summary>
    private static readonly ObservableGauge<long> _activeLlmCallGauge = _meter.CreateObservableGauge(
        "playframework.llm.active",
        () => Interlocked.Read(ref _activeLlmCalls),
        description: "Number of currently active LLM calls");

    /// <summary>
    /// Current number of active tool calls.
    /// </summary>
    private static readonly ObservableGauge<long> _activeToolCallGauge = _meter.CreateObservableGauge(
        "playframework.tool.active",
        () => Interlocked.Read(ref _activeToolCalls),
        description: "Number of currently active tool calls");

    // ========== PUBLIC API ==========
    
    /// <summary>
    /// Records a scene execution with all relevant metrics.
    /// </summary>
    public static void RecordSceneExecution(
        string sceneName,
        string executionMode,
        bool success,
        double durationMs,
        int tokenCount = 0,
        double cost = 0)
    {
        var tags = new TagList
        {
            { "scene.name", sceneName },
            { "scene.mode", executionMode },
            { "success", success }
        };
        
        _sceneExecutionCounter.Add(1, tags);
        _sceneExecutionDuration.Record(durationMs, tags);
        
        if (tokenCount > 0)
        {
            _tokenCounter.Add(tokenCount, tags);
            _tokenUsage.Record(tokenCount, tags);
        }
        
        if (cost > 0)
        {
            _costPerExecution.Record(cost, tags);
        }
    }
    
    /// <summary>
    /// Records a tool execution.
    /// </summary>
    public static void RecordToolCall(
        string toolName,
        string toolType,
        bool success,
        double durationMs)
    {
        var tags = new TagList
        {
            { "tool.name", toolName },
            { "tool.type", toolType },
            { "success", success }
        };
        
        _toolCallCounter.Add(1, tags);
        _toolExecutionDuration.Record(durationMs, tags);
    }
    
    /// <summary>
    /// Records a cache access (hit or miss).
    /// </summary>
    public static void RecordCacheAccess(
        bool hit,
        string cacheKey,
        double durationMs)
    {
        var tags = new TagList { { "cache.key", cacheKey } };
        
        if (hit)
            _cacheHitCounter.Add(1, tags);
        else
            _cacheMissCounter.Add(1, tags);
        
        _cacheAccessDuration.Record(durationMs, tags);
    }
    
    /// <summary>
    /// Records an LLM API call.
    /// </summary>
    public static void RecordLlmCall(
        string provider,
        string model,
        bool success,
        double durationMs,
        int promptTokens = 0,
        int completionTokens = 0)
    {
        var tags = new TagList
        {
            { "llm.provider", provider },
            { "llm.model", model },
            { "success", success }
        };
        
        _llmCallCounter.Add(1, tags);
        _llmCallDuration.Record(durationMs, tags);
        
        var totalTokens = promptTokens + completionTokens;
        if (totalTokens > 0)
        {
            _tokenCounter.Add(totalTokens, tags);
            _tokenUsage.Record(totalTokens, tags);
        }
    }
    
    /// <summary>
    /// Records an MCP tool execution.
    /// </summary>
    public static void RecordMcpToolExecution(
        string mcpServerName,
        string toolName,
        bool success,
        double durationMs)
    {
        var tags = new TagList
        {
            { "mcp.server", mcpServerName },
            { "mcp.tool", toolName },
            { "success", success }
        };

        _mcpToolExecutionCounter.Add(1, tags);
        _toolExecutionDuration.Record(durationMs, tags);
    }

    /// <summary>
    /// Records a RAG search operation with token usage and cost tracking.
    /// </summary>
    /// <param name="provider">RAG provider name (e.g., "azure", "pinecone", or factory key).</param>
    /// <param name="documentsFound">Number of documents returned.</param>
    /// <param name="tokens">Total tokens consumed (embedding generation).</param>
    /// <param name="cost">Total cost in USD.</param>
    /// <param name="durationMs">Search duration in milliseconds.</param>
    public static void RecordRagSearch(
        string provider,
        int documentsFound,
        int tokens,
        double cost,
        double durationMs)
    {
        var tags = new TagList
        {
            { "rag.provider", provider },
            { "rag.documents_found", documentsFound }
        };

        _ragSearchCounter.Add(1, tags);

        if (tokens > 0)
        {
            _ragTokenCounter.Add(tokens, tags);
        }

        if (cost > 0)
        {
            _ragCostHistogram.Record(cost, tags);
        }

        _ragDurationHistogram.Record(durationMs, tags);
    }

    /// <summary>
    /// Records a web search operation with cost tracking.
    /// </summary>
    /// <param name="provider">Web search provider name (e.g., "bing", "google", or factory key).</param>
    /// <param name="resultsFound">Number of results returned.</param>
    /// <param name="cost">Total cost in USD.</param>
    /// <param name="durationMs">Search duration in milliseconds.</param>
    public static void RecordWebSearch(
        string provider,
        int resultsFound,
        double cost,
        double durationMs)
    {
        var tags = new TagList
        {
            { "web_search.provider", provider },
            { "web_search.results_found", resultsFound }
        };

        _webSearchCounter.Add(1, tags);

        if (resultsFound > 0)
        {
            _webSearchResultsHistogram.Record(resultsFound, tags);
        }

        if (cost > 0)
        {
            _webSearchCostHistogram.Record(cost, tags);
        }

        _webSearchDurationHistogram.Record(durationMs, tags);
    }

    // ========== GAUGE MANAGEMENT ==========

    /// <summary>
    /// Increments the active scenes counter.
    /// </summary>
    public static void IncrementActiveScenes() => Interlocked.Increment(ref _activeScenes);

    /// <summary>
    /// Decrements the active scenes counter.
    /// </summary>
    public static void DecrementActiveScenes() => Interlocked.Decrement(ref _activeScenes);

    /// <summary>
    /// Increments the active LLM calls counter.
    /// </summary>
    public static void IncrementActiveLlmCalls() => Interlocked.Increment(ref _activeLlmCalls);

    /// <summary>
    /// Decrements the active LLM calls counter.
    /// </summary>
    public static void DecrementActiveLlmCalls() => Interlocked.Decrement(ref _activeLlmCalls);

    /// <summary>
    /// Increments the active tool calls counter.
    /// </summary>
    public static void IncrementActiveToolCalls() => Interlocked.Increment(ref _activeToolCalls);

    /// <summary>
    /// Decrements the active tool calls counter.
    /// </summary>
    public static void DecrementActiveToolCalls() => Interlocked.Decrement(ref _activeToolCalls);
}
