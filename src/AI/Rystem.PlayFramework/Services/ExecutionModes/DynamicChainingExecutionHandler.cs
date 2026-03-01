using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Rystem.PlayFramework.Services.ExecutionModes;

/// <summary>
/// Handles Dynamic Chaining execution mode - LLM selects and chains multiple scenes.
/// </summary>
internal sealed class DynamicChainingExecutionHandler : IExecutionModeHandler
{
    private readonly IFactory<ExecutionModeHandlerDependencies> _dependenciesFactory;
    private readonly IFactory<ISceneExecutor> _sceneExecutorFactory;
    private readonly IFactory<FinalResponseGenerator> _finalResponseGeneratorFactory;

    public DynamicChainingExecutionHandler(
        IFactory<ExecutionModeHandlerDependencies> dependenciesFactory,
        IFactory<ISceneExecutor> sceneExecutorFactory,
        IFactory<FinalResponseGenerator> finalResponseGeneratorFactory)
    {
        _dependenciesFactory = dependenciesFactory;
        _sceneExecutorFactory = sceneExecutorFactory;
        _finalResponseGeneratorFactory = finalResponseGeneratorFactory;
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

        var finalResponseGenerator = _finalResponseGeneratorFactory.Create(factoryName)
            ?? throw new InvalidOperationException($"FinalResponseGenerator not found for factory: {factoryName}");

        var factoryNameString = factoryName?.ToString() ?? "default";
        yield return YieldStatus(AiResponseStatus.Running, "Starting dynamic scene chaining");

        var sceneExecutionCount = 0;

        while (sceneExecutionCount < settings.MaxDynamicScenes)
        {
            // All scenes are available for selection — scenes can be re-executed
            // when the task requires multiple rounds. The LLM decides based on
            // execution context (BuildChainingContext) whether re-use makes sense.
            var availableScenes = dependencies.SceneFactory.Scenes.ToList();

            if (availableScenes.Count == 0)
            {
                yield return YieldStatus(AiResponseStatus.Running, "No scenes available");
                break;
            }

            // Select next scene to execute
            yield return YieldStatus(AiResponseStatus.Running, $"Round {sceneExecutionCount + 1}/{settings.MaxDynamicScenes}");

            var selectedScene = await SelectSceneForChainingAsync(dependencies, context, availableScenes, settings, cancellationToken);
            if (selectedScene == null)
            {
                yield return YieldStatus(AiResponseStatus.Running, "No suitable scene found, ending chain");
                break;
            }

            yield return YieldAndTrack(context, new AiSceneResponse
            {
                Status = AiResponseStatus.ExecutingScene,
                SceneName = selectedScene.Name,
                Message = $"Executing scene: {selectedScene.Name}"
            });

            // Execute the selected scene
            var sceneResultBuilder = new StringBuilder();
            await foreach (var response in sceneExecutor.ExecuteSceneAsync(context, selectedScene, settings, cancellationToken))
            {
                // Accumulate scene results from various status types (not just Running)
                if (!string.IsNullOrWhiteSpace(response.Message))
                {
                    // Include Running, FunctionRequest, FunctionCompleted messages for complete context
                    if (response.Status == AiResponseStatus.Running ||
                        response.Status == AiResponseStatus.FunctionRequest ||
                        response.Status == AiResponseStatus.FunctionCompleted)
                    {
                        sceneResultBuilder.AppendLine(response.Message);
                    }
                }

                yield return response;

                // Check for budget exceeded
                if (response.Status == AiResponseStatus.BudgetExceeded)
                {
                    context.ExecutionPhase = ExecutionPhase.Completed;
                    yield break;
                }

                // If scene is awaiting client interaction or command execution,
                // stop the entire chain — the conversation history now contains an
                // assistant message with tool_calls that has no tool response yet.
                // Continuing would cause "tool_calls must be followed by tool messages" errors.
                if (response.Status == AiResponseStatus.AwaitingClient
                    || response.Status == AiResponseStatus.CommandClient)
                {
                    yield break;
                }
            }

            // Store scene result
            var sceneResult = sceneResultBuilder.ToString();
            context.SceneResults[selectedScene.Name] = sceneResult;
            context.ExecutedSceneOrder.Add(selectedScene.Name);

            sceneExecutionCount++;

            // Ask LLM if it needs to continue to another scene (or re-execute one)
            if (sceneExecutionCount < settings.MaxDynamicScenes)
            {
                yield return YieldStatus(AiResponseStatus.Running, "Evaluating if more scenes are needed");

                var shouldContinue = await AskContinueToNextSceneAsync(dependencies, factoryNameString, context, settings, cancellationToken);
                if (!shouldContinue)
                {
                    yield return YieldStatus(AiResponseStatus.Running, "Scene chain complete - generating final response");
                    break;
                }
            }
        }

        if (sceneExecutionCount >= settings.MaxDynamicScenes)
        {
            yield return YieldStatus(AiResponseStatus.Running, $"Maximum scene limit ({settings.MaxDynamicScenes}) reached");
        }

        // Generate final response using all accumulated scene results
        await foreach (var response in finalResponseGenerator.GenerateAsync(context, settings, cancellationToken))
        {
            yield return response;
        }
    }

