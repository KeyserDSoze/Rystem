namespace Rystem.PlayFramework;

/// <summary>
/// Settings for a specific scene request.
/// </summary>
public sealed class SceneRequestSettings
{
    /// <summary>
    /// Maximum recursion depth for planning (default: 5).
    /// </summary>
    public int MaxRecursionDepth { get; set; } = 5;

    /// <summary>
    /// Execution mode for this request.
    /// - Direct: Single scene, fast execution (default)
    /// - Planning: Multi-scene with upfront plan (requires IPlanner)
    /// - DynamicChaining: Multi-scene with live decisions
    /// If not set, uses the default configured in PlayFrameworkSettings.
    /// </summary>
    public SceneExecutionMode? ExecutionMode { get; set; }

    /// <summary>
    /// Whether to enable summarization for this request.
    /// </summary>
    public bool EnableSummarization { get; set; } = true;

    /// <summary>
    /// Whether to enable director for multi-scene orchestration.
    /// </summary>
    public bool EnableDirector { get; set; }

    /// <summary>
    /// Model to use for this request (overrides default).
    /// </summary>
    public string? ModelId { get; set; }

    /// <summary>
    /// Temperature for LLM calls (0.0 - 2.0).
    /// </summary>
    public float? Temperature { get; set; }

    /// <summary>
    /// Maximum tokens to generate.
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Cache behavior for this request.
    /// </summary>
    public CacheBehavior CacheBehavior { get; set; } = CacheBehavior.Default;

    /// <summary>
    /// Maximum budget for this request (in the configured currency).
    /// If set, execution will stop when total cost exceeds this value.
    /// Set to null for unlimited budget (default).
    /// </summary>
    public decimal? MaxBudget { get; set; }

    /// <summary>
    /// Enable streaming for text responses.
    /// When enabled, text responses are streamed token-by-token for better UX.
    /// Note: Tool/function calls are never streamed (require complete JSON).
    /// </summary>
    public bool EnableStreaming { get; set; }

    /// <summary>
    /// Maximum number of scenes that can be executed in dynamic chaining mode.
    /// Prevents infinite loops. Default: 5.
    /// </summary>
    public int MaxDynamicScenes { get; set; } = 5;

    /// <summary>
    /// Cache key (auto-generated if null).
    /// </summary>
    public string? CacheKey { get; set; }

    /// <summary>
    /// Additional metadata for this request.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = [];
}
