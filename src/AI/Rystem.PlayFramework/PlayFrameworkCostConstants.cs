namespace Rystem.PlayFramework;

/// <summary>
/// Well-known constants for cost tracking in PlayFramework.
/// </summary>
public static class PlayFrameworkCostConstants
{
    /// <summary>
    /// Key used in <see cref="Microsoft.Extensions.AI.ChatResponse.AdditionalProperties"/>
    /// and <see cref="Microsoft.Extensions.AI.ChatResponseUpdate.AdditionalProperties"/>
    /// to carry the pre-calculated <see cref="CostCalculation"/> produced by
    /// <see cref="CostTrackingChatClient"/>.
    /// </summary>
    public const string CostCalculationKey = "PlayFramework.CostCalculation";
}
