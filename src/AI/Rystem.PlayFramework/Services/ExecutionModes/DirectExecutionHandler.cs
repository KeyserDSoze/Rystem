using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Rystem.PlayFramework.Services.Helpers;
using System.Runtime.CompilerServices;

namespace Rystem.PlayFramework.Services.ExecutionModes;

/// <summary>
/// Handles Direct execution mode - LLM selects a scene using function calling.
/// </summary>
internal sealed class DirectExecutionHandler : IExecutionModeHandler
{
    private readonly ExecutionModeHandlerDependencies _dependencies;
    private readonly ISceneExecutor _sceneExecutor;

    public DirectExecutionHandler(
        ExecutionModeHandlerDependencies dependencies,
        ISceneExecutor sceneExecutor)
    {
        _dependencies = dependencies;
        _sceneExecutor = sceneExecutor;
    }

    public async IAsyncEnumerable<AiSceneResponse> ExecuteAsync(
        SceneContext context,
        SceneRequestSettings settings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Get all scenes as tools for selection
        var sceneTools = _dependencies.SceneFactory.GetSceneNames()
            .Select(name => _dependencies.SceneFactory.Create(name))
            .Select(scene => SceneSelectionToolFactory.CreateSceneSelectionTool(scene))
            .ToList();

        // Configure chat with scene selection tools
        var chatOptions = new ChatOptions
        {
            Tools = [.. sceneTools.Cast<AITool>()]
        };

        ChatMessage? finalMessage = null;
        var accumulatedText = string.Empty;
        List<FunctionCallContent> accumulatedFunctionCalls = [];
        var streamedToUser = false;
        decimal? totalCost = null;
        int? totalInputTokens = null;
        int? totalOutputTokens = null;
        int? totalCachedInputTokens = null;

        // Use streaming when enabled, fallback to non-streaming otherwise
        if (settings.EnableStreaming)
        {
            // Use StreamingHelper for optimistic streaming
            StreamingResult? lastResult = null;
            await foreach (var result in _dependencies.StreamingHelper.ProcessOptimisticStreamAsync(
                context,
                context.GetMessagesForLLM(),
                chatOptions,
                null, // No scene name for scene selection
                cancellationToken))
            {
                lastResult = result;

                // Stream to user if needed (text response with no function calls detected yet)
                if (result.FinalMessage == null && result.StreamedToUser)
                {
                    yield return _dependencies.ResponseHelper.CreateStreamingResponse(
                        sceneName: null,
                        streamingChunk: result.StreamChunk ?? string.Empty,
                        message: result.AccumulatedText,
                        isStreamingComplete: false,
                        totalCost: context.TotalCost);
                }
            }

            // Extract final state
            if (lastResult != null)
            {
                finalMessage = lastResult.FinalMessage;
                accumulatedText = lastResult.AccumulatedText;
                accumulatedFunctionCalls = lastResult.FunctionCalls;
                streamedToUser = lastResult.StreamedToUser;
                totalCost = lastResult.TotalCost;
                totalInputTokens = lastResult.TotalInputTokens;
                totalOutputTokens = lastResult.TotalOutputTokens;
                totalCachedInputTokens = lastResult.TotalCachedInputTokens;
            }
        }
        else
        {
            // NON-STREAMING MODE - fallback to complete response
            var responseWithCost = await context.ChatClientManager.GetResponseAsync(
                context.GetMessagesForLLM(),
                chatOptions,
                cancellationToken);

            finalMessage = responseWithCost.Response.Messages?.FirstOrDefault();

            if (finalMessage != null)
            {
                accumulatedText = finalMessage.Text ?? string.Empty;
                var functionCalls = finalMessage.Contents?
                    .OfType<FunctionCallContent>()
                    .ToList() ?? [];

                if (functionCalls.Any())
                {
                    accumulatedFunctionCalls = functionCalls;
                }
            }

            totalInputTokens = responseWithCost.InputTokens;
            totalOutputTokens = responseWithCost.OutputTokens;
            totalCachedInputTokens = responseWithCost.CachedInputTokens;
            totalCost = responseWithCost.CalculatedCost;
        }

        // Track costs for scene selection
        if (totalCost.HasValue)
        {
            context.AddCost(totalCost.Value);
        }

        // Check budget limit
        if (settings.MaxBudget.HasValue && context.TotalCost > settings.MaxBudget.Value)
        {
            yield return YieldAndTrack(context, new AiSceneResponse
            {
                Status = AiResponseStatus.BudgetExceeded,
                Message = $"Budget limit of {settings.MaxBudget:F6} {context.ChatClientManager.Currency} exceeded. Total cost: {context.TotalCost:F6}",
                ErrorMessage = "Maximum budget reached"
            });
            context.ExecutionPhase = ExecutionPhase.Completed;
            yield break;
        }

        // Save assistant response to conversation history for tracking
        if (finalMessage != null)
        {
            context.AddAssistantMessage(finalMessage);
        }

        // Extract multi-modal contents (DataContent, UriContent) from LLM response
        var multiModalContents = finalMessage?.Contents?
            .Where(c => c is DataContent or UriContent)
            .ToList();

        // Process function calls (scene selections)
        if (accumulatedFunctionCalls.Count > 0)
        {
            foreach (var functionCall in accumulatedFunctionCalls)
            {
                // Scene selection via function call
                var selectedSceneName = functionCall.Name;

                yield return YieldAndTrack(context, new AiSceneResponse
                {
                    Status = AiResponseStatus.Running,
                    Message = $"Selected scene: {selectedSceneName}",
                    SceneName = selectedSceneName,
                    Contents = multiModalContents
                });

                // Execute the selected scene
                var scene = _dependencies.SceneMatchingHelper.FindSceneByFuzzyMatch(selectedSceneName, _dependencies.SceneFactory);
                if (scene != null)
                {
                    await foreach (var sceneResponse in _sceneExecutor.ExecuteSceneAsync(context, scene, settings, cancellationToken))
                    {
                        yield return sceneResponse;
                    }

                    yield break;
                }
                else
                {
                    yield return YieldAndTrack(context, new AiSceneResponse
                    {
                        Status = AiResponseStatus.Error,
                        Message = $"Scene '{selectedSceneName}' not found"
                    });
                    context.ExecutionPhase = ExecutionPhase.Completed;
                    yield break;
                }
            }
        }

        // Fallback: if no function call, return text response with multi-modal contents
        // If streaming was enabled and already streamed, send final chunk
        if (settings.EnableStreaming && streamedToUser)
        {
            yield return _dependencies.ResponseHelper.CreateFinalResponse(
                sceneName: null,
                message: accumulatedText,
                context: context,
                inputTokens: totalInputTokens,
                outputTokens: totalOutputTokens,
                cachedInputTokens: totalCachedInputTokens,
                cost: totalCost,
                streamingChunk: string.Empty,
                isStreamingComplete: true,
                contents: multiModalContents);
        }
        else
        {
            // Non-streaming or no streaming happened
            var responseText = string.IsNullOrEmpty(accumulatedText) ? "No response from LLM" : accumulatedText;
            yield return YieldAndTrack(context, new AiSceneResponse
            {
                Status = AiResponseStatus.Running,
                Message = responseText,
                Contents = multiModalContents
            });
        }
        context.ExecutionPhase = ExecutionPhase.Completed;
    }

    private AiSceneResponse YieldAndTrack(SceneContext context, AiSceneResponse response)
    {
        response.TotalCost = context.TotalCost;
        context.Responses.Add(response);
        return response;
    }
}
