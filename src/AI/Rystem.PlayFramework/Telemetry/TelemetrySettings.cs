namespace Rystem.PlayFramework.Telemetry;

/// <summary>
/// Configuration settings for PlayFramework observability features.
/// Controls what telemetry data is collected and how it is sampled.
/// </summary>
public sealed class TelemetrySettings
{
    /// <summary>
    /// Enable distributed tracing with OpenTelemetry.
    /// </summary>
    public bool EnableTracing { get; set; } = true;
    
    /// <summary>
    /// Enable metrics collection with OpenTelemetry.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;
    
    /// <summary>
    /// Trace scene execution activities.
    /// </summary>
    public bool TraceScenes { get; set; } = true;
    
    /// <summary>
    /// Trace tool call activities.
    /// </summary>
    public bool TraceTools { get; set; } = true;
    
    /// <summary>
    /// Trace LLM API calls (includes token counts and timing).
    /// </summary>
    public bool TraceLlmCalls { get; set; } = true;
    
    /// <summary>
    /// Include full LLM prompts in trace attributes.
    /// Warning: This may result in large trace sizes and potential PII exposure.
    /// </summary>
    public bool IncludeLlmPrompts { get; set; } = false;
    
    /// <summary>
    /// Include full LLM responses in trace attributes.
    /// Warning: This may result in large trace sizes and potential PII exposure.
    /// </summary>
    public bool IncludeLlmResponses { get; set; } = false;
    
    /// <summary>
    /// Trace cache operations (get/set).
    /// </summary>
    public bool TraceCacheOperations { get; set; } = true;
    
    /// <summary>
    /// Trace planning operations (when planning is enabled).
    /// </summary>
    public bool TracePlanning { get; set; } = true;
    
    /// <summary>
    /// Trace summarization operations (when summarization is enabled).
    /// </summary>
    public bool TraceSummarization { get; set; } = true;
    
    /// <summary>
    /// Trace director operations (when director is enabled).
    /// </summary>
    public bool TraceDirector { get; set; } = true;
    
    /// <summary>
    /// Trace MCP server operations (tool loading, execution).
    /// </summary>
    public bool TraceMcpOperations { get; set; } = true;
    
    /// <summary>
    /// Sampling rate for traces (0.0 to 1.0).
    /// 1.0 = trace every request, 0.1 = trace 10% of requests, 0.01 = trace 1%.
    /// </summary>
    /// <remarks>
    /// Use lower sampling rates in high-traffic production environments to reduce overhead.
    /// Recommended: 1.0 for dev/staging, 0.1 for production.
    /// </remarks>
    public double SamplingRate { get; set; } = 1.0;
    
    /// <summary>
    /// Custom attributes to add to all activities and metrics.
    /// Useful for adding deployment environment, region, tenant information, etc.
    /// </summary>
    /// <example>
    /// new Dictionary&lt;string, object&gt;
    /// {
    ///     ["deployment.environment"] = "production",
    ///     ["deployment.region"] = "us-east-1",
    ///     ["tenant.id"] = "customer-123"
    /// }
    /// </example>
    public Dictionary<string, object> CustomAttributes { get; set; } = new();
    
    /// <summary>
    /// Maximum length for string attributes (to prevent excessively large traces).
    /// Strings longer than this will be truncated.
    /// </summary>
    public int MaxAttributeLength { get; set; } = 1000;
}
