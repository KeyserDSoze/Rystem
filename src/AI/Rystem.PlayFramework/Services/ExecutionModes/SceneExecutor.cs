using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rystem.PlayFramework.Helpers;
using Rystem.PlayFramework.Mcp;
using Rystem.PlayFramework.Services.Helpers;
using Rystem.PlayFramework.Telemetry;

namespace Rystem.PlayFramework.Services.ExecutionModes;

/// <summary>
/// Clean implementation of scene executor with pending tools execution strategy.
/// Uses IToolExecutionManager for centralized tool execution and sanitization.
/// </summary>
internal sealed class SceneExecutor : ISceneExecutor, IFactoryName
{
    private sealed record LoadedMcpTool(McpTool Tool, AIFunction Function);

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
        using var activity = PlayFrameworkActivitySource.Instance.StartActivity(
            PlayFrameworkActivitySource.Activities.SceneExecute, ActivityKind.Internal);
        activity?.SetTag(PlayFrameworkActivitySource.Tags.SceneName, scene.Name);
        activity?.SetTag(PlayFrameworkActivitySource.Tags.SceneDescription, scene.Description);
        activity?.AddEvent(new ActivityEvent(PlayFrameworkActivitySource.Events.SceneStarted));

        var sceneStartTime = DateTime.UtcNow;
        _dependencies.Logger.LogInformation("Entering scene '{SceneName}' (Factory: {FactoryName})", scene.Name, _factoryName);

        yield return YieldStatus(AiResponseStatus.ExecutingScene, $"Entering scene: {scene.Name}");

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

        // Check if resuming from client interaction (batch was already resolved by SceneManager)
        var isResumingFromClientInteraction = settings.ClientInteractionResults != null && settings.ClientInteractionResults.Count > 0;

        // Execute scene actors only if NOT resuming from client interaction
        if (!isResumingFromClientInteraction)
        {
            await scene.ExecuteActorsAsync(context, cancellationToken);
        }

        // Load MCP tools, resources, and prompts
        var loadedMcpTools = new List<LoadedMcpTool>();
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
                        loadedMcpTools.Add(new LoadedMcpTool(tool, aiFunction));
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

