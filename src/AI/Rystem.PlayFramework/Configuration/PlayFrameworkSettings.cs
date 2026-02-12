namespace Rystem.PlayFramework;

/// <summary>
/// Global settings for PlayFramework.
/// </summary>
public sealed class PlayFrameworkSettings
{
    /// <summary>
    /// Default execution mode for all requests.
    /// Can be overridden per-request via SceneRequestSettings.ExecutionMode.
    /// Default: Direct (single scene, fast execution).
    /// </summary>
    public SceneExecutionMode DefaultExecutionMode { get; set; } = SceneExecutionMode.Direct;

    /// <summary>
    /// Planning configuration.
    /// </summary>
    public PlanningSettings Planning { get; set; } = new();

    /// <summary>
    /// Summarization configuration.
    /// </summary>
    public SummarizationSettings Summarization { get; set; } = new();

    /// <summary>
    /// Director configuration.
    /// </summary>
    public DirectorSettings Director { get; set; } = new();

    /// <summary>
    /// Cache configuration.
    /// </summary>
    public CacheSettings Cache { get; set; } = new();

    /// <summary>
    /// Cost tracking configuration.
    /// </summary>
    public TokenCostSettings CostTracking { get; set; } = new();

    /// <summary>
    /// Default model to use for chat completions.
    /// </summary>
    public string? DefaultModelId { get; set; }

    /// <summary>
    /// Default temperature (0.0 - 2.0).
    /// </summary>
    public float? DefaultTemperature { get; set; }

    /// <summary>
    /// Default maximum tokens to generate.
    /// </summary>
    public int? DefaultMaxTokens { get; set; }
}

/// <summary>
/// Settings for planning.
/// </summary>
public sealed class PlanningSettings
{
    /// <summary>
    /// Whether planning is enabled by default.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum recursion depth for planning.
    /// </summary>
    public int MaxRecursionDepth { get; set; } = 5;

    /// <summary>
    /// Model to use for planning (overrides default if set).
    /// </summary>
    public string? ModelId { get; set; }
}

/// <summary>
/// Settings for summarization.
/// </summary>
public sealed class SummarizationSettings
{
    /// <summary>
    /// Whether summarization is enabled by default.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Character threshold for triggering summarization.
    /// </summary>
    public int CharacterThreshold { get; set; } = 15_000;

    /// <summary>
    /// Response count threshold for triggering summarization.
    /// </summary>
    public int ResponseCountThreshold { get; set; } = 20;

    /// <summary>
    /// Model to use for summarization (overrides default if set).
    /// </summary>
    public string? ModelId { get; set; }
}

/// <summary>
/// Settings for director.
/// </summary>
public sealed class DirectorSettings
{
    /// <summary>
    /// Whether director is enabled by default.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Maximum number of scene re-executions.
    /// </summary>
    public int MaxReExecutions { get; set; } = 3;

    /// <summary>
    /// Model to use for director decisions (overrides default if set).
    /// </summary>
    public string? ModelId { get; set; }
}

/// <summary>
/// Settings for caching.
/// </summary>
public sealed class CacheSettings
{
    /// <summary>
    /// Whether caching is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Default cache expiration in seconds (null = no expiration).
    /// </summary>
    public int? DefaultExpirationSeconds { get; set; }

    /// <summary>
    /// Cache key prefix.
    /// </summary>
    public string KeyPrefix { get; set; } = "play_framework:";
}
