using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rystem.PlayFramework.Mcp;
using Rystem.PlayFramework.Telemetry;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Rystem.PlayFramework;

/// <summary>
/// Main orchestrator for PlayFramework execution.
/// </summary>
internal sealed class SceneManager : ISceneManager, IFactoryName
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SceneManager> _logger;
    private readonly IFactory<ISceneFactory> _sceneFactoryFactory;
    private readonly IFactory<IChatClientManager> _chatClientManagerFactory;
    private readonly IFactory<PlayFrameworkSettings> _settingsFactory;
    private readonly IFactory<List<ActorConfiguration>> _mainActorsFactory;
    private readonly IFactory<IPlanner> _plannerFactory;
    private readonly IFactory<ISummarizer> _summarizerFactory;
    private readonly IFactory<IDirector> _directorFactory;
    private readonly IFactory<ICacheService> _cacheServiceFactory;
    private readonly IFactory<IJsonService> _jsonServiceFactory;
    private readonly IFactory<IMcpServerManager> _mcpServerManagerFactory;
    private readonly IFactory<IRateLimiter>? _rateLimiterFactory;
    private readonly IFactory<IMemory>? _memoryFactory;
    private readonly IFactory<IMemoryStorage>? _memoryStorageFactory;

    // Resolved dependencies (set via SetFactoryName)
    private string? _factoryName;
    private ISceneFactory _sceneFactory = null!;
    private IChatClientManager _chatClientManager = null!;
    private PlayFrameworkSettings _settings = null!;
    private List<ActorConfiguration> _mainActors = null!;
    private IPlanner? _planner;
    private ISummarizer? _summarizer;
    private IDirector? _director;
    private ICacheService? _cacheService;
    private IJsonService _jsonService = null!;
    private IRateLimiter? _rateLimiter;
    private IMemory? _memory;
    private IMemoryStorage? _memoryStorage;

    public SceneManager(
        IServiceProvider serviceProvider,
        ILogger<SceneManager> logger,
        IFactory<ISceneFactory> sceneFactoryFactory,
        IFactory<IChatClientManager> chatClientManagerFactory,
        IFactory<PlayFrameworkSettings> settingsFactory,
        IFactory<List<ActorConfiguration>> mainActorsFactory,
        IFactory<IPlanner> plannerFactory,
        IFactory<ISummarizer> summarizerFactory,
        IFactory<IDirector> directorFactory,
        IFactory<ICacheService> cacheServiceFactory,
        IFactory<IJsonService> jsonServiceFactory,
        IFactory<IMcpServerManager> mcpServerManagerFactory,
        IFactory<IRateLimiter>? rateLimiterFactory = null,
        IFactory<IMemory>? memoryFactory = null,
        IFactory<IMemoryStorage>? memoryStorageFactory = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _sceneFactoryFactory = sceneFactoryFactory;
        _chatClientManagerFactory = chatClientManagerFactory;
        _settingsFactory = settingsFactory;
        _mainActorsFactory = mainActorsFactory;
        _plannerFactory = plannerFactory;
        _summarizerFactory = summarizerFactory;
        _directorFactory = directorFactory;
        _cacheServiceFactory = cacheServiceFactory;
        _jsonServiceFactory = jsonServiceFactory;
        _mcpServerManagerFactory = mcpServerManagerFactory;
        _rateLimiterFactory = rateLimiterFactory;
        _memoryFactory = memoryFactory;
        _memoryStorageFactory = memoryStorageFactory;
    }

    public void SetFactoryName(AnyOf<string?, Enum>? name)
    {
        _factoryName = name?.ToString() ?? "default";

        _logger.LogDebug("Initializing SceneManager for factory: {FactoryName}", _factoryName);

        _sceneFactory = _sceneFactoryFactory.Create(name) ?? throw new InvalidOperationException($"SceneFactory not found for name: {name}");
        _logger.LogTrace("SceneFactory resolved: {SceneFactoryType} (Factory: {FactoryName})", _sceneFactory.GetType().Name, _factoryName);

        // Get ChatClientManager from factory
        _chatClientManager = _chatClientManagerFactory.Create(name) 
            ?? throw new InvalidOperationException($"ChatClientManager not found for factory key: {name}. Make sure IChatClientManager is registered.");

        _logger.LogTrace("ChatClientManager resolved: {ChatClientManagerType} (Factory: {FactoryName})", _chatClientManager.GetType().Name, _factoryName);

        _settings = _settingsFactory.Create(name) ?? new PlayFrameworkSettings();
        _logger.LogDebug("Settings loaded - ExecutionMode: {ExecutionMode}, Planning: {PlanningEnabled}, Cache: {CacheEnabled}, FallbackMode: {FallbackMode} (Factory: {FactoryName})", 
            _settings.DefaultExecutionMode, _settings.Planning.Enabled, _settings.Cache.Enabled, _settings.FallbackMode, _factoryName);

        _mainActors = _mainActorsFactory.Create(name) ?? [];
        _logger.LogDebug("{ActorCount} main actors loaded (Factory: {FactoryName})", _mainActors.Count, _factoryName);

        _planner = _plannerFactory.Create(name);
        _summarizer = _summarizerFactory.Create(name);
        _director = _directorFactory.Create(name);
        _cacheService = _cacheServiceFactory.Create(name);
        _jsonService = _jsonServiceFactory.Create(name) ?? new DefaultJsonService();

        // Resolve rate limiter if enabled
        _rateLimiter = _rateLimiterFactory?.Create(name);
        if (_rateLimiter != null)
        {
            _logger.LogDebug("Rate limiter enabled with strategy: {Strategy} (Factory: {FactoryName})", 
                _settings.RateLimiting?.Strategy, _factoryName);
        }

        // Resolve memory components if enabled
        _memory = _memoryFactory?.Create(name);
        _memoryStorage = _memoryStorageFactory?.Create(name);
        if (_memory != null && _memoryStorage != null)
        {
            _logger.LogDebug("Memory enabled with max summary length: {MaxLength} (Factory: {FactoryName})",
                _settings.Memory?.MaxSummaryLength, _factoryName);
        }

        var availableScenes = _sceneFactory.GetSceneNames().Count();
        _logger.LogInformation("SceneManager initialized successfully - Scenes: {SceneCount}, ChatClients: {ChatClientCount}, FallbackMode: {FallbackMode}, Planner: {HasPlanner}, Summarizer: {HasSummarizer}, Director: {HasDirector}, Cache: {HasCache}, RateLimit: {HasRateLimit}, Memory: {HasMemory} (Factory: {FactoryName})", 
            availableScenes, _settings.ChatClientNames?.Count ?? 1, _settings.FallbackMode, _planner != null, _summarizer != null, _director != null, _cacheService != null, _rateLimiter != null, _memory != null, _factoryName);
    }

    public async IAsyncEnumerable<AiSceneResponse> ExecuteAsync(
        string message,
        Dictionary<string, object>? metadata = null,
        SceneRequestSettings? settings = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Start root activity for telemetry
        using var activity = _settings.Telemetry.EnableTracing
            ? PlayFrameworkActivitySource.Instance.StartActivity(
                PlayFrameworkActivitySource.Activities.SceneManagerExecute,
                ActivityKind.Internal)
            : null;

        activity?.SetTag(PlayFrameworkActivitySource.Tags.FactoryName, _factoryName);
        activity?.SetTag(PlayFrameworkActivitySource.Tags.ServiceVersion, PlayFrameworkActivitySource.Version);

        if (_settings.Telemetry.IncludeLlmPrompts)
        {
            var truncatedMessage = message.Length > _settings.Telemetry.MaxAttributeLength
                ? message[.._settings.Telemetry.MaxAttributeLength] + "..."
                : message;
            activity?.SetTag(PlayFrameworkActivitySource.Tags.UserMessage, truncatedMessage);
        }

        // Add custom attributes
        foreach (var attr in _settings.Telemetry.CustomAttributes)
        {
            activity?.SetTag(attr.Key, attr.Value);
        }

        // Increment active scenes gauge
        if (_settings.Telemetry.EnableMetrics)
        {
            PlayFrameworkMetrics.IncrementActiveScenes();
        }

        var startTime = DateTime.UtcNow;
        var success = false;
        var sceneName = "unknown";
        var totalTokens = 0;
        var totalCost = 0m;
        Exception? exception = null;

        // Execute without try-catch (yield return restriction)
        await foreach (var response in ExecuteInternalAsync(message, metadata, settings, cancellationToken))
        {
            // Track scene name from first real response
            if (response.Status != AiResponseStatus.Initializing && !string.IsNullOrEmpty(response.SceneName))
            {
                sceneName = response.SceneName;
                activity?.SetTag(PlayFrameworkActivitySource.Tags.SceneName, sceneName);
            }

            // Track tokens and cost
            if (response.TotalTokens.HasValue && response.TotalTokens.Value > 0)
            {
                totalTokens = response.TotalTokens.Value;
                activity?.SetTag(PlayFrameworkActivitySource.Tags.TokensTotal, totalTokens);
            }

            if (response.Cost.HasValue && response.Cost.Value > 0)
            {
                totalCost = response.TotalCost;  // Use cumulative cost
                activity?.SetTag(PlayFrameworkActivitySource.Tags.Cost, (double)totalCost);
            }

            // Track errors
            if (response.Status == AiResponseStatus.Error)
            {
                exception = new InvalidOperationException(response.ErrorMessage ?? "Unknown error");
                activity?.SetStatus(ActivityStatusCode.Error, response.ErrorMessage);
                activity?.SetTag("exception.message", response.ErrorMessage);
                activity?.AddEvent(new ActivityEvent(PlayFrameworkActivitySource.Events.SceneFailed));
            }

            // Mark success if completed
            if (response.Status == AiResponseStatus.Completed)
            {
                success = true;
                activity?.SetStatus(ActivityStatusCode.Ok);
                activity?.AddEvent(new ActivityEvent(PlayFrameworkActivitySource.Events.SceneCompleted));
            }

            yield return response;
        }

        // Finalization (always executes)
        try
        {
            // If no explicit status was set, mark as OK
            if (activity != null && activity.Status == ActivityStatusCode.Unset && exception == null)
            {
                activity.SetStatus(ActivityStatusCode.Ok);
            }
        }
        finally
        {
            // Decrement active scenes gauge
            if (_settings.Telemetry.EnableMetrics)
            {
                PlayFrameworkMetrics.DecrementActiveScenes();

                // Record final metrics
                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                PlayFrameworkMetrics.RecordSceneExecution(
                    sceneName: sceneName,
                    executionMode: settings?.ExecutionMode?.ToString() ?? _settings.DefaultExecutionMode.ToString(),
                    success: success,
                    durationMs: duration,
                    tokenCount: totalTokens,
                    cost: (double)totalCost);
            }
        }
    }

    /// <summary>
    /// Builds rate limit key from metadata based on GroupByKeys configuration.
    /// Returns "global" if no grouping keys specified, uses "unknown" for missing metadata values.
    /// </summary>
    private string BuildRateLimitKey(Dictionary<string, object>? metadata)
    {
        var groupByKeys = _settings.RateLimiting?.GroupByKeys;

        if (groupByKeys == null || groupByKeys.Length == 0)
        {
            _logger.LogWarning("Rate limiting enabled but no GroupBy keys specified. Using 'global' key for rate limiting. Configure with .GroupBy(\"userId\") or similar to enable per-user/tenant rate limits.");
            return "global";
        }

        var keyParts = new List<string>();

        foreach (var key in groupByKeys)
        {
            if (metadata?.TryGetValue(key, out var value) == true && value != null)
            {
                keyParts.Add($"{key}:{value}");
            }
            else
            {
                _logger.LogWarning("Metadata key '{Key}' not found in request metadata. Using 'unknown' for rate limiting. Pass metadata dictionary with required keys in ExecuteAsync(message, metadata, settings).", key);
                keyParts.Add($"{key}:unknown");
            }
        }

        var rateLimitKey = string.Join("|", keyParts);
        _logger.LogDebug("Built rate limit key: {RateLimitKey} from GroupBy keys: {GroupByKeys}", rateLimitKey, string.Join(", ", groupByKeys));
        return rateLimitKey;
    }

    /// <summary>
    /// Internal implementation of ExecuteAsync (original logic without telemetry wrapper).
    /// </summary>
    private async IAsyncEnumerable<AiSceneResponse> ExecuteInternalAsync(
        string message,
        Dictionary<string, object>? metadata,
        SceneRequestSettings? settings = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        settings ??= new SceneRequestSettings();

        // Initialize
        yield return YieldStatus(AiResponseStatus.Initializing, "Initializing context");

        var context = await InitializeContextAsync(message, metadata, settings, cancellationToken);

        // Rate limiting check
        if (_rateLimiter != null && _settings.RateLimiting?.Enabled == true)
        {
            yield return YieldStatus(AiResponseStatus.Initializing, "Checking rate limit");

            var rateLimitKey = BuildRateLimitKey(metadata);
            RateLimitCheckResult? checkResult = null;
            RateLimitExceededException? rateLimitException = null;

            try
            {
                checkResult = await _rateLimiter.CheckAndWaitAsync(
                    rateLimitKey,
                    cost: 1,
                    cancellationToken);
            }
            catch (RateLimitExceededException ex)
            {
                rateLimitException = ex;
            }

            if (rateLimitException != null)
            {
                _logger.LogError("Rate limit exceeded for key '{Key}': {Error}. RetryAfter: {RetryAfter}",
                    rateLimitKey, rateLimitException.Message, rateLimitException.RetryAfter);

                yield return YieldAndTrack(context, new AiSceneResponse
                {
                    Status = AiResponseStatus.Error,
                    ErrorMessage = $"Rate limit exceeded: {rateLimitException.Message}. Retry after {rateLimitException.RetryAfter?.TotalSeconds:F0} seconds.",
                    Message = $"Rate limit exceeded. Please try again in {rateLimitException.RetryAfter?.TotalSeconds:F0} seconds."
                });

                yield break; // Stop execution
            }

            _logger.LogDebug("Rate limit check passed for key '{Key}'. Remaining: {Remaining}, Reset: {ResetTime}",
                rateLimitKey, checkResult!.RemainingTokens, checkResult.ResetTime);
        }

        // Load previous memory if enabled
        ConversationMemory? previousMemory = null;
        if (_memory != null && _memoryStorage != null && _settings.Memory?.Enabled == true && context.ConversationKey != null)
        {
            yield return YieldStatus(AiResponseStatus.LoadingCache, "Loading conversation memory");

            previousMemory = await _memoryStorage.GetAsync(context.ConversationKey, metadata, settings, cancellationToken);

            if (previousMemory != null)
            {
                _logger.LogInformation(
                    "Previous memory loaded for conversation '{Key}': {ConversationCount} conversations, summary length: {Length} (Factory: {FactoryName})",
                    context.ConversationKey, previousMemory.ConversationCount, previousMemory.Summary?.Length ?? 0, _factoryName);

                // Add memory context to the beginning of conversation
                context.Properties["previous_memory"] = previousMemory;
            }
            else
            {
                _logger.LogDebug("No previous memory found for conversation '{Key}' (Factory: {FactoryName})",
                    context.ConversationKey, _factoryName);
            }
        }

        // Load from cache if available
        if (settings.CacheBehavior != CacheBehavior.Avoidable && _cacheService != null && context.ConversationKey != null)
        {
            yield return YieldStatus(AiResponseStatus.LoadingCache, "Loading from cache");

            var cached = await _cacheService.GetAsync(context.ConversationKey, cancellationToken);
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
        if (settings.CacheBehavior != CacheBehavior.Avoidable && _cacheService != null && context.ConversationKey != null)
        {
            yield return YieldStatus(AiResponseStatus.SavingCache, "Saving to cache");
            await _cacheService.SetAsync(context.ConversationKey, context.Responses, settings.CacheBehavior, cancellationToken);
        }

        // Save updated memory if enabled
        if (_memory != null && _memoryStorage != null && _settings.Memory?.Enabled == true && context.ConversationKey != null)
        {
            yield return YieldStatus(AiResponseStatus.SavingCache, "Saving conversation memory");

            // Collect all conversation messages from responses
            var conversationMessages = context.Responses
                .Where(r => !string.IsNullOrWhiteSpace(r.Message))
                .Select(r => new ChatMessage(ChatRole.Assistant, r.Message ?? string.Empty))
                .ToList();

            // Add user's initial message
            conversationMessages.Insert(0, new ChatMessage(ChatRole.User, message));

            // Get previous memory from context
            var prevMemoryFromContext = context.Properties.TryGetValue("previous_memory", out var prevMem)
                ? prevMem as ConversationMemory
                : null;

            try
            {
                // Summarize and save
                var updatedMemory = await _memory.SummarizeAsync(
                    prevMemoryFromContext,
                    message,
                    conversationMessages,
                    metadata,
                    settings,
                    _chatClientManager,
                    cancellationToken);

                _logger.LogInformation(
                    "Conversation memory updated and saved. Key: '{Key}', ConversationCount: {Count} (Factory: {FactoryName})",
                    context.ConversationKey, updatedMemory.ConversationCount, _factoryName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save conversation memory for key '{Key}' (Factory: {FactoryName})",
                    context.ConversationKey, _factoryName);
                // Don't fail the entire request if memory save fails
            }
        }

        yield return YieldStatus(AiResponseStatus.Completed, "Execution completed", context.TotalCost);
    }

    private Task<SceneContext> InitializeContextAsync(
        string message,
        Dictionary<string, object>? metadata,
        SceneRequestSettings settings,
        CancellationToken cancellationToken)
    {
        var context = new SceneContext
        {
            ServiceProvider = _serviceProvider,
            InputMessage = message,
            Metadata = metadata ?? new Dictionary<string, object>(),
            ChatClientManager = _chatClientManager,
            ConversationKey = settings.ConversationKey ?? Guid.NewGuid().ToString(),
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
        var responseWithCost = await context.ChatClientManager.GetResponseAsync(
            new[] { userMessage },
            chatOptions,
            cancellationToken);

        // Track costs for scene selection
        var selectionResponse = new AiSceneResponse
        {
            Status = AiResponseStatus.Running,
            Message = "Scene selection",
            InputTokens = responseWithCost.InputTokens,
            OutputTokens = responseWithCost.OutputTokens,
            CachedInputTokens = responseWithCost.CachedInputTokens,
            Cost = responseWithCost.CalculatedCost,
            TotalCost = context.AddCost(responseWithCost.CalculatedCost)
        };

        // Check budget limit
        if (settings.MaxBudget.HasValue && context.TotalCost > settings.MaxBudget.Value)
        {
            yield return YieldAndTrack(context, new AiSceneResponse
            {
                Status = AiResponseStatus.BudgetExceeded,
                Message = $"Budget limit of {settings.MaxBudget:F6} {_chatClientManager.Currency} exceeded. Total cost: {context.TotalCost:F6}",
                ErrorMessage = "Maximum budget reached"
            });
            yield break;
        }

        // Process function calls (scene selections)
        var responseMessage = responseWithCost.Response.Messages?.FirstOrDefault();
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

        // Add previous memory context if available
        if (context.Properties.TryGetValue("previous_memory", out var previousMemoryObj) &&
            previousMemoryObj is ConversationMemory previousMemory)
        {
            var memoryContext = $@"Previous conversation context (Conversation #{previousMemory.ConversationCount}):

Summary: {previousMemory.Summary}

Important Facts:
{System.Text.Json.JsonSerializer.Serialize(previousMemory.ImportantFacts, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })}

Last Updated: {previousMemory.LastUpdated:yyyy-MM-dd HH:mm:ss} UTC

Use this context to provide personalized and coherent responses.";

            conversationMessages.Insert(0, new ChatMessage(ChatRole.System, memoryContext));

            _logger.LogDebug("Added previous memory to conversation (ConversationCount: {Count}, SummaryLength: {Length}, Factory: {FactoryName})",
                previousMemory.ConversationCount, previousMemory.Summary?.Length ?? 0, _factoryName);
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
            var responseWithCost = await context.ChatClientManager.GetResponseAsync(
                conversationMessages,
                chatOptions,
                cancellationToken);

            var responseMessage = responseWithCost.Response.Messages?.FirstOrDefault();
            if (responseMessage == null)
            {
                var errorResponse = new AiSceneResponse
                {
                    Status = AiResponseStatus.Error,
                    SceneName = scene.Name,
                    Message = "No response from LLM",
                    InputTokens = responseWithCost.InputTokens,
                    OutputTokens = responseWithCost.OutputTokens,
                    CachedInputTokens = responseWithCost.CachedInputTokens,
                    Cost = responseWithCost.CalculatedCost,
                    TotalCost = context.AddCost(responseWithCost.CalculatedCost)
                };
                yield return YieldAndTrack(context, errorResponse);

                if (settings.MaxBudget.HasValue && context.TotalCost > settings.MaxBudget.Value)
                {
                    yield return YieldAndTrack(context, new AiSceneResponse
                    {
                        Status = AiResponseStatus.BudgetExceeded,
                        SceneName = scene.Name,
                        Message = $"Budget limit of {settings.MaxBudget:F6} {_chatClientManager.Currency} exceeded. Total cost: {context.TotalCost:F6}",
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
                        responseWithCost,
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
                        Message = responseMessage.Text ?? string.Empty,
                        InputTokens = responseWithCost.InputTokens,
                        OutputTokens = responseWithCost.OutputTokens,
                        CachedInputTokens = responseWithCost.CachedInputTokens,
                        Cost = responseWithCost.CalculatedCost,
                        TotalCost = context.AddCost(responseWithCost.CalculatedCost)
                    };
                    yield return YieldAndTrack(context, finalResponse);

                    if (settings.MaxBudget.HasValue && context.TotalCost > settings.MaxBudget.Value)
                    {
                        yield return YieldAndTrack(context, new AiSceneResponse
                        {
                            Status = AiResponseStatus.BudgetExceeded,
                            SceneName = scene.Name,
                            Message = $"Budget limit of {settings.MaxBudget:F6} {_chatClientManager.Currency} exceeded. Total cost: {context.TotalCost:F6}",
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
                Message = $"LLM returned {functionCalls.Count} function call(s)",
                InputTokens = responseWithCost.InputTokens,
                OutputTokens = responseWithCost.OutputTokens,
                CachedInputTokens = responseWithCost.CachedInputTokens,
                Cost = responseWithCost.CalculatedCost,
                TotalCost = context.AddCost(responseWithCost.CalculatedCost)
            };

            // Only yield if there are costs to report
            if (llmResponse.Cost.HasValue)
            {
                yield return YieldAndTrack(context, llmResponse);
            }

            // Check budget before executing tools
            if (settings.MaxBudget.HasValue && context.TotalCost > settings.MaxBudget.Value)
            {
                yield return YieldAndTrack(context, new AiSceneResponse
                {
                    Status = AiResponseStatus.BudgetExceeded,
                    SceneName = scene.Name,
                    Message = $"Budget limit of {settings.MaxBudget:F6} {_chatClientManager.Currency} exceeded. Total cost: {context.TotalCost:F6}",
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
            await foreach (var streamUpdateWithCost in context.ChatClientManager.GetStreamingResponseAsync(
                new[] { finalPrompt },
                cancellationToken: cancellationToken))
            {
                await foreach (var streamResponse in ProcessStreamingChunkAsync(
                    streamUpdateWithCost.Update,
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
            var responseWithCost = await context.ChatClientManager.GetResponseAsync(
                new[] { finalPrompt },
                cancellationToken: cancellationToken);

            var finalResponse = new AiSceneResponse
            {
                Status = AiResponseStatus.Running,
                Message = responseWithCost.Response.Messages?.FirstOrDefault()?.Text,
                InputTokens = responseWithCost.InputTokens,
                OutputTokens = responseWithCost.OutputTokens,
                CachedInputTokens = responseWithCost.CachedInputTokens,
                Cost = responseWithCost.CalculatedCost,
                TotalCost = context.AddCost(responseWithCost.CalculatedCost)
            };
            yield return YieldAndTrack(context, finalResponse);

            if (settings.MaxBudget.HasValue && context.TotalCost > settings.MaxBudget.Value)
            {
                yield return YieldAndTrack(context, new AiSceneResponse
                {
                    Status = AiResponseStatus.BudgetExceeded,
                    Message = $"Budget limit of {settings.MaxBudget:F6} {_chatClientManager.Currency} exceeded. Total cost: {context.TotalCost:F6}",
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
        var responseWithCost = await context.ChatClientManager.GetResponseAsync(
            messages,
            chatOptions,
            cancellationToken);

        // Track costs
        var selectionResponse = new AiSceneResponse
        {
            Status = AiResponseStatus.Running,
            Message = "Scene selection for chaining",
            InputTokens = responseWithCost.InputTokens,
            OutputTokens = responseWithCost.OutputTokens,
            CachedInputTokens = responseWithCost.CachedInputTokens,
            Cost = responseWithCost.CalculatedCost,
            TotalCost = context.AddCost(responseWithCost.CalculatedCost)
        };

        // Extract function call
        var responseMessage = responseWithCost.Response.Messages?.FirstOrDefault();
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

        var responseWithCost = await context.ChatClientManager.GetResponseAsync(
            new[] { new ChatMessage(ChatRole.User, prompt) },
            cancellationToken: cancellationToken);

        // Track costs
        var continueResponse = new AiSceneResponse
        {
            Status = AiResponseStatus.Running,
            Message = "Continuation decision",
            InputTokens = responseWithCost.InputTokens,
            OutputTokens = responseWithCost.OutputTokens,
            CachedInputTokens = responseWithCost.CachedInputTokens,
            Cost = responseWithCost.CalculatedCost,
            TotalCost = context.AddCost(responseWithCost.CalculatedCost)
        };

        var responseText = responseWithCost.Response.Messages?.FirstOrDefault()?.Text?.Trim().ToUpperInvariant() ?? "";
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
    /// Streams a text response that's already been received (non-streaming fallback).
    /// </summary>
    private async IAsyncEnumerable<AiSceneResponse> StreamTextResponseAsync(
        ChatMessage responseMessage,
        ChatResponseWithCost responseWithCost,
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
                streamResponse.InputTokens = responseWithCost.InputTokens;
                streamResponse.OutputTokens = responseWithCost.OutputTokens;
                streamResponse.CachedInputTokens = responseWithCost.CachedInputTokens;
                streamResponse.Cost = responseWithCost.CalculatedCost;
                streamResponse.TotalCost = context.AddCost(responseWithCost.CalculatedCost);
                yield return YieldAndTrack(context, streamResponse);

                if (settings.MaxBudget.HasValue && context.TotalCost > settings.MaxBudget.Value)
                {
                    yield return YieldAndTrack(context, new AiSceneResponse
                    {
                        Status = AiResponseStatus.BudgetExceeded,
                        SceneName = sceneName,
                        Message = $"Budget limit of {settings.MaxBudget:F6} {_chatClientManager.Currency} exceeded. Total cost: {context.TotalCost:F6}",
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

    private void ThrowChatClientNotFoundException(AnyOf<string?, Enum>? name)
    {
        var factoryKeyDisplay = string.IsNullOrEmpty(name?.ToString()) || name?.ToString() == "default"
            ? "default (empty key)"
            : $"'{name}'";

        var errorMessage = $$"""
            No IChatClient registered for factory key {{factoryKeyDisplay}}.

            🔧 How to fix:

            1️⃣ Register IChatClient as singleton (simplest):

               services.AddSingleton<IChatClient>(sp =>
               {
                   return new AzureOpenAIClient(endpoint, apiKey, modelName);
               });

            2️⃣ Register with factory pattern (for multiple models):

               services.AddFactory<IChatClient, AzureOpenAIClient>(name: "gpt4");
               services.AddFactory<IChatClient, OllamaClient>(name: "llama");

            3️⃣ Example with Azure OpenAI:

               using Microsoft.Extensions.AI;
               using Azure.AI.OpenAI;

               services.AddSingleton<IChatClient>(sp =>
               {
                   var client = new AzureOpenAIClient(
                       new Uri("https://your-resource.openai.azure.com"),
                       new AzureKeyCredential("your-api-key"));

                   return client.AsChatClient("gpt-4o");
               });

            4️⃣ Example with Ollama (local):

               services.AddSingleton<IChatClient>(sp =>
               {
                   var client = new HttpClient 
                   { 
                       BaseAddress = new Uri("http://localhost:11434") 
                   };
                   return new OllamaChatClient(client, "llama3.1");
               });

            5️⃣ Example with OpenAI (cloud):

               services.AddSingleton<IChatClient>(sp =>
               {
                   var client = new OpenAI.OpenAIClient("your-openai-api-key");
                   return client.AsChatClient("gpt-4o");
               });

            📖 Documentation: https://learn.microsoft.com/dotnet/ai/get-started
            """;

        _logger.LogError("IChatClient with factory key {FactoryKey} not registered", name);

        throw new InvalidOperationException(errorMessage);
    }
}