            if (loadedMcpTools.Count > 0)
            {
                _dependencies.Logger.LogInformation("Loaded {McpToolCount} MCP tools for scene: {SceneName} (Factory: {FactoryName})",
                    loadedMcpTools.Count, scene.Name, _factoryName);
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

        var availableSceneTools = scene.Tools;
        var availableSceneAiTools = scene.AiTools;
        var availableMcpTools = loadedMcpTools;

        var forcedToolsForScene = GetForcedToolsForScene(settings, scene.Name);
        if (forcedToolsForScene.Count > 0)
        {
            availableSceneTools = [.. scene.Tools.Where(x => IsForcedToolMatch(x, forcedToolsForScene))];
            availableSceneAiTools = [.. availableSceneTools.Select(x => x.ToolDescription)];
            availableMcpTools = [.. loadedMcpTools.Where(x => IsForcedMcpToolMatch(x.Tool, forcedToolsForScene))];

            var missingForcedTools = forcedToolsForScene
                .Where(x => !availableSceneTools.Any(tool => IsForcedToolMatch(tool, x))
                    && !availableMcpTools.Any(tool => IsForcedMcpToolMatch(tool.Tool, x)))
                .ToList();

            if (missingForcedTools.Count > 0)
            {
                var missingToolNames = string.Join(", ", missingForcedTools.Select(x => x.ToolName));
                yield return _dependencies.ResponseHelper.CreateErrorResponse(
                    sceneName: scene.Name,
                    message: $"Forced tools not available for scene '{scene.Name}'",
                    errorMessage: $"The following forced tools were not found: {missingToolNames}",
                    context: context);

                context.ExecutionPhase = ExecutionPhase.Break;
                yield break;
            }

            _dependencies.Logger.LogInformation(
                "Scene {SceneName} constrained to {SceneToolCount} scene tool(s) and {McpToolCount} MCP tool(s) by request settings (Factory: {FactoryName})",
                scene.Name,
                availableSceneTools.Count,
                availableMcpTools.Count,
                _factoryName);
        }

        _dependencies.Logger.LogDebug("Scene {SceneName} executing with {SceneToolCount} scene tools + {McpToolCount} MCP tools (Factory: {FactoryName})",
            scene.Name, availableSceneAiTools.Count, availableMcpTools.Count, _factoryName);

        // Client interactions are now resolved by SceneManager.InitializePlayFrameworkAsync
        // via ToolExecutionManager.ResolveClientInteractionsAsync before entering this method.
        // No need to call ResumeAfterClientResponseAsync here.

        // Tool calling loop - continue until LLM stops calling tools
        const int MaxToolCallIterations = 10;
        var iteration = 0;

        while (iteration < MaxToolCallIterations)
        {
            iteration++;

            var chatOptions = CreateChatOptions(scene.Name, settings, context, availableSceneAiTools, availableMcpTools);

            ChatMessage? finalMessage = null;
            var accumulatedText = string.Empty;
            List<FunctionCallContent> accumulatedFunctionCalls = [];
            var streamedToUser = false;
            decimal? totalCost = null;
            int? totalInputTokens = null;
            int? totalOutputTokens = null;
            int? totalCachedInputTokens = null;

            // Rebuild messages from context each iteration - using centralized sanitization
            var conversationMessages = context.GetMessagesForLLM();

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

                context.ExecutionPhase = ExecutionPhase.CompletedNoResponse;
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
                if (context.ExecutionPhase != ExecutionPhase.AwaitingClient)
                    context.ExecutionPhase = ExecutionPhase.Completed;
                yield break;
            }

            // Function calls detected - track costs
            if (totalCost.HasValue)
            {
                yield return _dependencies.ResponseHelper.CreateAndTrackResponse(
                    context: context,
                    status: AiResponseStatus.ExecutingScene,
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

                context.ExecutionPhase = ExecutionPhase.BudgetExceeded;
                yield break;
            }

            // STEP 3: Execute function calls using centralized ToolExecutionManager
            var hasClientInteraction = false;
            await foreach (var result in _toolExecutionManager.ExecuteToolCallsAsync(
                context,
                accumulatedFunctionCalls,
                availableSceneTools,
                [.. availableMcpTools.Select(x => x.Function)],
                scene.ClientInteractionDefinitions,
                scene.Name,
                _jsonService,
                cancellationToken))
            {
                var response = ConvertToolResult(result, scene.Name, context);
                yield return response;

                // Track any client interaction (both AwaitingClient and CommandClient)
                if (result.Status is ToolExecutionStatus.AwaitingClient
                                  or ToolExecutionStatus.CommandClient)
                {
                    hasClientInteraction = true;
                }
            }

            // Break AFTER yielding ALL client interactions to the client
            if (hasClientInteraction)
            {
                yield break;
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

        context.ExecutionPhase = ExecutionPhase.TooManyToolRequests;
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

            ToolExecutionStatus.AwaitingClient or ToolExecutionStatus.CommandClient => new AiSceneResponse
            {
                Status = result.ClientRequest?.IsCommand == true ? AiResponseStatus.CommandClient : AiResponseStatus.AwaitingClient,
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

    private static ChatOptions CreateChatOptions(
        string sceneName,
        SceneRequestSettings settings,
        SceneContext context,
        IReadOnlyCollection<AITool> sceneTools,
        IReadOnlyCollection<LoadedMcpTool> mcpTools)
    {
        var chatOptions = new ChatOptions
        {
            Tools = [.. sceneTools, .. mcpTools.Select(x => x.Function)]
        };

        var pendingForcedTools = GetPendingForcedTools(settings, sceneName, context);
        if (pendingForcedTools.Count == 1)
        {
            chatOptions.ToolMode = ChatToolMode.RequireSpecific(pendingForcedTools[0].ToolName);
        }
        else if (pendingForcedTools.Count > 1)
        {
            chatOptions.ToolMode = ChatToolMode.RequireAny;
            chatOptions.AllowMultipleToolCalls = true;
        }

        return chatOptions;
    }

    private static List<ForcedToolRequest> GetForcedToolsForScene(SceneRequestSettings settings, string sceneName)
        => settings.ForcedTools?
            .Where(x => ToolNameNormalizer.Matches(sceneName, x.SceneName))
            .Select(x => new ForcedToolRequest
            {
                SceneName = sceneName,
                ToolName = ToolNameNormalizer.Normalize(x.ToolName),
                SourceType = x.SourceType,
                SourceName = x.SourceName,
                MemberName = x.MemberName
            })
            .ToList()
            ?? [];

    private static List<ForcedToolRequest> GetPendingForcedTools(
        SceneRequestSettings settings,
        string sceneName,
        SceneContext context)
        => GetForcedToolsForScene(settings, sceneName)
            .Where(x => !context.ExecutedTools.Contains(BuildExecutedToolKey(sceneName, x.ToolName)))
            .ToList();

    private static string BuildExecutedToolKey(string sceneName, string toolName)
        => $"{sceneName}.{ToolNameNormalizer.Normalize(toolName)}";

    private static bool IsForcedToolMatch(ISceneTool tool, IReadOnlyCollection<ForcedToolRequest> forcedTools)
        => forcedTools.Any(x => IsForcedToolMatch(tool, x));

    private static bool IsForcedToolMatch(ISceneTool tool, ForcedToolRequest forcedTool)
    {
        var normalizedToolName = ToolNameNormalizer.Normalize(tool.Name);
        if (!string.Equals(normalizedToolName, ToolNameNormalizer.Normalize(forcedTool.ToolName), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (tool is not ISceneToolMetadata metadata)
        {
            return forcedTool.SourceType is null or PlayFrameworkToolSourceType.Other;
        }

        return MatchesMetadata(
            forcedTool,
            metadata.SourceType,
            metadata.SourceName,
            metadata.MemberName);
    }

    private static bool IsForcedMcpToolMatch(McpTool tool, IReadOnlyCollection<ForcedToolRequest> forcedTools)
        => forcedTools.Any(x => IsForcedMcpToolMatch(tool, x));

    private static bool IsForcedMcpToolMatch(McpTool tool, ForcedToolRequest forcedTool)
    {
        var normalizedToolName = ToolNameNormalizer.Normalize(tool.Name);
        if (!string.Equals(normalizedToolName, ToolNameNormalizer.Normalize(forcedTool.ToolName), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return MatchesMetadata(
            forcedTool,
            PlayFrameworkToolSourceType.Mcp,
            tool.FactoryName,
            tool.Name);
    }

    private static bool MatchesMetadata(
        ForcedToolRequest forcedTool,
        PlayFrameworkToolSourceType sourceType,
        string? sourceName,
        string? memberName)
    {
        if (forcedTool.SourceType.HasValue && forcedTool.SourceType.Value != sourceType)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(forcedTool.SourceName)
            && !string.Equals(forcedTool.SourceName, sourceName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(forcedTool.MemberName)
            && !string.Equals(forcedTool.MemberName, memberName, StringComparison.OrdinalIgnoreCase)
            && !ToolNameNormalizer.Matches(memberName, forcedTool.MemberName))
        {
            return false;
        }

        return true;
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
