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
    /// Whether to enable planning for this request.
    /// </summary>
    public bool EnablePlanning { get; set; } = true;

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
    /// Cache key (auto-generated if null).
    /// </summary>
    public string? CacheKey { get; set; }

    /// <summary>
    /// Additional metadata for this request.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = [];
}
