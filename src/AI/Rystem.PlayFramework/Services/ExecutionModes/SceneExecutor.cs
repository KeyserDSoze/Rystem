using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rystem.PlayFramework.Helpers;
using Rystem.PlayFramework.Mcp;
using Rystem.PlayFramework.Services.Helpers;

namespace Rystem.PlayFramework.Services.ExecutionModes;

/// <summary>
/// Clean implementation of scene executor with pending tools execution strategy.
/// Uses IToolExecutionManager for centralized tool execution and sanitization.
/// </summary>
internal sealed class SceneExecutor : ISceneExecutor, IFactoryName
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IFactory<IMcpServerManager> _mcpServerManagerFactory;
    private readonly IFactory<IJsonService> _jsonServiceFactory;
    private readonly IToolExecutionManager _toolExecutionManager;

    private ExecutionModeHandlerDependencies _dependencies = null!;
    private string _factoryName = "default";
    private IJsonService _jsonService = null!;

    public SceneExecutor(
        IServiceProvider serviceProvider,
        IFactory<IMcpServerManager> mcpServerManagerFactory,
        IFactory<IJsonService> jsonServiceFactory,
        IToolExecutionManager toolExecutionManager)
    {
        _serviceProvider = serviceProvider;
        _mcpServerManagerFactory = mcpServerManagerFactory;
        _jsonServiceFactory = jsonServiceFactory;
        _toolExecutionManager = toolExecutionManager;
    }
    public bool FactoryNameAlreadySetup { get; set; }
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

        // Check if resuming from client interaction
        var isResumingFromClientInteraction = settings.ClientInteractionResults != null && settings.ClientInteractionResults.Count > 0
            && context.Properties.ContainsKey("_continuation_sceneName");

        // Execute scene actors only if NOT resuming from client interaction
        if (!isResumingFromClientInteraction)
        {
            await scene.ExecuteActorsAsync(context, cancellationToken);
        }

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

        // Add MCP system messages (resources and prompts) to ConversationHistory for tracking/caching
        if (mcpSystemMessages.Count > 0)
        {
            var combinedMcpMessage = string.Join("\n\n---\n\n", mcpSystemMessages);
            context.AddMcpContextMessage(scene.Name, combinedMcpMessage);

            _dependencies.Logger.LogDebug("Added MCP context message to conversation history (Total length: {MessageLength} chars, Factory: {FactoryName})",
                combinedMcpMessage.Length, _factoryName);
        }

        // Combine scene tools and MCP tools
        var allTools = new List<AITool>();
        allTools.AddRange(scene.AiTools);
        allTools.AddRange(mcpTools);

        var chatOptions = new ChatOptions
        {
            Tools = allTools
        };

        _dependencies.Logger.LogDebug("Scene {SceneName} executing with {SceneToolCount} scene tools + {McpToolCount} MCP tools (Factory: {FactoryName})",
            scene.Name, scene.AiTools.Count, mcpTools.Count, _factoryName);

        // STEP 1: If resuming from client interaction, use ToolExecutionManager
        if (settings.ClientInteractionResults != null && settings.ClientInteractionResults.Count > 0)
        {
            await foreach (var result in _toolExecutionManager.ResumeAfterClientResponseAsync(
                context,
                settings.ClientInteractionResults,
                scene.Tools,
                mcpTools,
                scene.Name,
                cancellationToken))
            {
                yield return ConvertToolResult(result, scene.Name, context);
            }
        }

        // Tool calling loop - continue until LLM stops calling tools
        const int MaxToolCallIterations = 10;
        var iteration = 0;

        while (iteration < MaxToolCallIterations)
        {
            iteration++;

            ChatMessage? finalMessage = null;
            var accumulatedText = string.Empty;
            List<FunctionCallContent> accumulatedFunctionCalls = [];
            var streamedToUser = false;
            decimal? totalCost = null;
            int? totalInputTokens = null;
            int? totalOutputTokens = null;
            int? totalCachedInputTokens = null;

            // Rebuild messages from context each iteration - using centralized sanitization
            var conversationMessages = _toolExecutionManager.GetMessagesForLLM(context);

            // Use streaming when enabled, fallback to non-streaming otherwise
            if (settings.EnableStreaming)
            {
                StreamingResult? lastResult = null;
                await foreach (var result in _dependencies.StreamingHelper.ProcessOptimisticStreamAsync(
                    context,
                    conversationMessages,
                    chatOptions,
                    scene.Name,
                    cancellationToken))
                {
                    lastResult = result;

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
                // NON-STREAMING MODE
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

            // Add assistant message to conversation history
            context.AddAssistantMessage(finalMessage);

            // Process based on what we detected
            if (accumulatedFunctionCalls.Count == 0)
            {
                // Extract multi-modal contents
                var finalContents = finalMessage.Contents?
                    .Where(c => c is DataContent or UriContent)
                    .ToList();

                // Pure text response - finalize streaming if enabled
                if (settings.EnableStreaming && streamedToUser)
                {
                    yield return _dependencies.ResponseHelper.CreateFinalResponse(
                        sceneName: scene.Name,
                        message: string.Empty, // Text already streamed
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

            // Function calls detected - track costs
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

            // STEP 3: Execute function calls using centralized ToolExecutionManager
            await foreach (var result in _toolExecutionManager.ExecuteToolCallsAsync(
                context,
                accumulatedFunctionCalls,
                scene.Tools,
                mcpTools,
                scene.ClientInteractionDefinitions,
                scene.Name,
                cancellationToken))
            {
                var response = ConvertToolResult(result, scene.Name, context);
                yield return response;

                // If awaiting client, yield break
                if (result.Status == ToolExecutionStatus.AwaitingClient)
                {
                    yield break;
                }
            }
        }

        // Max iterations reached
        yield return _dependencies.ResponseHelper.CreateErrorResponse(
            sceneName: scene.Name,
            message: $"Maximum tool calling iterations ({MaxToolCallIterations}) exceeded",
            errorMessage: "The conversation exceeded the maximum number of tool calling iterations.",
            context: context,
            inputTokens: null,
            outputTokens: null,
            cachedInputTokens: null,
            cost: null);

        context.ExecutionPhase = ExecutionPhase.Completed;
    }

    /// <summary>
    /// Converts ToolExecutionResult to AiSceneResponse.
    /// </summary>
    private AiSceneResponse ConvertToolResult(ToolExecutionResult result, string sceneName, SceneContext context)
    {
        return result.Status switch
        {
            ToolExecutionStatus.Started => _dependencies.ResponseHelper.CreateAndTrackResponse(
                context: context,
                status: AiResponseStatus.FunctionRequest,
                sceneName: sceneName,
                message: result.Message ?? $"Executing tool: {result.ToolName}",
                functionName: result.ToolName),

            ToolExecutionStatus.Completed => _dependencies.ResponseHelper.CreateAndTrackResponse(
                context: context,
                status: AiResponseStatus.FunctionCompleted,
                sceneName: sceneName,
                message: result.Message ?? $"Tool {result.ToolName} completed",
                functionName: result.ToolName),

            ToolExecutionStatus.AwaitingClient => new AiSceneResponse
            {
                Status = AiResponseStatus.AwaitingClient,
                ConversationKey = context.ConversationKey,
                ClientInteractionRequest = result.ClientRequest,
                Message = result.Message ?? $"Awaiting client execution of tool: {result.ToolName}"
            },

            ToolExecutionStatus.Error => _dependencies.ResponseHelper.CreateErrorResponse(
                sceneName: sceneName,
                message: result.Message ?? $"Tool execution failed",
                errorMessage: result.Error,
                context: context),

            _ => new AiSceneResponse
            {
                Status = AiResponseStatus.Running,
                Message = result.Message
            }
        };
    }

    /// <summary>
    /// Create AIFunction wrapper for MCP tool.
    /// This is no longer used - MCP tools are added directly from McpServerManager.
    /// Kept for reference but should be removed if McpServerManager handles conversion.
    /// </summary>
    private AIFunction CreateMcpToolFunction(McpTool mcpTool, IMcpServerManager mcpManager)
    {
        var normalizedName = ToolNameNormalizer.Normalize(mcpTool.Name);

        return AIFunctionFactory.Create(
            (IDictionary<string, object?> arguments, CancellationToken ct) =>
            {
                // Serialize arguments to JSON for MCP call
                var argsJson = _jsonService.Serialize(arguments);
                return mcpManager.ExecuteToolAsync(mcpTool.Name, argsJson, ct);
            },
            normalizedName,
            mcpTool.Description);
    }

    private static AiSceneResponse YieldStatus(AiResponseStatus status, string message)
    {
        return new AiSceneResponse
        {
            Status = status,
            Message = message,
            Timestamp = DateTime.UtcNow
        };
    }
}
