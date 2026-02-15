namespace Rystem.PlayFramework.Api.Models;

/// <summary>
/// Response model for PlayFramework HTTP API (non-streaming).
/// </summary>
public sealed class PlayFrameworkResponse
{
    /// <summary>
    /// All responses from the execution.
    /// </summary>
    public List<AiSceneResponse> Responses { get; set; } = [];

    /// <summary>
    /// Final message (last Running status).
    /// </summary>
    public string? FinalMessage { get; set; }

    /// <summary>
    /// Execution status.
    /// </summary>
    public string Status { get; set; } = "Success";

    /// <summary>
    /// Error message (if failed).
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Total cost of execution.
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Total tokens used.
    /// </summary>
    public int? TotalTokens { get; set; }

    /// <summary>
    /// Execution time in milliseconds.
    /// </summary>
    public long ElapsedMilliseconds { get; set; }

    /// <summary>
    /// Request metadata (echoed back).
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
