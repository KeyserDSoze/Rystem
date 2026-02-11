namespace Rystem.PlayFramework;

/// <summary>
/// Default implementation of cost calculator.
/// </summary>
internal sealed class CostCalculator : ICostCalculator
{
    private readonly TokenCostSettings _settings;

    public CostCalculator(TokenCostSettings settings)
    {
        _settings = settings;
    }

    public bool IsEnabled => _settings.Enabled;

    public string Currency => _settings.Currency;

    public CostCalculation Calculate(TokenUsage usage)
    {
        if (!IsEnabled)
        {
            return new CostCalculation
            {
                Usage = usage,
                Currency = Currency,
                ModelId = usage.ModelId
            };
        }

        // Try to find model-specific costs
        ModelCostSettings? modelCosts = null;
        if (!string.IsNullOrEmpty(usage.ModelId) && 
            _settings.ModelCosts.TryGetValue(usage.ModelId, out var costs))
        {
            modelCosts = costs;
        }

        // Calculate costs per token type
        var inputCostPer1K = modelCosts?.InputTokenCostPer1K ?? _settings.InputTokenCostPer1K;
        var outputCostPer1K = modelCosts?.OutputTokenCostPer1K ?? _settings.OutputTokenCostPer1K;
        var cachedCostPer1K = modelCosts?.CachedInputTokenCostPer1K ?? _settings.CachedInputTokenCostPer1K;

        var inputCost = CalculateCost(usage.InputTokens, inputCostPer1K);
        var outputCost = CalculateCost(usage.OutputTokens, outputCostPer1K);
        var cachedCost = CalculateCost(usage.CachedInputTokens, cachedCostPer1K);

        return new CostCalculation
        {
            InputCost = inputCost,
            OutputCost = outputCost,
            CachedInputCost = cachedCost,
            Currency = Currency,
            ModelId = usage.ModelId,
            Usage = usage
        };
    }

    public CostCalculation Calculate(TokenUsage usage, string modelId)
    {
        usage.ModelId = modelId;
        return Calculate(usage);
    }

    private static decimal CalculateCost(int tokens, decimal costPer1K)
    {
        if (tokens == 0 || costPer1K == 0)
            return 0;

        // Cost = (tokens / 1000) * costPer1K
        return Math.Round((tokens / 1000m) * costPer1K, 6);
    }
}
