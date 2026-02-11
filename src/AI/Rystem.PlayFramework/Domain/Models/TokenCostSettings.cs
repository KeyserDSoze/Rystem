namespace Rystem.PlayFramework;

/// <summary>
/// Configuration for token cost tracking.
/// </summary>
public sealed class TokenCostSettings
{
    /// <summary>
    /// Currency code (e.g., "USD", "EUR", "GBP").
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Cost per 1,000 input tokens.
    /// </summary>
    public decimal InputTokenCostPer1K { get; set; }

    /// <summary>
    /// Cost per 1,000 output tokens.
    /// </summary>
    public decimal OutputTokenCostPer1K { get; set; }

    /// <summary>
    /// Cost per 1,000 cached input tokens (usually 10% of regular input cost).
    /// </summary>
    public decimal CachedInputTokenCostPer1K { get; set; }

    /// <summary>
    /// Whether cost tracking is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Model-specific cost overrides.
    /// Key: model ID (e.g., "gpt-4", "gpt-3.5-turbo")
    /// Value: TokenCostSettings for that model
    /// </summary>
    public Dictionary<string, ModelCostSettings> ModelCosts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Cost settings for a specific model.
/// </summary>
public sealed class ModelCostSettings
{
    /// <summary>
    /// Model identifier (e.g., "gpt-4", "gpt-3.5-turbo").
    /// </summary>
    public required string ModelId { get; init; }

    /// <summary>
    /// Cost per 1,000 input tokens.
    /// </summary>
    public decimal InputTokenCostPer1K { get; set; }

    /// <summary>
    /// Cost per 1,000 output tokens.
    /// </summary>
    public decimal OutputTokenCostPer1K { get; set; }

    /// <summary>
    /// Cost per 1,000 cached input tokens.
    /// </summary>
    public decimal CachedInputTokenCostPer1K { get; set; }
}

/// <summary>
/// Token usage information from a chat completion.
/// </summary>
public sealed class TokenUsage
{
    /// <summary>
    /// Number of input tokens used.
    /// </summary>
    public int InputTokens { get; set; }

    /// <summary>
    /// Number of output tokens generated.
    /// </summary>
    public int OutputTokens { get; set; }

    /// <summary>
    /// Number of cached input tokens (from prompt caching).
    /// </summary>
    public int CachedInputTokens { get; set; }

    /// <summary>
    /// Total tokens (input + output).
    /// </summary>
    public int TotalTokens => InputTokens + OutputTokens + CachedInputTokens;

    /// <summary>
    /// Model used for this request.
    /// </summary>
    public string? ModelId { get; set; }
}

/// <summary>
/// Cost calculation result.
/// </summary>
public sealed class CostCalculation
{
    /// <summary>
    /// Cost for input tokens.
    /// </summary>
    public decimal InputCost { get; set; }

    /// <summary>
    /// Cost for output tokens.
    /// </summary>
    public decimal OutputCost { get; set; }

    /// <summary>
    /// Cost for cached input tokens.
    /// </summary>
    public decimal CachedInputCost { get; set; }

    /// <summary>
    /// Total cost for this operation.
    /// </summary>
    public decimal TotalCost => InputCost + OutputCost + CachedInputCost;

    /// <summary>
    /// Currency code.
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Model used for calculation.
    /// </summary>
    public string? ModelId { get; set; }

    /// <summary>
    /// Token usage that generated this cost.
    /// </summary>
    public required TokenUsage Usage { get; init; }
}
