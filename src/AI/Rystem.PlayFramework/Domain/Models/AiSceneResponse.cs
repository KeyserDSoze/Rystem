namespace Rystem.PlayFramework;

/// <summary>
/// Represents a response from a scene execution.
/// </summary>
public sealed class AiSceneResponse
{
    /// <summary>
    /// Current status of the execution.
    /// </summary>
    public AiResponseStatus Status { get; set; }

    /// <summary>
    /// Scene name (if applicable).
    /// </summary>
    public string? SceneName { get; set; }

    /// <summary>
    /// Function/tool name that was called (if applicable).
    /// </summary>
    public string? FunctionName { get; set; }

    /// <summary>
    /// Function/tool arguments (if applicable).
    /// </summary>
    public string? FunctionArguments { get; set; }

    /// <summary>
    /// Response message from AI or tool execution.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Error message (if Status is Error).
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Input tokens used in this operation.
    /// </summary>
    public int? InputTokens { get; set; }

    /// <summary>
    /// Cached input tokens (10% cost).
    /// </summary>
    public int? CachedInputTokens { get; set; }

    /// <summary>
    /// Output tokens generated in this operation.
    /// </summary>
    public int? OutputTokens { get; set; }

    /// <summary>
    /// Total tokens (input + cached + output).
    /// </summary>
    public int? TotalTokens { get; set; }

    /// <summary>
    /// Cost of this specific operation (null if no LLM call).
    /// </summary>
    public decimal? Cost { get; set; }

    /// <summary>
    /// Cumulative cost so far in the execution.
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Timestamp of this response.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = [];
}
