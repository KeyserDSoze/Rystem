using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Rystem.PlayFramework;

/// <summary>
/// A <see cref="DelegatingChatClient"/> that calculates LLM token costs and embeds the result
/// into <see cref="ChatResponse.AdditionalProperties"/> / <see cref="ChatResponseUpdate.AdditionalProperties"/>
/// under the key <see cref="PlayFrameworkCostConstants.CostCalculationKey"/>.
/// <para>
/// Register this wrapper from the adapter package by passing <see cref="TokenCostSettings"/> via
/// <c>AdapterSettings.CostTracking</c>. PlayFramework's <see cref="ChatClientManager"/> reads the
/// pre-calculated <see cref="CostCalculation"/> from the response, so it never needs to own cost logic.
/// </para>
/// </summary>
public sealed class CostTrackingChatClient : DelegatingChatClient
{
    private readonly ICostCalculator _calculator;

    public CostTrackingChatClient(IChatClient innerClient, TokenCostSettings settings)
        : base(innerClient)
    {
        _calculator = new CostCalculator(settings);
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await base.GetResponseAsync(messages, options, cancellationToken);

        var usage = new TokenUsage
        {
            InputTokens = (int)(response.Usage?.InputTokenCount ?? 0),
            OutputTokens = (int)(response.Usage?.OutputTokenCount ?? 0),
            CachedInputTokens = (int)(response.Usage?.CachedInputTokenCount ?? 0),
            ModelId = response.ModelId
        };

        var costCalc = _calculator.Calculate(usage);
        response.AdditionalProperties ??= [];
        response.AdditionalProperties[PlayFrameworkCostConstants.CostCalculationKey] = costCalc;

        return response;
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? modelId = null;

        await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            if (update.ModelId is not null)
                modelId = update.ModelId;

            // UsageContent arrives (usually on the last chunk) with token counts
            var usageContent = update.Contents?.OfType<UsageContent>().FirstOrDefault();
            if (usageContent != null)
            {
                var usage = new TokenUsage
                {
                    InputTokens = (int)(usageContent.Details.InputTokenCount ?? 0),
                    OutputTokens = (int)(usageContent.Details.OutputTokenCount ?? 0),
                    CachedInputTokens = (int)(usageContent.Details.CachedInputTokenCount ?? 0),
                    ModelId = modelId
                };

                var costCalc = _calculator.Calculate(usage);
                update.AdditionalProperties ??= [];
                update.AdditionalProperties[PlayFrameworkCostConstants.CostCalculationKey] = costCalc;
            }

            yield return update;
        }
    }
}
