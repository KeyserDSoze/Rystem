using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace Rystem.PlayFramework.Services.ExecutionModes;

/// <summary>
/// Generates final response after plan execution or dynamic chaining.
/// </summary>
internal sealed class FinalResponseGenerator : IFactoryName
{
    private readonly IServiceProvider _serviceProvider;
    private ExecutionModeHandlerDependencies _dependencies = null!;

    public FinalResponseGenerator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SetFactoryName(AnyOf<string?, Enum>? name)
    {
        var dependenciesFactory = _serviceProvider.GetRequiredService<IFactory<ExecutionModeHandlerDependencies>>();
        _dependencies = dependenciesFactory.Create(name)
            ?? throw new InvalidOperationException($"ExecutionModeHandlerDependencies not found for factory: {name}");
    }

    public async IAsyncEnumerable<AiSceneResponse> GenerateAsync(
        SceneContext context,
        SceneRequestSettings settings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return YieldStatus(AiResponseStatus.GeneratingFinalResponse, "Generating final response");

        // Check if any scene already provided a SPECIFIC_COMMAND
        var directAnswer = context.Responses
            .Where(r => r.Status == AiResponseStatus.Running &&
                       r.Message?.Contains("SPECIFIC_COMMAND:") == true)
            .LastOrDefault();

        if (directAnswer != null)
        {
            yield return YieldAndTrack(context, new AiSceneResponse
            {
                Status = AiResponseStatus.Running,
                Message = directAnswer.Message
            });
            context.ExecutionPhase = ExecutionPhase.Completed;
            yield break;
        }

        // Generate final response based on gathered data
        var finalPrompt = new ChatMessage(ChatRole.User, "Based on all the information gathered, provide the final answer to the user's request.");

        if (settings.EnableStreaming)
        {
            // Streaming mode - use StreamingHelper
            await foreach (var streamUpdateWithCost in context.ChatClientManager.GetStreamingResponseAsync(
                new[] { finalPrompt },
                cancellationToken: cancellationToken))
            {
                await foreach (var streamResponse in _dependencies.StreamingHelper.ProcessChunkAsync(
                    streamUpdateWithCost.Update,
                    null, // No scene name for final response
                    context))
                {
                    yield return streamResponse;
                }
            }
        }
        else
        {
            // Non-streaming mode
            var responseWithCost = await context.ChatClientManager.GetResponseAsync(
                new[] { finalPrompt },
                cancellationToken: cancellationToken);

            // Extract multi-modal contents from LLM response and save to conversation history
            var finalResponseMessage = responseWithCost.Response.Messages?.FirstOrDefault();
            if (finalResponseMessage != null)
            {
                context.AddAssistantMessage(finalResponseMessage);
            }

            var finalContents = finalResponseMessage?.Contents?
                .Where(c => c is DataContent or UriContent)
                .ToList();

            yield return _dependencies.ResponseHelper.CreateFinalResponse(
                sceneName: null,
                message: finalResponseMessage?.Text,
                context: context,
                inputTokens: responseWithCost.InputTokens,
                outputTokens: responseWithCost.OutputTokens,
                cachedInputTokens: responseWithCost.CachedInputTokens,
                cost: responseWithCost.CalculatedCost,
                contents: finalContents);

            if (settings.MaxBudget.HasValue && context.TotalCost > settings.MaxBudget.Value)
            {
                yield return _dependencies.ResponseHelper.CreateBudgetExceededResponse(
                    sceneName: null,
                    maxBudget: settings.MaxBudget.Value,
                    totalCost: context.TotalCost,
                    currency: context.ChatClientManager.Currency);
            }
        }
    }

    private static AiSceneResponse YieldStatus(AiResponseStatus status, string? message = null)
    {
        return new AiSceneResponse
        {
            Status = status,
            Message = message
        };
    }

    private static AiSceneResponse YieldAndTrack(SceneContext context, AiSceneResponse response)
    {
        response.TotalCost = context.TotalCost;
        context.Responses.Add(response);
        return response;
    }
}