    private async Task<IScene?> SelectSceneForChainingAsync(
        ExecutionModeHandlerDependencies dependencies,
        SceneContext context,
        List<IScene> availableScenes,
        SceneRequestSettings settings,
        CancellationToken cancellationToken)
    {
        // Create scene selection tools from available scenes
        var sceneTools = availableScenes
            .Select(scene => scene.AiTool)
            .ToList();

        var chatOptions = new ChatOptions
        {
            Tools = sceneTools
        };

        // Use full conversation history from context (includes all previous messages, tool calls, and results)
        var messages = context.GetMessagesForLLM();

        // Optionally add execution context as system message for additional guidance
        var contextMessage = BuildChainingContext(context);
        if (!string.IsNullOrWhiteSpace(contextMessage))
        {
            messages.Add(new ChatMessage(ChatRole.System, contextMessage));
        }

        // Call LLM for scene selection
        var responseWithCost = await context.ChatClientManager.GetResponseAsync(
            messages,
            chatOptions,
            cancellationToken);

        // Track costs
        context.AddCost(responseWithCost.CalculatedCost);

        // Extract function call and save to conversation history
        var responseMessage = responseWithCost.Response.Messages?.FirstOrDefault();
        if (responseMessage != null)
        {
            context.AddAssistantMessage(responseMessage);
        }

        var functionCall = responseMessage?.Contents?.OfType<FunctionCallContent>().FirstOrDefault();

        if (functionCall != null)
        {
            var selectedSceneName = functionCall.Name;
            var selectedScene = dependencies.SceneFactory.TryGetScene(selectedSceneName);

            // Add tool result to conversation history (required by OpenAI/Azure OpenAI)
            // Assistant message with tool_calls MUST be followed by tool message with result
            var toolResult = new FunctionResultContent(functionCall.CallId, functionCall.Name)
            {
                Result = selectedScene != null 
                    ? $"Scene '{selectedScene.Name}' selected successfully. Description: {selectedScene.Description}. Available tools: {string.Join(", ", selectedScene.Tools.Select(t => t.Name))}"
                    : $"Error: Scene '{selectedSceneName}' not found"
            };
            var toolMessage = new ChatMessage(ChatRole.Tool, [toolResult]);
            context.AddToolMessage(toolMessage);

            return selectedScene;
        }

        return null;
    }

