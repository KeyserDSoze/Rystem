using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rystem.PlayFramework.Mcp;

namespace Rystem.PlayFramework;

/// <summary>
/// Main orchestrator for PlayFramework execution.
/// </summary>
internal sealed class SceneManager : ISceneManager, IFactoryName
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SceneManager> _logger;
    private readonly IFactory<ISceneFactory> _sceneFactoryFactory;
    private readonly IFactory<IChatClient> _chatClientFactory;
    private readonly IFactory<PlayFrameworkSettings> _settingsFactory;
    private readonly IFactory<List<ActorConfiguration>> _mainActorsFactory;
    private readonly IFactory<ICostCalculator> _costCalculatorFactory;
    private readonly IFactory<IPlanner> _plannerFactory;
    private readonly IFactory<ISummarizer> _summarizerFactory;
    private readonly IFactory<IDirector> _directorFactory;
    private readonly IFactory<ICacheService> _cacheServiceFactory;
    private readonly IFactory<IJsonService> _jsonServiceFactory;
    private readonly IFactory<IMcpServerManager> _mcpServerManagerFactory;

    // Resolved dependencies (set via SetFactoryName)
    private string? _factoryName;
    private ISceneFactory _sceneFactory = null!;
    private IChatClient _chatClient = null!;
    private PlayFrameworkSettings _settings = null!;
    private List<ActorConfiguration> _mainActors = null!;
    private ICostCalculator _costCalculator = null!;
    private IPlanner? _planner;
    private ISummarizer? _summarizer;
    private IDirector? _director;
    private ICacheService? _cacheService;
    private IJsonService _jsonService = null!;

    public SceneManager(
        IServiceProvider serviceProvider,
        ILogger<SceneManager> logger,
        IFactory<ISceneFactory> sceneFactoryFactory,
        IFactory<IChatClient> chatClientFactory,
        IFactory<PlayFrameworkSettings> settingsFactory,
        IFactory<List<ActorConfiguration>> mainActorsFactory,
        IFactory<ICostCalculator> costCalculatorFactory,
        IFactory<IPlanner> plannerFactory,
        IFactory<ISummarizer> summarizerFactory,
        IFactory<IDirector> directorFactory,
        IFactory<ICacheService> cacheServiceFactory,
        IFactory<IJsonService> jsonServiceFactory,
        IFactory<IMcpServerManager> mcpServerManagerFactory)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _sceneFactoryFactory = sceneFactoryFactory;
        _chatClientFactory = chatClientFactory;
        _settingsFactory = settingsFactory;
        _mainActorsFactory = mainActorsFactory;
        _costCalculatorFactory = costCalculatorFactory;
        _plannerFactory = plannerFactory;
        _summarizerFactory = summarizerFactory;
        _directorFactory = directorFactory;
        _cacheServiceFactory = cacheServiceFactory;
        _jsonServiceFactory = jsonServiceFactory;
        _mcpServerManagerFactory = mcpServerManagerFactory;
    }

    public void SetFactoryName(AnyOf<string?, Enum>? name)
    {
        _factoryName = name?.ToString() ?? "default";

        _logger.LogDebug("Initializing SceneManager for factory: {FactoryName}", _factoryName);

        _sceneFactory = _sceneFactoryFactory.Create(name) ?? throw new InvalidOperationException($"SceneFactory not found for name: {name}");
        _logger.LogTrace("SceneFactory resolved: {SceneFactoryType} (Factory: {FactoryName})", _sceneFactory.GetType().Name, _factoryName);

        // Try to get ChatClient from factory, fall back to service provider if not found
        _chatClient = _chatClientFactory.Create(name) 
            ?? _serviceProvider.GetService<IChatClient>()
            ?? throw new InvalidOperationException($"IChatClient not found. Please register IChatClient with AddFactory or as a singleton service.");
        _logger.LogTrace("ChatClient resolved: {ChatClientType} (Factory: {FactoryName})", _chatClient.GetType().Name, _factoryName);

        _settings = _settingsFactory.Create(name) ?? new PlayFrameworkSettings();
        _logger.LogDebug("Settings loaded - ExecutionMode: {ExecutionMode}, Planning: {PlanningEnabled}, Cache: {CacheEnabled} (Factory: {FactoryName})", 
            _settings.DefaultExecutionMode, _settings.Planning.Enabled, _settings.Cache.Enabled, _factoryName);

        _mainActors = _mainActorsFactory.Create(name) ?? [];
        _logger.LogDebug("{ActorCount} main actors loaded (Factory: {FactoryName})", _mainActors.Count, _factoryName);

        _costCalculator = _costCalculatorFactory.Create(name) ?? new CostCalculator(new TokenCostSettings { Enabled = false });
        _logger.LogDebug("Cost calculator initialized - Enabled: {CostTrackingEnabled}, Currency: {Currency} (Factory: {FactoryName})", 
            _costCalculator.IsEnabled, _costCalculator.Currency, _factoryName);

        _planner = _plannerFactory.Create(name);
        _summarizer = _summarizerFactory.Create(name);
        _director = _directorFactory.Create(name);
        _cacheService = _cacheServiceFactory.Create(name);
        _jsonService = _jsonServiceFactory.Create(name) ?? new DefaultJsonService();

        var availableScenes = _sceneFactory.GetSceneNames().Count();
        _logger.LogInformation("SceneManager initialized successfully - Scenes: {SceneCount}, Planner: {HasPlanner}, Summarizer: {HasSummarizer}, Director: {HasDirector}, Cache: {HasCache} (Factory: {FactoryName})", 
            availableScenes, _planner != null, _summarizer != null, _director != null, _cacheService != null, _factoryName);
    }

    public async IAsyncEnumerable<AiSceneResponse> ExecuteAsync(
        string message,
        SceneRequestSettings? settings = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        settings ??= new SceneRequestSettings();

        // Initialize
        yield return YieldStatus(AiResponseStatus.Initializing, "Initializing context");

        var context = await InitializeContextAsync(message, settings, cancellationToken);

        // Load from cache if available
        if (settings.CacheBehavior != CacheBehavior.Avoidable && _cacheService != null && context.CacheKey != null)
        {
            yield return YieldStatus(AiResponseStatus.LoadingCache, "Loading from cache");

            var cached = await _cacheService.GetAsync(context.CacheKey, cancellationToken);
            if (cached != null)
            {
                // Check if summarization needed for cached data
                if (_summarizer != null && _summarizer.ShouldSummarize(cached))
                {
                    yield return YieldStatus(AiResponseStatus.Summarizing, "Summarizing cached conversation");

                    var summary = await _summarizer.SummarizeAsync(cached, cancellationToken);
                    context.ConversationSummary = summary;

                    // Store summary in context to include in future messages
                    context.Properties["conversation_summary"] = summary;
                }
                else
                {
                    // Replay cached messages
                    foreach (var cachedResponse in cached)
                    {
                        context.Responses.Add(cachedResponse);
                    }
                }
            }
        }

        // Execute main actors
        yield return YieldStatus(AiResponseStatus.ExecutingMainActors, "Executing main actors");
        await ExecuteMainActorsAsync(context, cancellationToken);

        // Determine execution mode (request setting overrides default)
        var executionMode = settings.ExecutionMode ?? _settings.DefaultExecutionMode;

        // Route to appropriate execution method based on mode
        switch (executionMode)
        {
            case SceneExecutionMode.Planning:
                if (_planner == null)
                {
                    yield return YieldAndTrack(context, new AiSceneResponse
                    {
                        Status = AiResponseStatus.Error,
                        ErrorMessage = "Planning mode requested but no IPlanner is registered"
                    });
                    yield break;
                }

                // Create execution plan
                yield return YieldStatus(AiResponseStatus.Planning, "Creating execution plan");

                var plan = await _planner.CreatePlanAsync(context, settings, cancellationToken);
                context.ExecutionPlan = plan;

                if (!plan.NeedsExecution)
                {
                    // Direct answer available
                    yield return YieldAndTrack(context, new AiSceneResponse
                    {
                        Status = AiResponseStatus.Running,
                        Message = plan.Reasoning
                    });
                }
                else
                {
                    // Execute plan
                    await foreach (var response in ExecutePlanAsync(context, settings, plan, cancellationToken))
                    {
                        yield return response;
                    }
                }
                break;

            case SceneExecutionMode.DynamicChaining:
                // Dynamic scene chaining mode
                await foreach (var response in DynamicChainAsync(context, settings, cancellationToken))
                {
                    yield return response;
                }
                break;

            case SceneExecutionMode.Direct:
            default:
                // Direct execution (no planning)
                await foreach (var response in RequestAsync(context, settings, cancellationToken))
                {
                    yield return response;
                }
                break;
        }

        // Save to cache
        if (settings.CacheBehavior != CacheBehavior.Avoidable && _cacheService != null && context.CacheKey != null)
        {
            yield return YieldStatus(AiResponseStatus.SavingCache, "Saving to cache");
            await _cacheService.SetAsync(context.CacheKey, context.Responses, settings.CacheBehavior, cancellationToken);
        }

        yield return YieldStatus(AiResponseStatus.Completed, "Execution completed", context.TotalCost);
    }

    private Task<SceneContext> InitializeContextAsync(
        string message,
        SceneRequestSettings settings,
        CancellationToken cancellationToken)
    {
        var context = new SceneContext
        {
            ServiceProvider = _serviceProvider,
            InputMessage = message,
            ChatClient = _chatClient,
            CacheKey = settings.CacheKey ?? Guid.NewGuid().ToString(),
            CacheBehavior = settings.CacheBehavior
        };

        return Task.FromResult(context);
    }

    private async Task ExecuteMainActorsAsync(SceneContext context, CancellationToken cancellationToken)
    {
        foreach (var actorConfig in _mainActors)
        {
            var actor = ActorFactory.Create(actorConfig, _serviceProvider);
            var response = await actor.PlayAsync(context, cancellationToken);

            if (!string.IsNullOrWhiteSpace(response.Message))
            {
                // Store actor message in context
                context.Properties[$"main_actor_{_mainActors.IndexOf(actorConfig)}"] = response.Message;

                // Track for planner
                context.MainActorContext.Add(response.Message);
            }
        }
    }

    private async IAsyncEnumerable<AiSceneResponse> ExecutePlanAsync(
        SceneContext context,
        SceneRequestSettings settings,
        ExecutionPlan plan,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken,
        int recursionDepth = 0)
    {
        if (recursionDepth >= settings.MaxRecursionDepth)
        {
            yield return YieldAndTrack(context, new AiSceneResponse
            {
                Status = AiResponseStatus.Error,
                ErrorMessage = $"Maximum recursion depth ({settings.MaxRecursionDepth}) reached"
            });
            yield break;
        }

        // Execute each step in order
        foreach (var step in plan.Steps.OrderBy(s => s.StepNumber))
        {
            if (step.IsCompleted)
            {
                continue;
            }

            yield return YieldStatus(AiResponseStatus.ExecutingScene, $"Executing step {step.StepNumber}: {step.SceneName}");

            // Execute scene for this step
            var scene = FindSceneByFuzzyMatch(step.SceneName);
            if (scene == null)
            {
                yield return YieldAndTrack(context, new AiSceneResponse
                {
                    Status = AiResponseStatus.Error,
                    SceneName = step.SceneName,
                    ErrorMessage = $"Scene '{step.SceneName}' not found"
                });
                continue;
            }

            // Execute scene
            await foreach (var response in ExecuteSceneAsync(context, scene, settings, cancellationToken))
            {
                yield return response;
            }

            step.IsCompleted = true;
        }

        // Check if we need to continue (all steps completed? can we answer now?)
        var allStepsCompleted = plan.Steps.All(s => s.IsCompleted);
        if (allStepsCompleted)
        {
            // Generate final response
            await foreach (var response in GenerateFinalResponseAsync(context, settings, cancellationToken))
            {
                yield return response;
            }
        }
    }

    private async IAsyncEnumerable<AiSceneResponse> RequestAsync(
        SceneContext context,
        SceneRequestSettings settings,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Add user message
        var userMessage = new ChatMessage(ChatRole.User, context.InputMessage);

        // Get all scenes as tools for selection
        var sceneTools = _sceneFactory.GetSceneNames()
            .Select(name => _sceneFactory.Create(name))
            .Select(scene => CreateSceneSelectionTool(scene))
            .ToList();

        // Configure chat with scene selection tools
        var chatOptions = new ChatOptions
        {
            Tools = sceneTools.Cast<AITool>().ToList()
        };

        // Call LLM for scene selection
        var response = await context.ChatClient.GetResponseAsync(
            new[] { userMessage },
            chatOptions,
            cancellationToken);

        // Track costs for scene selection
        var selectionResponse = new AiSceneResponse
        {
            Status = AiResponseStatus.Running,
            Message = "Scene selection"
        };
        var canContinue = TrackCosts(response, selectionResponse, context, settings);

        // Check budget limit
        if (!canContinue)
        {
            yield return YieldAndTrack(context, new AiSceneResponse
            {
                Status = AiResponseStatus.BudgetExceeded,
                Message = $"Budget limit of {settings.MaxBudget:F6} {_costCalculator.Currency} exceeded. Total cost: {context.TotalCost:F6}",
                ErrorMessage = "Maximum budget reached"
            });
            yield break;
        }

        // Process function calls (scene selections)
        var responseMessage = response.Messages?.FirstOrDefault();
        if (responseMessage?.Contents != null)
        {
            foreach (var content in responseMessage.Contents)
            {
                if (content is FunctionCallContent functionCall)
                {
                    // Scene selection via function call
                    var selectedSceneName = functionCall.Name;

                    yield return YieldAndTrack(context, new AiSceneResponse
                    {
                        Status = AiResponseStatus.Running,
                        Message = $"Selected scene: {selectedSceneName}",
                        SceneName = selectedSceneName
                    });

                    // Execute the selected scene
                    var scene = FindSceneByFuzzyMatch(selectedSceneName);
                    if (scene != null)
                    {
                        await foreach (var sceneResponse in ExecuteSceneAsync(context, scene, settings, cancellationToken))
                        {
                            yield return sceneResponse;
                        }
                    }
                    else
                    {
                        yield return YieldAndTrack(context, new AiSceneResponse
                        {
                            Status = AiResponseStatus.Error,
                            Message = $"Scene '{selectedSceneName}' not found"
                        });
                    }

                    yield break; // Exit after processing scene selection
                }
            }
        }

        // Fallback: if no function call, return text response
        yield return YieldAndTrack(context, new AiSceneResponse
        {
            Status = AiResponseStatus.Running,
            Message = responseMessage?.Text ?? "No response from LLM"
        });
    }

    private async IAsyncEnumerable<AiSceneResponse> ExecuteSceneAsync(
        SceneContext context,
        IScene scene,
        SceneRequestSettings settings,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return YieldStatus(AiResponseStatus.Running, $"Entering scene: {scene.Name}");

        // Execute scene actors
        await scene.ExecuteActorsAsync(context, cancellationToken);

        // Load MCP tools, resources, and prompts
        var mcpTools = new List<AIFunction>();
        var mcpSystemMessages = new List<string>();

        if (scene.McpServerReferences.Count > 0)
        {
            _logger.LogDebug("Loading MCP capabilities from {McpServerCount} server(s) for scene: {SceneName} (Factory: {FactoryName})",
                scene.McpServerReferences.Count, scene.Name, _factoryName);

            foreach (var mcpRef in scene.McpServerReferences)
            {
                try
                {
                    var mcpManager = _mcpServerManagerFactory.Create(mcpRef.FactoryName);

                    // Load tools
                    var tools = await mcpManager.GetToolsAsync(mcpRef.FilterSettings, cancellationToken);
                    _logger.LogDebug("Loaded {ToolCount} MCP tools from server {McpServerName} (Factory: {FactoryName})",
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
                        _logger.LogDebug("Built MCP system message from server {McpServerName} (Length: {MessageLength} chars, Factory: {FactoryName})",
                            mcpRef.FactoryName, systemMessage.Length, _factoryName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load MCP capabilities from server {McpServerName} (Factory: {FactoryName})",
                        mcpRef.FactoryName, _factoryName);
                }
            }

            if (mcpTools.Count > 0)
            {
                _logger.LogInformation("Loaded {McpToolCount} MCP tools for scene: {SceneName} (Factory: {FactoryName})",
                    mcpTools.Count, scene.Name, _factoryName);
            }
        }

        // Get scene tools
        var sceneTools = scene.GetTools().ToList();
        var sceneToolsFunctions = sceneTools.Select(t => t.ToAIFunction()).ToList();

        // Build conversation messages
        var conversationMessages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, context.InputMessage)
        };

        // Add main actor context if available
        if (context.MainActorContext.Count > 0)
        {
            var mainActorText = string.Join("\n\n", context.MainActorContext);
            conversationMessages.Add(new ChatMessage(ChatRole.System, mainActorText));
        }

        // Add MCP system messages (resources and prompts)
        if (mcpSystemMessages.Count > 0)
        {
            var combinedMcpMessage = string.Join("\n\n---\n\n", mcpSystemMessages);
            conversationMessages.Add(new ChatMessage(ChatRole.System, combinedMcpMessage));

            _logger.LogDebug("Added MCP system message to conversation (Total length: {MessageLength} chars, Factory: {FactoryName})",
                combinedMcpMessage.Length, _factoryName);
        }

        // Combine scene tools and MCP tools
        var allTools = sceneToolsFunctions.Cast<AITool>().ToList();
        allTools.AddRange(mcpTools.Cast<AITool>());

        var chatOptions = new ChatOptions
        {
            Tools = allTools
        };

        _logger.LogDebug("Scene {SceneName} executing with {SceneToolCount} scene tools + {McpToolCount} MCP tools (Factory: {FactoryName})",
            scene.Name, sceneToolsFunctions.Count, mcpTools.Count, _factoryName);

        // Tool calling loop - continue until LLM stops calling tools
        const int MaxToolCallIterations = 10;
        var iteration = 0;

        while (iteration < MaxToolCallIterations)
        {
            iteration++;

            // Call LLM
            var response = await context.ChatClient.GetResponseAsync(
                conversationMessages,
                chatOptions,
                cancellationToken);

            var responseMessage = response.Messages?.FirstOrDefault();
            if (responseMessage == null)
            {
                var errorResponse = new AiSceneResponse
                {
                    Status = AiResponseStatus.Error,
                    SceneName = scene.Name,
                    Message = "No response from LLM"
                };
                var canContinue = TrackCosts(response, errorResponse, context, settings);
                yield return YieldAndTrack(context, errorResponse);

                if (!canContinue)
                {
                    yield return YieldAndTrack(context, new AiSceneResponse
                    {
                        Status = AiResponseStatus.BudgetExceeded,
                        SceneName = scene.Name,
                        Message = $"Budget limit of {settings.MaxBudget:F6} {_costCalculator.Currency} exceeded. Total cost: {context.TotalCost:F6}",
                        ErrorMessage = "Maximum budget reached"
                    });
                }

                yield break;
            }

            // Add assistant message to conversation
            conversationMessages.Add(responseMessage);

            // Check for function calls
            var functionCalls = responseMessage.Contents?
                .OfType<FunctionCallContent>()
                .ToList() ?? [];

            if (functionCalls.Count == 0)
            {
                // No more function calls - check if streaming is enabled
                if (settings.EnableStreaming)
                {
                    // Stream the final text response
                    await foreach (var streamResponse in StreamTextResponseAsync(
                        responseMessage,
                        response,
                        scene.Name,
                        context,
                        settings,
                        cancellationToken))
                    {
                        yield return streamResponse;
                    }
                }
                else
                {
                    // Non-streaming: return complete response
                    var finalResponse = new AiSceneResponse
                    {
                        Status = AiResponseStatus.Running,
                        SceneName = scene.Name,
                        Message = responseMessage.Text ?? string.Empty
                    };
                    var canContinue = TrackCosts(response, finalResponse, context, settings);
                    yield return YieldAndTrack(context, finalResponse);

                    if (!canContinue)
                    {
                        yield return YieldAndTrack(context, new AiSceneResponse
                        {
                            Status = AiResponseStatus.BudgetExceeded,
                            SceneName = scene.Name,
                            Message = $"Budget limit of {settings.MaxBudget:F6} {_costCalculator.Currency} exceeded. Total cost: {context.TotalCost:F6}",
                            ErrorMessage = "Maximum budget reached"
                        });
                    }
                }

                yield break;
            }

            // Track costs for this LLM response (even if it contains function calls)
            var llmResponse = new AiSceneResponse
            {
                Status = AiResponseStatus.Running,
                SceneName = scene.Name,
                Message = $"LLM returned {functionCalls.Count} function call(s)"
            };
            var canContinueAfterLLM = TrackCosts(response, llmResponse, context, settings);

            // Only yield if there are costs to report
            if (llmResponse.Cost.HasValue)
            {
                yield return YieldAndTrack(context, llmResponse);
            }

            // Check budget before executing tools
            if (!canContinueAfterLLM)
            {
                yield return YieldAndTrack(context, new AiSceneResponse
                {
                    Status = AiResponseStatus.BudgetExceeded,
                    SceneName = scene.Name,
                    Message = $"Budget limit of {settings.MaxBudget:F6} {_costCalculator.Currency} exceeded. Total cost: {context.TotalCost:F6}",
                    ErrorMessage = "Maximum budget reached"
                });
                yield break;
            }

            // Execute each function call
            foreach (var functionCall in functionCalls)
            {
                // Find the tool
                var tool = sceneTools.FirstOrDefault(t => t.Name == functionCall.Name);
                if (tool == null)
                {
                    // Tool not found - send error result
                    var errorResult = new FunctionResultContent(functionCall.CallId, functionCall.Name)
                    {
                        Result = $"Tool '{functionCall.Name}' not found"
                    };
                    conversationMessages.Add(new ChatMessage(ChatRole.Tool, [errorResult]));
                    continue;
                }

                // Track tool execution
                var toolKey = $"{scene.Name}.{functionCall.Name}";
                context.ExecutedTools.Add(toolKey);

                // Execute tool - store responses before yielding
                var statusResponse = YieldStatus(AiResponseStatus.FunctionRequest, $"Executing tool: {functionCall.Name}");
                yield return statusResponse;

                AiSceneResponse resultResponse;
                try
                {
                    // Serialize arguments to JSON using IJsonService
                    var argsJson = _jsonService.Serialize(functionCall.Arguments ?? new Dictionary<string, object?>());

                    // Execute the tool
                    var toolResult = await tool.ExecuteAsync(argsJson, context, cancellationToken);

                    // Send result back to LLM
                    var functionResult = new FunctionResultContent(functionCall.CallId, functionCall.Name)
                    {
                        Result = toolResult
                    };
                    conversationMessages.Add(new ChatMessage(ChatRole.Tool, [functionResult]));

                    resultResponse = YieldAndTrack(context, new AiSceneResponse
                    {
                        Status = AiResponseStatus.FunctionCompleted,
                        SceneName = scene.Name,
                        Message = $"Tool {functionCall.Name} executed: {toolResult}",
                        FunctionName = functionCall.Name
                    });
                }
                catch (Exception ex)
                {
                    // Send error result
                    var errorResult = new FunctionResultContent(functionCall.CallId, functionCall.Name)
                    {
                        Result = $"Error executing tool: {ex.Message}"
                    };
                    conversationMessages.Add(new ChatMessage(ChatRole.Tool, [errorResult]));

                    resultResponse = YieldAndTrack(context, new AiSceneResponse
                    {
                        Status = AiResponseStatus.Error,
                        SceneName = scene.Name,
                        ErrorMessage = ex.Message,
                        Message = $"Tool execution failed: {ex.Message}",
                        FunctionName = functionCall.Name
                    });
                }

                yield return resultResponse;
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

    private async IAsyncEnumerable<AiSceneResponse> GenerateFinalResponseAsync(
        SceneContext context,
        SceneRequestSettings settings,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
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
            yield break;
        }

        // Generate final response based on gathered data
        var finalPrompt = new ChatMessage(ChatRole.User, "Based on all the information gathered, provide the final answer to the user's request.");

        if (settings.EnableStreaming)
        {
            // Streaming mode
            await foreach (var streamChunk in context.ChatClient.GetStreamingResponseAsync(
                new[] { finalPrompt },
                cancellationToken: cancellationToken))
            {
                await foreach (var streamResponse in ProcessStreamingChunkAsync(
                    streamChunk,
                    null, // No scene name for final response
                    context,
                    settings,
                    cancellationToken))
                {
                    yield return streamResponse;
                }
            }
        }
        else
        {
            // Non-streaming mode
            var response = await context.ChatClient.GetResponseAsync(
                new[] { finalPrompt },
                cancellationToken: cancellationToken);

            var finalResponse = new AiSceneResponse
            {
                Status = AiResponseStatus.Running,
                Message = response.Messages?.FirstOrDefault()?.Text
            };
            var canContinue = TrackCosts(response, finalResponse, context, settings);
            yield return YieldAndTrack(context, finalResponse);

            if (!canContinue)
            {
                yield return YieldAndTrack(context, new AiSceneResponse
                {
                    Status = AiResponseStatus.BudgetExceeded,
                    Message = $"Budget limit of {settings.MaxBudget:F6} {_costCalculator.Currency} exceeded. Total cost: {context.TotalCost:F6}",
                    ErrorMessage = "Maximum budget reached"
                });
            }
        }
    }

    private async IAsyncEnumerable<AiSceneResponse> DynamicChainAsync(
        SceneContext context,
        SceneRequestSettings settings,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return YieldStatus(AiResponseStatus.Running, "Starting dynamic scene chaining");

        var sceneExecutionCount = 0;

        while (sceneExecutionCount < settings.MaxDynamicScenes)
        {
            // Get available scenes (exclude already executed ones)
            var availableScenes = _sceneFactory.GetSceneNames()
                .Where(name => !context.ExecutedScenes.ContainsKey(name))
                .Select(name => _sceneFactory.Create(name))
                .ToList();

            if (availableScenes.Count == 0)
            {
                yield return YieldStatus(AiResponseStatus.Running, "No more scenes available");
                break;
            }

            // Select next scene to execute
            yield return YieldStatus(AiResponseStatus.Running, $"Selecting scene {sceneExecutionCount + 1}/{settings.MaxDynamicScenes}");

            var selectedScene = await SelectSceneForChainingAsync(context, availableScenes, settings, cancellationToken);
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
            var sceneResultBuilder = new System.Text.StringBuilder();
            await foreach (var response in ExecuteSceneAsync(context, selectedScene, settings, cancellationToken))
            {
                // Accumulate scene results
                if (response.Status == AiResponseStatus.Running && !string.IsNullOrWhiteSpace(response.Message))
                {
                    sceneResultBuilder.AppendLine(response.Message);
                }

                yield return response;

                // Check for budget exceeded
                if (response.Status == AiResponseStatus.BudgetExceeded)
                {
                    yield break;
                }
            }

            // Store scene result
            var sceneResult = sceneResultBuilder.ToString();
            context.SceneResults[selectedScene.Name] = sceneResult;
            context.ExecutedSceneOrder.Add(selectedScene.Name);

            sceneExecutionCount++;

            // Ask LLM if it needs to continue to another scene
            if (sceneExecutionCount < settings.MaxDynamicScenes && availableScenes.Count > 1)
            {
                yield return YieldStatus(AiResponseStatus.Running, "Evaluating if more scenes are needed");

                var shouldContinue = await AskContinueToNextSceneAsync(context, settings, cancellationToken);
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
        await foreach (var response in GenerateFinalResponseAsync(context, settings, cancellationToken))
        {
            yield return response;
        }
    }

    private async Task<IScene?> SelectSceneForChainingAsync(
        SceneContext context,
        List<IScene> availableScenes,
        SceneRequestSettings settings,
        CancellationToken cancellationToken)
    {
        // Build context message with execution history
        var contextMessage = BuildChainingContext(context);

        // Create scene selection tools from available scenes
        var sceneTools = availableScenes
            .Select(scene => CreateSceneSelectionTool(scene))
            .ToList();

        var chatOptions = new ChatOptions
        {
            Tools = sceneTools.Cast<AITool>().ToList()
        };

        // Build prompt
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, context.InputMessage)
        };

        if (!string.IsNullOrWhiteSpace(contextMessage))
        {
            messages.Add(new ChatMessage(ChatRole.System, contextMessage));
        }

        // Call LLM for scene selection
        var response = await context.ChatClient.GetResponseAsync(
            messages,
            chatOptions,
            cancellationToken);

        // Track costs
        var selectionResponse = new AiSceneResponse
        {
            Status = AiResponseStatus.Running,
            Message = "Scene selection for chaining"
        };
        TrackCosts(response, selectionResponse, context, settings);

        // Extract function call
        var responseMessage = response.Messages?.FirstOrDefault();
        var functionCall = responseMessage?.Contents?.OfType<FunctionCallContent>().FirstOrDefault();

        if (functionCall != null)
        {
            var selectedSceneName = functionCall.Name;
            return FindSceneByFuzzyMatch(selectedSceneName);
        }

        return null;
    }

    private async Task<bool> AskContinueToNextSceneAsync(
        SceneContext context,
        SceneRequestSettings settings,
        CancellationToken cancellationToken)
    {
        // Build summary of what has been done
        var executionSummary = BuildExecutionSummary(context);

        // Get remaining available scenes
        var remainingScenes = _sceneFactory.GetSceneNames()
            .Where(name => !context.ExecutedScenes.ContainsKey(name))
            .ToList();

        if (remainingScenes.Count == 0)
        {
            return false; // No more scenes available
        }

        var prompt = $@"Based on the user's original request: ""{context.InputMessage}""

Execution so far:
{executionSummary}

Available scenes for further execution:
{string.Join("\n", remainingScenes.Select(s => $"- {s}"))}

Do you need to execute another scene to complete the user's request?
Respond with 'YES' if you need more information from another scene, or 'NO' if you have enough information to provide a complete answer.";

        var response = await context.ChatClient.GetResponseAsync(
            new[] { new ChatMessage(ChatRole.User, prompt) },
            cancellationToken: cancellationToken);

        // Track costs
        var continueResponse = new AiSceneResponse
        {
            Status = AiResponseStatus.Running,
            Message = "Continuation decision"
        };
        TrackCosts(response, continueResponse, context, settings);

        var responseText = response.Messages?.FirstOrDefault()?.Text?.Trim().ToUpperInvariant() ?? "";
        return responseText.Contains("YES");
    }

    private string BuildChainingContext(SceneContext context)
    {
        if (context.ExecutedSceneOrder.Count == 0)
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder();
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

    private string BuildExecutionSummary(SceneContext context)
    {
        var builder = new System.Text.StringBuilder();

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

    private IScene? FindSceneByFuzzyMatch(string requestedName)
    {
        // Normalize scene names for matching
        var normalized = NormalizeSceneName(requestedName);

        foreach (var sceneName in _sceneFactory.GetSceneNames())
        {
            if (NormalizeSceneName(sceneName) == normalized)
            {
                return _sceneFactory.Create(sceneName);
            }
        }

        return null;
    }

    private static string NormalizeSceneName(string name)
    {
        return name.Replace("-", "")
                  .Replace("_", "")
                  .Replace(" ", "")
                  .ToLowerInvariant()
                  .Trim();
    }

    private static AIFunction CreateSceneSelectionTool(IScene scene)
    {
        return AIFunctionFactory.Create(
            (string input) => scene.Name,
            scene.Name,
            scene.Description);
    }

    private AiSceneResponse YieldStatus(AiResponseStatus status, string? message = null, decimal? cost = null)
    {
        return new AiSceneResponse
        {
            Status = status,
            Message = message,
            Cost = cost,
            TotalCost = cost ?? 0
        };
    }

    private AiSceneResponse YieldAndTrack(SceneContext context, AiSceneResponse response)
    {
        response.TotalCost = context.TotalCost;
        context.Responses.Add(response);
        return response;
    }

    /// <summary>
    /// Extracts token usage from ChatResponse and calculates costs.
    /// </summary>
    /// <returns>True if execution should continue; False if budget exceeded.</returns>
    private bool TrackCosts(ChatResponse chatResponse, AiSceneResponse sceneResponse, SceneContext context, SceneRequestSettings? settings = null)
    {
        if (!_costCalculator.IsEnabled || chatResponse.Usage == null)
            return true; // Continue execution

        // Extract token usage from ChatResponse
        var usage = new TokenUsage
        {
            InputTokens = (int)(chatResponse.Usage.InputTokenCount ?? 0),
            OutputTokens = (int)(chatResponse.Usage.OutputTokenCount ?? 0),
            CachedInputTokens = 0, // Azure OpenAI may provide this in future
            ModelId = chatResponse.ModelId
        };

        // Calculate costs
        var costCalculation = _costCalculator.Calculate(usage);

        // Update scene response with token and cost info
        sceneResponse.InputTokens = usage.InputTokens;
        sceneResponse.OutputTokens = usage.OutputTokens;
        sceneResponse.CachedInputTokens = usage.CachedInputTokens;
        sceneResponse.Cost = costCalculation.TotalCost;

        // Accumulate total cost
        context.TotalCost += costCalculation.TotalCost;
        sceneResponse.TotalCost = context.TotalCost;

        // Check budget limit
        if (settings?.MaxBudget.HasValue == true && context.TotalCost > settings.MaxBudget.Value)
        {
            return false; // Budget exceeded - stop execution
        }

        return true; // Continue execution
    }

    /// <summary>
    /// Streams a text response that's already been received (non-streaming fallback).
    /// </summary>
    private async IAsyncEnumerable<AiSceneResponse> StreamTextResponseAsync(
        ChatMessage responseMessage,
        ChatResponse chatResponse,
        string? sceneName,
        SceneContext context,
        SceneRequestSettings settings,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var text = responseMessage.Text ?? string.Empty;
        var accumulatedText = new System.Text.StringBuilder();

        // Simulate streaming by splitting into words
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < words.Length; i++)
        {
            var word = i == 0 ? words[i] : $" {words[i]}";
            accumulatedText.Append(word);

            var isLastChunk = i == words.Length - 1;

            var streamResponse = new AiSceneResponse
            {
                Status = isLastChunk ? AiResponseStatus.Running : AiResponseStatus.Streaming,
                SceneName = sceneName,
                StreamingChunk = word,
                Message = accumulatedText.ToString(),
                IsStreamingComplete = isLastChunk
            };

            // Track costs only on the last chunk
            if (isLastChunk)
            {
                var canContinue = TrackCosts(chatResponse, streamResponse, context, settings);
                yield return YieldAndTrack(context, streamResponse);

                if (!canContinue)
                {
                    yield return YieldAndTrack(context, new AiSceneResponse
                    {
                        Status = AiResponseStatus.BudgetExceeded,
                        SceneName = sceneName,
                        Message = $"Budget limit of {settings.MaxBudget:F6} {_costCalculator.Currency} exceeded. Total cost: {context.TotalCost:F6}",
                        ErrorMessage = "Maximum budget reached"
                    });
                }
            }
            else
            {
                streamResponse.TotalCost = context.TotalCost;
                yield return streamResponse;
            }

            // Small delay to simulate streaming
            await Task.Delay(5, cancellationToken);
        }
    }

    /// <summary>
    /// Processes streaming chunks from IChatClient.GetStreamingResponseAsync.
    /// </summary>
    private async IAsyncEnumerable<AiSceneResponse> ProcessStreamingChunkAsync(
        ChatResponseUpdate streamChunk,
        string? sceneName,
        SceneContext context,
        SceneRequestSettings settings,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Accumulate complete message (stored in context for tracking)
        var contextKey = $"streaming_message_{sceneName ?? "final"}";
        if (!context.Properties.TryGetValue(contextKey, out var accumulatedObj))
        {
            accumulatedObj = new System.Text.StringBuilder();
            context.Properties[contextKey] = accumulatedObj;
        }
        var accumulated = (System.Text.StringBuilder)accumulatedObj;

        // Get the text from this chunk
        var chunkText = streamChunk.Text ?? string.Empty;
        accumulated.Append(chunkText);

        // Check if this is the final chunk (has completion reason)
        var isComplete = streamChunk.FinishReason != null;

        var streamResponse = new AiSceneResponse
        {
            Status = isComplete ? AiResponseStatus.Running : AiResponseStatus.Streaming,
            SceneName = sceneName,
            StreamingChunk = chunkText,
            Message = accumulated.ToString(),
            IsStreamingComplete = isComplete
        };

        // On the last chunk, try to estimate costs (usage may not be available in streaming)
        if (isComplete)
        {
            streamResponse.TotalCost = context.TotalCost;
            yield return YieldAndTrack(context, streamResponse);

            // Clean up accumulated text
            context.Properties.Remove(contextKey);
        }
        else
        {
            streamResponse.TotalCost = context.TotalCost;
            yield return streamResponse;
        }

        await Task.CompletedTask; // Satisfy async requirement
    }

    /// <summary>
    /// Creates an AIFunction wrapper for an MCP tool.
    /// </summary>
    private AIFunction CreateMcpToolFunction(McpTool mcpTool, IMcpServerManager mcpManager)
    {
        return AIFunctionFactory.Create(
            async (string argsJson) =>
            {
                _logger.LogDebug("Executing MCP tool: {ToolName} from server {ServerUrl} (Factory: {FactoryName})",
                    mcpTool.Name, mcpTool.ServerUrl, _factoryName);

                try
                {
                    var result = await mcpManager.ExecuteToolAsync(mcpTool.Name, argsJson, CancellationToken.None);

                    _logger.LogInformation("MCP tool {ToolName} executed successfully (Factory: {FactoryName})",
                        mcpTool.Name, _factoryName);

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to execute MCP tool {ToolName} from server {ServerUrl} (Factory: {FactoryName})",
                        mcpTool.Name, mcpTool.ServerUrl, _factoryName);

                    return $"Error executing MCP tool: {ex.Message}";
                }
            },
            mcpTool.Name,
            mcpTool.Description);
    }
}
