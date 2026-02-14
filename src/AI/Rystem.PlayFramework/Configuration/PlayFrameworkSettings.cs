namespace Rystem.PlayFramework;

using Rystem.PlayFramework.Telemetry;

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
    /// Telemetry and observability configuration.
    /// </summary>
    public TelemetrySettings Telemetry { get; set; } = new();

    /// <summary>
    /// Chat client names for PRIMARY load balancing pool.
    /// These clients handle normal traffic with load distribution.
    /// Example: ["gpt-4o-1", "gpt-4o-2", "gpt-4o-3"]
    /// </summary>
    public List<string> ChatClientNames { get; set; } = [];

    /// <summary>
    /// Load balancing mode for primary client pool.
    /// - None: Use only first client (no load balancing)
    /// - Sequential: Distribute sequentially (client1 → client2 → client3 → client1...)
    /// - RoundRobin: Balanced rotation
    /// - Random: Random selection
    /// Default: None
    /// </summary>
    public LoadBalancingMode LoadBalancingMode { get; set; } = LoadBalancingMode.None;

    /// <summary>
    /// Chat client names for FALLBACK chain.
    /// These clients are used ONLY if all primary clients fail.
    /// Example: ["claude-sonnet", "llama-3.1-local"]
    /// </summary>
    public List<string> FallbackChatClientNames { get; set; } = [];

    /// <summary>
    /// Fallback mode for fallback chain (Sequential/RoundRobin/Random).
    /// Default: Sequential.
    /// </summary>
    public FallbackMode FallbackMode { get; set; } = FallbackMode.Sequential;

    /// <summary>
    /// Maximum retry attempts per client on transient errors.
    /// Default: 3.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay for exponential backoff (in seconds).
    /// Retry delays: 1s, 2s, 4s with default base of 1.
    /// Default: 1.0.
    /// </summary>
    public double RetryBaseDelaySeconds { get; set; } = 1.0;

    /// <summary>
    /// Rate limiting configuration.
    /// Controls request rate to prevent overloading LLM providers.
    /// Disabled by default. Enable via .WithRateLimit() builder.
    /// </summary>
    public RateLimitSettings? RateLimiting { get; set; }

    /// <summary>
    /// Memory configuration.
    /// Enables conversation memory that persists important information across requests.
    /// Disabled by default. Enable via .WithMemory() builder.
    /// </summary>
    public MemorySettings? Memory { get; set; }

    /// <summary>
    /// Global RAG configurations (key = factory key or empty for default).
    /// </summary>
    public Dictionary<string, RagSettings> GlobalRagSettings { get; set; } = new();

    /// <summary>
    /// RAG cost settings per factory key (key = factory key or empty for default).
    /// Used to calculate costs when IRagService returns TokenUsage but not Cost.
    /// </summary>
    public Dictionary<string, RagCostSettings> RagCostSettings { get; set; } = new();

    /// <summary>
    /// Global Web Search configurations (key = factory key or empty for default).
    /// </summary>
    public Dictionary<string, WebSearchSettings> GlobalWebSearchSettings { get; set; } = new();

    /// <summary>
    /// Web Search cost settings per factory key (key = factory key or empty for default).
    /// Used to calculate costs when IWebSearchService returns Cost=null.
    /// </summary>
    public Dictionary<string, WebSearchCostSettings> WebSearchCostSettings { get; set; } = new();

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