    private async Task<bool> AskContinueToNextSceneAsync(
        ExecutionModeHandlerDependencies dependencies,
        string factoryNameString,
        SceneContext context,
        SceneRequestSettings settings,
        CancellationToken cancellationToken)
    {
        // Build summary of what has been done
        var executionSummary = BuildExecutionSummary(context);

        // All scenes are available — scenes can be re-executed if needed
        var allScenes = dependencies.SceneFactory.SceneNames.ToList();

        if (allScenes.Count == 0)
        {
            return false; // No scenes available
        }

        var prompt = $@"Based on the user's original request: ""{context.InputMessage}""

Execution so far:
{executionSummary}

All available scenes (scenes can be re-executed if needed):
{string.Join("\n", allScenes.Select(s => $"- {s}{(context.ExecutedScenes.ContainsKey(s) ? " [already executed]" : "")}"))}

Decide if you need to execute another scene (or re-execute a previous one) to complete the user's request.
Use the decideContinuation tool to indicate your decision.";

        // Create a forced tool for the continuation decision
        var continuationTool = AIFunctionFactory.Create(
            ([System.ComponentModel.Description("Set to true if more scenes are needed to complete the user's request, false if you have enough information.")] bool shouldContinue,
             [System.ComponentModel.Description("Brief explanation of why you made this decision.")] string? reasoning) => shouldContinue,
            "decideContinuation",
            "Decide whether to continue executing more scenes or stop and generate the final response.");

        var chatOptions = new ChatOptions
        {
            Tools = [continuationTool],
            ToolMode = ChatToolMode.RequireAny // Force the LLM to call the tool
        };

        var responseWithCost = await context.ChatClientManager.GetResponseAsync(
            [new ChatMessage(ChatRole.User, prompt)],
            chatOptions,
            cancellationToken);

        // Track costs
        context.AddCost(responseWithCost.CalculatedCost);

        // Extract the boolean from the function call and save to conversation history
        var responseMessage = responseWithCost.Response.Messages?.FirstOrDefault();
        if (responseMessage != null)
        {
            context.AddAssistantMessage(responseMessage);
        }

        var functionCall = responseMessage?.Contents?.OfType<FunctionCallContent>().FirstOrDefault();

        bool shouldContinue = false;
        string? reasoning = null;

        if (functionCall?.Arguments != null &&
            functionCall.Arguments.TryGetValue("shouldContinue", out var shouldContinueValue))
        {
            // Extract reasoning if available
            if (functionCall.Arguments.TryGetValue("reasoning", out var reasoningValue))
            {
                reasoning = reasoningValue?.ToString();
            }

            // Handle various formats the LLM might return
            if (shouldContinueValue is bool boolValue)
            {
                shouldContinue = boolValue;
            }
            else if (shouldContinueValue is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.True) shouldContinue = true;
                else if (jsonElement.ValueKind == JsonValueKind.False) shouldContinue = false;
                else if (jsonElement.ValueKind == JsonValueKind.String)
                {
                    shouldContinue = bool.TryParse(jsonElement.GetString(), out var parsed) && parsed;
                }
            }
            else if (shouldContinueValue is string strValue)
            {
                shouldContinue = bool.TryParse(strValue, out var parsed) && parsed;
            }

            // Add tool result to conversation history (required by OpenAI/Azure OpenAI)
            // Assistant message with tool_calls MUST be followed by tool message with result
            var toolResult = new FunctionResultContent(functionCall.CallId, functionCall.Name)
            {
                Result = $"Decision: {(shouldContinue ? "continue" : "stop")}. {(reasoning != null ? $"Reasoning: {reasoning}" : "")}"
            };
            var toolMessage = new ChatMessage(ChatRole.Tool, [toolResult]);
            context.AddToolMessage(toolMessage);

            return shouldContinue;
        }

        // Fallback: if tool wasn't called properly, default to false (stop chaining)
        dependencies.Logger.LogWarning("Failed to extract continuation decision from LLM response. Defaulting to stop chaining. (Factory: {FactoryName})", factoryNameString);
        return false;
    }

    private static string BuildChainingContext(SceneContext context)
    {
        if (context.ExecutedSceneOrder.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.AppendLine("Previously executed scenes and their results:");
        builder.AppendLine();

        foreach (var sceneName in context.ExecutedSceneOrder)
        {
            if (context.SceneResults.TryGetValue(sceneName, out var result))
            {
                builder.AppendLine($"Scene: {sceneName}");
                builder.AppendLine($"Result: {result}");
                builder.AppendLine();
            }
        }

        // Add information about executed tools
        if (context.ExecutedScenes.Count > 0)
        {
            builder.AppendLine("Executed tools:");
            foreach (var (sceneName, tools) in context.ExecutedScenes)
            {
                foreach (var tool in tools)
                {
                    builder.AppendLine($"- {sceneName}.{tool.ToolName}({tool.Arguments ?? "no args"})");
                }
            }
        }

        return builder.ToString();
    }

    private static string BuildExecutionSummary(SceneContext context)
    {
        var builder = new StringBuilder();

        foreach (var sceneName in context.ExecutedSceneOrder)
        {
            builder.AppendLine($"✓ Executed scene: {sceneName}");

            if (context.ExecutedScenes.TryGetValue(sceneName, out var tools))
            {
                foreach (var tool in tools)
                {
                    builder.AppendLine($"  - Tool: {tool.ToolName}");
                }
            }

            if (context.SceneResults.TryGetValue(sceneName, out var result) && !string.IsNullOrWhiteSpace(result))
            {
                var preview = result.Length > 200 ? result.Substring(0, 200) + "..." : result;
                builder.AppendLine($"  Result preview: {preview}");
            }

            builder.AppendLine();
        }

        return builder.ToString();
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
