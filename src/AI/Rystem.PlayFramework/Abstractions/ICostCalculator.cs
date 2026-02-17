namespace Rystem.PlayFramework;

/// <summary>
/// Service for calculating costs based on token usage.
/// </summary>
public interface ICostCalculator
{
    /// <summary>
    /// Calculate cost for a given token usage.
    /// </summary>
    /// <param name="usage">Token usage information</param>
    /// <returns>Cost calculation result</returns>
    CostCalculation Calculate(TokenUsage usage);

    /// <summary>
    /// Calculate cost with explicit model override.
    /// </summary>
    /// <param name="usage">Token usage information</param>
    /// <param name="modelId">Model identifier to use for cost calculation</param>
    /// <returns>Cost calculation result</returns>
    CostCalculation Calculate(TokenUsage usage, string modelId);

    /// <summary>
    /// Calculate cost with explicit model and client name override.
    /// Client costs take priority over model costs.
    /// </summary>
    /// <param name="usage">Token usage information</param>
    /// <param name="modelId">Model identifier (e.g., "gpt-4o")</param>
    /// <param name="clientName">Client name as registered in DI (e.g., "gpt4o-east")</param>
    /// <returns>Cost calculation result</returns>
    CostCalculation Calculate(TokenUsage usage, string? modelId, string? clientName);

    /// <summary>
    /// Whether cost tracking is enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Currency code being used.
    /// </summary>
    string Currency { get; }
}
