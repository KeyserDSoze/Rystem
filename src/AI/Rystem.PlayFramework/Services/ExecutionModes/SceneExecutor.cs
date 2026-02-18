using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rystem.PlayFramework.Mcp;
using Rystem.PlayFramework.Services.Helpers;

namespace Rystem.PlayFramework.Services.ExecutionModes;

/// <summary>
/// Default implementation of scene executor.
/// Handles tool calling loop, MCP integration, client interactions, and streaming.
/// </summary>
internal sealed class SceneExecutor : ISceneExecutor, IFactoryName
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IFactory<IMcpServerManager> _mcpServerManagerFactory;
    private readonly IFactory<IJsonService> _jsonServiceFactory;
    private readonly IClientInteractionHandler _clientInteractionHandler;

    private ExecutionModeHandlerDependencies _dependencies = null!;
    private string _factoryName = "default";
    private IJsonService _jsonService = null!;

    public SceneExecutor(
        IServiceProvider serviceProvider,
        IFactory<IMcpServerManager> mcpServerManagerFactory,
        IFactory<IJsonService> jsonServiceFactory,
        IClientInteractionHandler clientInteractionHandler)
    {
        _serviceProvider = serviceProvider;
        _mcpServerManagerFactory = mcpServerManagerFactory;
        _jsonServiceFactory = jsonServiceFactory;
        _clientInteractionHandler = clientInteractionHandler;
    }

    public void SetFactoryName(AnyOf<string?, Enum>? name)
    {
        _factoryName = name?.ToString() ?? "default";

        var dependenciesFactory = _serviceProvider.GetRequiredService<IFactory<ExecutionModeHandlerDependencies>>();
        _dependencies = dependenciesFactory.Create(name)
            ?? throw new InvalidOperationException($"ExecutionModeHandlerDependencies not found for factory: {name}");

        _jsonService = _jsonServiceFactory.Create(name) ?? new DefaultJsonService();
    }
    public async IAsyncEnumerable<AiSceneResponse> ExecuteSceneAsync(
        SceneContext context,
        IScene scene,
        SceneRequestSettings settings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return YieldStatus(AiResponseStatus.Running, $"Entering scene: {scene.Name}");

        // Track this scene as being executed
        if (!context.ExecutedScenes.ContainsKey(scene.Name))
        {
            context.ExecutedScenes[scene.Name] = [];
        }
        if (!context.ExecutedSceneOrder.Contains(scene.Name))
        {
            context.ExecutedSceneOrder.Add(scene.Name);
        }

        context.ExecutionPhase = ExecutionPhase.ExecutingScene;
        // Execute scene actors
        await scene.ExecuteActorsAsync(context, cancellationToken);

        // Load MCP tools, resources, and prompts
        var mcpTools = new List<AIFunction>();
        var mcpSystemMessages = new List<string>();

        if (scene.McpServerReferences.Count > 0)
        {
            _dependencies.Logger.LogDebug("Loading MCP capabilities from {McpServerCount} server(s) for scene: {SceneName} (Factory: {FactoryName})",
                scene.McpServerReferences.Count, scene.Name, _factoryName);

            foreach (var mcpRef in scene.McpServerReferences)
            {
                try
                {
                    var mcpManager = _mcpServerManagerFactory.Create(mcpRef.FactoryName);

                    // Load tools
                    var tools = await mcpManager.GetToolsAsync(mcpRef.FilterSettings, cancellationToken);
                    _dependencies.Logger.LogDebug("Loaded {ToolCount} MCP tools from server {McpServerName} (Factory: {FactoryName})",
                        tools.Count, mcpRef.FactoryName, _factoryName);

                    // Convert MCP tools to AIFunction and add to chat options
                    foreach (var tool in tools)
                    {
                        var aiFunction = CreateMcpToolFunction(tool, mcpManager);
                        mcpTools.Add(aiFunction);
                    }

                    // Build system message from resources and prompts
                    var systemMessage = await mcpManager.BuildSystemMessageAsync(mcpRef.FilterSettings, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(systemMessage))
                    {
                        mcpSystemMessages.Add(systemMessage);
                        _dependencies.Logger.LogDebug("Built MCP system message from server {McpServerName} (Length: {MessageLength} chars, Factory: {FactoryName})",
                            mcpRef.FactoryName, systemMessage.Length, _factoryName);
                    }
                }
                catch (Exception ex)
                {
                    _dependencies.Logger.LogError(ex, "Failed to load MCP capabilities from server {McpServerName} (Factory: {FactoryName})",
                        mcpRef.FactoryName, _factoryName);
                }
            }

            if (mcpTools.Count > 0)
            {
                _dependencies.Logger.LogInformation("Loaded {McpToolCount} MCP tools for scene: {SceneName} (Factory: {FactoryName})",
                    mcpTools.Count, scene.Name, _factoryName);
            }
        }

        // Get scene tools
        var sceneTools = scene.GetTools().ToList();
        var sceneToolsFunctions = sceneTools.Select(t => t.ToAITool()).ToList();

        // Add MCP system messages (resources and prompts) to ConversationHistory for tracking/caching
        if (mcpSystemMessages.Count > 0)
        {
            var combinedMcpMessage = string.Join("\n\n---\n\n", mcpSystemMessages);
            context.AddMcpContextMessage(scene.Name, combinedMcpMessage);

            _dependencies.Logger.LogDebug("Added MCP context message to conversation history (Total length: {MessageLength} chars, Factory: {FactoryName})",
                combinedMcpMessage.Length, _factoryName);
        }

        // Combine scene tools and MCP tools
        var allTools = new List<AITool>(sceneToolsFunctions);
        allTools.AddRange(mcpTools);

        var chatOptions = new ChatOptions
        {
            Tools = allTools
        };

        _dependencies.Logger.LogDebug("Scene {SceneName} executing with {SceneToolCount} scene tools + {McpToolCount} MCP tools (Factory: {FactoryName})",
            scene.Name, sceneToolsFunctions.Count, mcpTools.Count, _factoryName);

        // If resuming from client interaction, inject the client results into conversation
        if (settings.ClientInteractionResults != null && settings.ClientInteractionResults.Count > 0)
        {
            // Retrieve original FunctionCallContent info saved during continuation
            var originalCallId = context.Properties.TryGetValue("_continuation_callId", out var cid) ? cid as string : null;
            var originalToolName = context.Properties.TryGetValue("_continuation_toolName", out var tn) ? tn as string : null;

            foreach (var clientResult in settings.ClientInteractionResults)
            {
                // Build FunctionResultContent from client result
                string resultText;
                if (!string.IsNullOrEmpty(clientResult.Error))
                {
                    resultText = $"Client tool error: {clientResult.Error}";
                }
                else if (clientResult.Contents != null && clientResult.Contents.Count > 0)
                {
                    // Convert client contents to descriptive text for the LLM
                    var contentParts = new List<string>();
                    foreach (var content in clientResult.Contents)
                    {
                        if (string.Equals(content.Type, "text", StringComparison.OrdinalIgnoreCase))
                        {
                            contentParts.Add(content.Text ?? "");
                        }
                        else if (string.Equals(content.Type, "data", StringComparison.OrdinalIgnoreCase))
                        {
                            contentParts.Add($"[Binary data: {content.MediaType ?? "unknown"}]");
                        }
                        else
                        {
                            contentParts.Add($"[Content: {content.Type}]");
                        }
                    }
                    resultText = string.Join("\n", contentParts);
                }
                else
                {
                    resultText = "Client tool executed successfully (no data returned)";
                }

                // Use original callId/name from the LLM's FunctionCallContent for proper correlation
                var functionResult = new FunctionResultContent(
                    originalCallId ?? clientResult.InteractionId,
                    originalToolName ?? clientResult.InteractionId)
                {
                    Result = resultText
                };
                context.AddToolMessage(new ChatMessage(ChatRole.Tool, [functionResult]));

                _dependencies.Logger.LogInformation("Injected client interaction result for '{InteractionId}' into conversation (Factory: {FactoryName})",
                    clientResult.InteractionId, _factoryName);
            }
        }

        // Tool calling loop - continue until LLM stops calling tools
        const int MaxToolCallIterations = 10;
        var iteration = 0;

        while (iteration < MaxToolCallIterations)
        {
            iteration++;

            ChatMessage? finalMessage = null;
            string accumulatedText = string.Empty;
            List<FunctionCallContent> accumulatedFunctionCalls = [];
            bool streamedToUser = false;
            decimal? totalCost = null;
            int? totalInputTokens = null;
            int? totalOutputTokens = null;
            int? totalCachedInputTokens = null;

            // Rebuild messages from context each iteration (includes newly added assistant/tool/mcp messages)
            var conversationMessages = context.GetMessagesForLLM();

            // Use streaming when enabled, fallback to non-streaming otherwise
            if (settings.EnableStreaming)
            {
                // Use StreamingHelper for optimistic streaming
                StreamingResult? lastResult = null;
                await foreach (var result in _dependencies.StreamingHelper.ProcessOptimisticStreamAsync(
                    context,
                    conversationMessages,
                    chatOptions,
                    scene.Name,
                    cancellationToken))
                {
                    lastResult = result;

                    // Stream to user if needed
                    if (result.FinalMessage == null && result.StreamedToUser)
                    {
                        yield return _dependencies.ResponseHelper.CreateStreamingResponse(
                            sceneName: scene.Name,
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
                    conversationMessages,
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

            // Validate response
            if (finalMessage == null)
            {
                yield return _dependencies.ResponseHelper.CreateErrorResponse(
                    sceneName: scene.Name,
                    message: "No response from LLM",
                    errorMessage: null,
                    context: context,
                    inputTokens: totalInputTokens,
                    outputTokens: totalOutputTokens,
                    cachedInputTokens: totalCachedInputTokens,
                    cost: totalCost);

                if (settings.MaxBudget.HasValue && context.TotalCost > settings.MaxBudget.Value)
                {
                    yield return _dependencies.ResponseHelper.CreateBudgetExceededResponse(
                        sceneName: scene.Name,
                        maxBudget: settings.MaxBudget.Value,
                        totalCost: context.TotalCost,
                        currency: context.ChatClientManager.Currency);
                }

                context.ExecutionPhase = ExecutionPhase.Completed;

                yield break;
            }

            // Add assistant message to conversation history (persists for cache/memory)
            context.AddAssistantMessage(finalMessage);

            // Process based on what we detected
            if (accumulatedFunctionCalls.Count == 0)
            {
                // Extract multi-modal contents from final message
                var finalContents = finalMessage.Contents?
                    .Where(c => c is DataContent or UriContent)
                    .ToList();

                // Pure text response - finalize streaming if enabled
                if (settings.EnableStreaming && streamedToUser)
                {
                    // Already streamed to user, just send final chunk with costs
                    yield return _dependencies.ResponseHelper.CreateFinalResponse(
                        sceneName: scene.Name,
                        message: accumulatedText,
                        context: context,
                        inputTokens: totalInputTokens,
                        outputTokens: totalOutputTokens,
                        cachedInputTokens: totalCachedInputTokens,
                        cost: totalCost,
                        streamingChunk: string.Empty,
                        isStreamingComplete: true,
                        contents: finalContents);

                    _dependencies.Logger.LogInformation("Native streaming completed for scene {SceneName} (Factory: {FactoryName})",
                        scene.Name, _factoryName);
                }
                else
                {
                    // Non-streaming mode or no streaming happened
                    yield return _dependencies.ResponseHelper.CreateFinalResponse(
                        sceneName: scene.Name,
                        message: accumulatedText,
                        context: context,
                        inputTokens: totalInputTokens,
                        outputTokens: totalOutputTokens,
                        cachedInputTokens: totalCachedInputTokens,
                        cost: totalCost,
                        contents: finalContents);
                }

                // Check budget
                if (settings.MaxBudget.HasValue && context.TotalCost > settings.MaxBudget.Value)
                {
                    yield return _dependencies.ResponseHelper.CreateBudgetExceededResponse(
                        sceneName: scene.Name,
                        maxBudget: settings.MaxBudget.Value,
                        totalCost: context.TotalCost,
                        currency: context.ChatClientManager.Currency);
                }

                context.ExecutionPhase = ExecutionPhase.Completed;

                yield break;
            }

            // Function calls detected - track costs and prepare for tool execution
            if (totalCost.HasValue)
            {
                yield return _dependencies.ResponseHelper.CreateAndTrackResponse(
                    context: context,
                    status: AiResponseStatus.Running,
                    sceneName: scene.Name,
                    message: $"LLM returned {accumulatedFunctionCalls.Count} function call(s)",
                    inputTokens: totalInputTokens,
                    outputTokens: totalOutputTokens,
                    cachedInputTokens: totalCachedInputTokens,
                    cost: totalCost);
            }

            // Check budget before executing tools
            if (settings.MaxBudget.HasValue && context.TotalCost > settings.MaxBudget.Value)
            {
                yield return _dependencies.ResponseHelper.CreateBudgetExceededResponse(
                    sceneName: scene.Name,
                    maxBudget: settings.MaxBudget.Value,
                    totalCost: context.TotalCost,
                    currency: context.ChatClientManager.Currency);

                context.ExecutionPhase = ExecutionPhase.Completed;

                yield break;
            }

            // Execute each function call
            foreach (var functionCall in accumulatedFunctionCalls)
            {
                // Check if this is a client-side tool FIRST (before server tool lookup)
                var clientRequest = scene.ClientInteractionDefinitions != null
                    ? _clientInteractionHandler.CreateRequestIfClientTool(
                        scene.ClientInteractionDefinitions,
                        functionCall.Name,
                        functionCall.Arguments?.ToDictionary(x => x.Key, x => x.Value))
                    : null;

                if (clientRequest != null)
                {
                    // Save continuation info in context.Properties (will be cached)
                    context.SetProperty("_continuation_sceneName", scene.Name);
                    context.SetProperty("_continuation_interactionId", clientRequest.InteractionId);
                    context.SetProperty("_continuation_callId", functionCall.CallId);
                    context.SetProperty("_continuation_toolName", functionCall.Name);

                    _dependencies.Logger.LogInformation("Client tool '{ToolName}' detected. Awaiting client execution for conversation '{ConversationKey}'",
                        functionCall.Name, context.ConversationKey);

                    // Yield AwaitingClient status with request - client uses ConversationKey to resume
                    yield return new AiSceneResponse
                    {
                        Status = AiResponseStatus.AwaitingClient,
                        ConversationKey = context.ConversationKey,
                        ClientInteractionRequest = clientRequest,
                        Message = $"Awaiting client execution of tool: {functionCall.Name}"
                    };

                    context.ExecutionPhase = ExecutionPhase.AwaitingClient;

                    // Stop execution - client will resume with new POST using ConversationKey + ClientInteractionResults
                    yield break;
                }

                // Find the server-side tool
                var tool = sceneTools.FirstOrDefault(t => t.Name == functionCall.Name);
                if (tool == null)
                {
                    // Tool not found - send error result
                    var errorResult = new FunctionResultContent(functionCall.CallId, functionCall.Name)
                    {
                        Result = $"Tool '{functionCall.Name}' not found"
                    };
                    context.AddToolMessage(new ChatMessage(ChatRole.Tool, [errorResult]));
                    continue;
                }

                // Track tool execution
                var toolKey = $"{scene.Name}.{functionCall.Name}";
                context.ExecutedTools.Add(toolKey);

                // Execute tool
                yield return _dependencies.ResponseHelper.CreateStatusResponse(
                    status: AiResponseStatus.FunctionRequest,
                    message: $"Executing tool: {functionCall.Name}");

                AiSceneResponse toolResponse;
                try
                {
                    // Serialize arguments to JSON using IJsonService
                    var argsJson = _jsonService.Serialize(functionCall.Arguments ?? new Dictionary<string, object?>());

                    // Execute the tool
                    var toolResult = await tool.ExecuteAsync(argsJson, context, cancellationToken);

                    // Send result back to LLM
                    var functionResult = new FunctionResultContent(functionCall.CallId, functionCall.Name);

                    // Support multi-modal output from tools
                    if (toolResult is AIContent aiContent)
                    {
                        // Tool returned AIContent directly (DataContent, UriContent, etc.)
                        functionResult.Result = aiContent;
                    }
                    else if (toolResult is IEnumerable<AIContent> aiContents)
                    {
                        // Tool returned list of AIContent
                        functionResult.Result = aiContents;
                    }
                    else
                    {
                        // Tool returned string or other object - use as-is
                        functionResult.Result = toolResult;
                    }

                    context.AddToolMessage(new ChatMessage(ChatRole.Tool, [functionResult]));

                    toolResponse = _dependencies.ResponseHelper.CreateAndTrackResponse(
                        context: context,
                        status: AiResponseStatus.FunctionCompleted,
                        sceneName: scene.Name,
                        message: $"Tool {functionCall.Name} executed: {toolResult}",
                        functionName: functionCall.Name);
                }
                catch (Exception ex)
                {
                    // Send error result
                    var errorResult = new FunctionResultContent(functionCall.CallId, functionCall.Name)
                    {
                        Result = $"Error executing tool: {ex.Message}"
                    };
                    context.AddToolMessage(new ChatMessage(ChatRole.Tool, [errorResult]));

                    toolResponse = _dependencies.ResponseHelper.CreateErrorResponse(
                        sceneName: scene.Name,
                        message: $"Tool execution failed: {ex.Message}",
                        errorMessage: ex.Message,
                        context: context);
                }

                yield return toolResponse;
            }

            // Continue loop to get next LLM response after function results
        }

        // Max iterations reached
        yield return YieldAndTrack(context, new AiSceneResponse
        {
            Status = AiResponseStatus.Error,
            SceneName = scene.Name,
            Message = $"Maximum tool call iterations ({MaxToolCallIterations}) reached"
        });
    }

    private static AiSceneResponse YieldStatus(AiResponseStatus status, string? message = null, decimal? cost = null)
    {
        return new AiSceneResponse
        {
            Status = status,
            Message = message,
            Cost = cost,
            TotalCost = cost ?? 0
        };
    }

    private static AiSceneResponse YieldAndTrack(SceneContext context, AiSceneResponse response)
    {
        response.TotalCost = context.TotalCost;
        context.Responses.Add(response);
        return response;
    }

    private AIFunction CreateMcpToolFunction(McpTool mcpTool, IMcpServerManager mcpManager)
    {
        return AIFunctionFactory.Create(
            async (string argsJson) =>
            {
                _dependencies.Logger.LogDebug("Executing MCP tool: {ToolName} from server {ServerUrl} (Factory: {FactoryName})",
                    mcpTool.Name, mcpTool.ServerUrl, _factoryName);

                try
                {
                    var result = await mcpManager.ExecuteToolAsync(mcpTool.Name, argsJson, CancellationToken.None);

                    _dependencies.Logger.LogInformation("MCP tool {ToolName} executed successfully (Factory: {FactoryName})",
                        mcpTool.Name, _factoryName);

                    return result;
                }
                catch (Exception ex)
                {
                    _dependencies.Logger.LogError(ex, "Failed to execute MCP tool {ToolName} from server {ServerUrl} (Factory: {FactoryName})",
                        mcpTool.Name, mcpTool.ServerUrl, _factoryName);

                    return $"Error executing MCP tool: {ex.Message}";
                }
            },
            mcpTool.Name,
            mcpTool.Description);
    }


}
