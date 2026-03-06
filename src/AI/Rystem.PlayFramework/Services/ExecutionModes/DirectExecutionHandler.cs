using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rystem.PlayFramework.Services.Helpers;
using Rystem.PlayFramework.Telemetry;

namespace Rystem.PlayFramework.Services.ExecutionModes;

/// <summary>
/// Handles Direct execution mode - LLM selects a scene using function calling.
/// </summary>
internal sealed class DirectExecutionHandler : IExecutionModeHandler
{
    private readonly IFactory<ExecutionModeHandlerDependencies> _dependenciesFactory;
    private readonly IFactory<ISceneExecutor> _sceneExecutorFactory;
    private readonly ILogger<DirectExecutionHandler> _logger;

    public DirectExecutionHandler(
        IFactory<ExecutionModeHandlerDependencies> dependenciesFactory,
        IFactory<ISceneExecutor> sceneExecutorFactory,
        ILogger<DirectExecutionHandler> logger)
    {
        _dependenciesFactory = dependenciesFactory;
        _sceneExecutorFactory = sceneExecutorFactory;
        _logger = logger;
    }

    public async IAsyncEnumerable<AiSceneResponse> ExecuteAsync(
        AnyOf<string?, Enum>? factoryName,
        SceneContext context,
        SceneRequestSettings settings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Resolve dependencies from factory
        var dependencies = _dependenciesFactory.Create(factoryName)
            ?? throw new InvalidOperationException($"ExecutionModeHandlerDependencies not found for factory: {factoryName}");

        var sceneExecutor = _sceneExecutorFactory.Create(factoryName)
            ?? throw new InvalidOperationException($"SceneExecutor not found for factory: {factoryName}");

        var factoryNameString = factoryName?.ToString() ?? "default";
        _logger.LogDebug("Starting Direct execution mode (Factory: {FactoryName})", factoryNameString);

        // Configure chat with scene selection tools
        var chatOptions = new ChatOptions
        {
            Tools = [.. dependencies.SceneFactory.ScenesAsAiTool]
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
            await foreach (var result in dependencies.StreamingHelper.ProcessOptimisticStreamAsync(
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
                    yield return dependencies.ResponseHelper.CreateStreamingResponse(
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
            PlayFrameworkMetrics.IncrementActiveLlmCalls();
            var llmCallStart = DateTime.UtcNow;
            var responseWithCost = await context.ChatClientManager.GetResponseAsync(
                context.GetMessagesForLLM(),
                chatOptions,
                cancellationToken);
            var llmCallDuration = (DateTime.UtcNow - llmCallStart).TotalMilliseconds;
            PlayFrameworkMetrics.DecrementActiveLlmCalls();
            PlayFrameworkMetrics.RecordLlmCall(
                provider: context.ChatClientManager.GetType().Name,
                model: "direct",
                success: true,
                durationMs: llmCallDuration,
                promptTokens: responseWithCost.InputTokens,
                completionTokens: responseWithCost.OutputTokens);
            _logger.LogDebug("Scene-selection LLM call (non-streaming) completed in {Duration:F1}ms (Factory: {FactoryName})",
                llmCallDuration, factoryNameString);

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
            context.ExecutionPhase = ExecutionPhase.BudgetExceeded;
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
                _logger.LogInformation("Scene '{SceneName}' selected via Direct mode (Factory: {FactoryName})",
                    selectedSceneName, factoryNameString);

                yield return YieldAndTrack(context, new AiSceneResponse
                {
                    Status = AiResponseStatus.ExecutingScene,
                    Message = $"Selected scene: {selectedSceneName}",
                    SceneName = selectedSceneName,
                    Contents = multiModalContents
                });

                // Execute the selected scene
                var scene = dependencies.SceneFactory.TryGetScene(selectedSceneName);
                if (scene != null)
                {
                    // ✅ CRITICAL: Add tool result to conversation BEFORE entering scene
                    // OpenAI requires: assistant message with tool_calls → tool message with result
                    var toolResult = new FunctionResultContent(functionCall.CallId, functionCall.Name)
                    {
                        Result = $"Scene '{scene.Name}' loaded successfully. Available tools: {string.Join(", ", scene.Tools.Select(t => t.Name))}"
                    };

                    var toolMessage = new ChatMessage(ChatRole.Tool, [toolResult]);
                    context.AddToolMessage(toolMessage);

                    await foreach (var sceneResponse in sceneExecutor.ExecuteSceneAsync(context, scene, settings, cancellationToken))
                    {
                        yield return sceneResponse;
                    }

                    yield break;
                }
                else
                {
                    // ✅ Also add tool result for error case
                    var toolResult = new FunctionResultContent(functionCall.CallId, functionCall.Name)
                    {
                        Result = $"Error: Scene '{selectedSceneName}' not found"
                    };

                    var toolMessage = new ChatMessage(ChatRole.Tool, [toolResult]);
                    context.AddToolMessage(toolMessage);

                    _logger.LogWarning("Scene '{SceneName}' not found in Direct execution mode (Factory: {FactoryName})",
                        selectedSceneName, factoryNameString);

                    yield return YieldAndTrack(context, new AiSceneResponse
                    {
                        Status = AiResponseStatus.Error,
                        Message = $"Scene '{selectedSceneName}' not found"
                    });
                    context.ExecutionPhase = ExecutionPhase.SceneNotFound;
                    yield break;
                }
            }
        }

        // Fallback: if no function call, return text response with multi-modal contents
        // If streaming was enabled and already streamed, send only completion marker (no message duplication)
        _logger.LogDebug("Direct execution: no scene selected via function call, returning LLM text response (Factory: {FactoryName})", factoryNameString);
        if (settings.EnableStreaming && streamedToUser)
        {
            yield return dependencies.ResponseHelper.CreateFinalResponse(
                sceneName: null,
                message: string.Empty, // ✅ Empty - text already streamed
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
