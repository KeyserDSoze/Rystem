using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rystem.PlayFramework.Mcp;
using Rystem.PlayFramework.Services;
using Rystem.PlayFramework.Services.Helpers;
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
    private readonly IResponseHelper _responseHelper;
    private readonly IStreamingHelper _streamingHelper;
    private readonly ISceneMatchingHelper _sceneMatchingHelper;
    private readonly IClientInteractionHandler _clientInteractionHandler;
    private readonly IDistributedCache? _distributedCache;

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
        IResponseHelper responseHelper,
        IStreamingHelper streamingHelper,
        ISceneMatchingHelper sceneMatchingHelper,
        IClientInteractionHandler clientInteractionHandler,
        IFactory<IRateLimiter>? rateLimiterFactory = null,
        IFactory<IMemory>? memoryFactory = null,
        IFactory<IMemoryStorage>? memoryStorageFactory = null,
        IDistributedCache? distributedCache = null)
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
        _responseHelper = responseHelper;
        _streamingHelper = streamingHelper;
        _sceneMatchingHelper = sceneMatchingHelper;
        _clientInteractionHandler = clientInteractionHandler;
        _distributedCache = distributedCache;
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
        MultiModalInput input,
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

        if (_settings.Telemetry.IncludeLlmPrompts && input.Text != null)
        {
            var truncatedMessage = input.Text.Length > _settings.Telemetry.MaxAttributeLength
                ? input.Text[.._settings.Telemetry.MaxAttributeLength] + "..."
                : input.Text;
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
        await foreach (var response in ExecuteInternalAsync(input, metadata, settings, cancellationToken))
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

    public IAsyncEnumerable<AiSceneResponse> ExecuteAsync(
        string message,
        Dictionary<string, object>? metadata = null,
        SceneRequestSettings? settings = null,
        CancellationToken cancellationToken = default)
    {
        // Allow empty message when resuming from continuation token
        if (string.IsNullOrEmpty(settings?.ContinuationToken))
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(message);
        }
        return ExecuteAsync(MultiModalInput.FromText(message ?? ""), metadata, settings, cancellationToken);
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
        MultiModalInput input,
        Dictionary<string, object>? metadata,
        SceneRequestSettings? settings = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        settings ??= new SceneRequestSettings();

        // Initialize
        yield return YieldStatus(AiResponseStatus.Initializing, "Initializing context");

        var context = await InitializeContextAsync(input, metadata, settings, cancellationToken);

        // Restore from continuation token if resuming
        if (!string.IsNullOrEmpty(settings.ContinuationToken) && _distributedCache != null)
        {
            yield return YieldStatus(AiResponseStatus.LoadingCache, "Restoring continuation state");

            var continuationKey = $"continuation:{_factoryName}:{settings.ContinuationToken}";
            var continuationBytes = await _distributedCache.GetAsync(continuationKey, cancellationToken);

            if (continuationBytes == null)
            {
                _logger.LogError("Continuation token '{Token}' not found or expired", settings.ContinuationToken);

                yield return YieldAndTrack(context, new AiSceneResponse
                {
                    Status = AiResponseStatus.Error,
                    ErrorMessage = "Continuation token not found or expired. Please start a new conversation.",
                    Message = "Session expired. Please start over."
                });

                yield break;
            }

            var continuationJson = System.Text.Encoding.UTF8.GetString(continuationBytes);
            var continuation = _jsonService.Deserialize<SceneContinuation>(continuationJson);

            if (continuation == null)
            {
                _logger.LogError("Failed to deserialize continuation token '{Token}'", settings.ContinuationToken);
                yield return YieldAndTrack(context, new AiSceneResponse
                {
                    Status = AiResponseStatus.Error,
                    ErrorMessage = "Invalid continuation state.",
                    Message = "Session recovery failed. Please start over."
                });
                yield break;
            }

            _logger.LogInformation("Restoring continuation for interaction '{InteractionId}' from token '{Token}'",
                continuation.PendingInteractionId, settings.ContinuationToken);

            // Validate client interaction results
            if (settings.ClientInteractionResults != null)
            {
                foreach (var result in settings.ClientInteractionResults)
                {
                    if (!_clientInteractionHandler.ValidateResult(result))
                    {
                        yield return YieldAndTrack(context, new AiSceneResponse
                        {
                            Status = AiResponseStatus.Error,
                            ErrorMessage = $"Invalid client interaction result for '{result.InteractionId}': {result.Error}",
                            Message = "Client interaction failed. Please try again."
                        });

                        yield break;
                    }

                    _logger.LogInformation("Received client interaction result for '{InteractionId}' with {Count} contents",
                        result.InteractionId, result.Contents?.Count ?? 0);
                }
            }

            // Delete continuation token from cache (single use)
            await _distributedCache.RemoveAsync(continuationKey, cancellationToken);

            // Route directly to the scene from continuation, bypassing scene selection
            settings.ExecutionMode = SceneExecutionMode.Scene;
            settings.SceneName = continuation.SceneName;

            // Store original call info for FunctionResultContent reconstruction
            context.Properties["_continuation_callId"] = continuation.OriginalCallId;
            context.Properties["_continuation_toolName"] = continuation.OriginalToolName;
        }

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

            case SceneExecutionMode.Scene:
                // Direct scene execution by name (bypasses scene selection)
                if (string.IsNullOrEmpty(settings.SceneName))
                {
                    yield return YieldAndTrack(context, new AiSceneResponse
                    {
                        Status = AiResponseStatus.Error,
                        ErrorMessage = "Scene mode requested but SceneName is not set in SceneRequestSettings"
                    });
                    yield break;
                }

                var targetScene = _sceneFactory.Create(settings.SceneName);
                if (targetScene == null)
                {
                    yield return YieldAndTrack(context, new AiSceneResponse
                    {
                        Status = AiResponseStatus.Error,
                        ErrorMessage = $"Scene '{settings.SceneName}' not found"
                    });
                    yield break;
                }

                await foreach (var response in ExecuteSceneAsync(context, targetScene, settings, cancellationToken))
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

            // Add user's initial message (multi-modal)
            conversationMessages.Insert(0, context.Input.ToChatMessage(ChatRole.User));

            // Get previous memory from context
            var prevMemoryFromContext = context.Properties.TryGetValue("previous_memory", out var prevMem)
                ? prevMem as ConversationMemory
                : null;

            try
            {
                // Summarize and save
                var updatedMemory = await _memory.SummarizeAsync(
                    prevMemoryFromContext,
                    input.Text ?? string.Empty,
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
        MultiModalInput input,
        Dictionary<string, object>? metadata,
        SceneRequestSettings settings,
        CancellationToken cancellationToken)
    {
        var context = new SceneContext
        {
            ServiceProvider = _serviceProvider,
            Input = input,
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
            var scene = _sceneMatchingHelper.FindSceneByFuzzyMatch(step.SceneName, _sceneFactory);
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

        // Process function calls (scene selections) and extract multi-modal contents
        var responseMessage = responseWithCost.Response.Messages?.FirstOrDefault();
        
        // Extract multi-modal contents (DataContent, UriContent) from LLM response
        var multiModalContents = responseMessage?.Contents?
            .Where(c => c is DataContent or UriContent)
            .ToList();
        
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
                        SceneName = selectedSceneName,
                        Contents = multiModalContents
                    });

                    // Execute the selected scene
                    var scene = _sceneMatchingHelper.FindSceneByFuzzyMatch(selectedSceneName, _sceneFactory);
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

        // Fallback: if no function call, return text response with multi-modal contents
        var fallbackContents = responseMessage?.Contents?
            .Where(c => c is DataContent or UriContent)
            .ToList();
        
        yield return YieldAndTrack(context, new AiSceneResponse
        {
            Status = AiResponseStatus.Running,
            Message = responseMessage?.Text ?? "No response from LLM",
            Contents = fallbackContents
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
            context.Input.ToChatMessage(ChatRole.User)
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
                conversationMessages.Add(new ChatMessage(ChatRole.Tool, [functionResult]));

                _logger.LogInformation("Injected client interaction result for '{InteractionId}' into conversation (Factory: {FactoryName})",
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

            // Use streaming when enabled, fallback to non-streaming otherwise
            if (settings.EnableStreaming)
            {
                // Use StreamingHelper for optimistic streaming
                StreamingResult? lastResult = null;
                await foreach (var result in _streamingHelper.ProcessOptimisticStreamAsync(
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
                        yield return _responseHelper.CreateStreamingResponse(
                            sceneName: scene.Name,
                            streamingChunk: result.AccumulatedText,
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
                yield return _responseHelper.CreateErrorResponse(
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
                    yield return _responseHelper.CreateBudgetExceededResponse(
                        sceneName: scene.Name,
                        maxBudget: settings.MaxBudget.Value,
                        totalCost: context.TotalCost,
                        currency: _chatClientManager.Currency);
                }

                yield break;
            }

            // Add assistant message to conversation history
            conversationMessages.Add(finalMessage);

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
                    yield return _responseHelper.CreateFinalResponse(
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

                    _logger.LogInformation("Native streaming completed for scene {SceneName} (Factory: {FactoryName})",
                        scene.Name, _factoryName);
                }
                else
                {
                    // Non-streaming mode or no streaming happened
                    yield return _responseHelper.CreateFinalResponse(
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
                    yield return _responseHelper.CreateBudgetExceededResponse(
                        sceneName: scene.Name,
                        maxBudget: settings.MaxBudget.Value,
                        totalCost: context.TotalCost,
                        currency: _chatClientManager.Currency);
                }

                yield break; // No function calls, we're done!
            }

            // Function calls detected - track costs and prepare for tool execution
            if (totalCost.HasValue)
            {
                yield return _responseHelper.CreateAndTrackResponse(
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
                yield return _responseHelper.CreateBudgetExceededResponse(
                    sceneName: scene.Name,
                    maxBudget: settings.MaxBudget.Value,
                    totalCost: context.TotalCost,
                    currency: _chatClientManager.Currency);
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

                if (clientRequest != null && _distributedCache == null)
                {
                    // Client tool detected but no distributed cache — this is a configuration error
                    _logger.LogError("Client tool '{ToolName}' detected in scene '{SceneName}' but IDistributedCache is not registered. " +
                        "Add services.AddDistributedMemoryCache() or a Redis cache. The tool call will fail. (Factory: {FactoryName})",
                        functionCall.Name, scene.Name, _factoryName);

                    var errorResult = new FunctionResultContent(functionCall.CallId, functionCall.Name)
                    {
                        Result = $"Client tool '{functionCall.Name}' cannot execute: IDistributedCache is not configured on the server"
                    };
                    conversationMessages.Add(new ChatMessage(ChatRole.Tool, [errorResult]));
                    continue;
                }

                if (clientRequest != null && _distributedCache != null)
                {
                    // Generate continuation token for resuming later
                    var continuationToken = Guid.NewGuid().ToString();

                    // Save minimal state to cache with continuation token
                    var continuation = new SceneContinuation
                    {
                        ConversationKey = context.ConversationKey!,
                        ContinuationToken = continuationToken,
                        SceneName = scene.Name,
                        PendingInteractionId = clientRequest.InteractionId,
                        OriginalCallId = functionCall.CallId,
                        OriginalToolName = functionCall.Name
                    };

                    var continuationKey = $"continuation:{_factoryName}:{continuationToken}";
                    var continuationJson = _jsonService.Serialize(continuation);
                    var continuationBytes = System.Text.Encoding.UTF8.GetBytes(continuationJson);
                    var cacheOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = scene.CacheExpiration };
                    await _distributedCache.SetAsync(continuationKey, continuationBytes, cacheOptions, cancellationToken);

                    _logger.LogInformation("Client tool '{ToolName}' detected. Awaiting client execution with token '{Token}'",
                        functionCall.Name, continuationToken);

                    // Yield AwaitingClient status with request
                    yield return new AiSceneResponse
                    {
                        Status = AiResponseStatus.AwaitingClient,
                        ConversationKey = context.ConversationKey,
                        ContinuationToken = continuationToken,
                        ClientInteractionRequest = clientRequest,
                        Message = $"Awaiting client execution of tool: {functionCall.Name}"
                    };

                    // Stop execution - client will resume with new POST
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
                    conversationMessages.Add(new ChatMessage(ChatRole.Tool, [errorResult]));
                    continue;
                }

                // Track tool execution
                var toolKey = $"{scene.Name}.{functionCall.Name}";
                context.ExecutedTools.Add(toolKey);

                // Execute tool
                yield return _responseHelper.CreateStatusResponse(
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
                    
                    conversationMessages.Add(new ChatMessage(ChatRole.Tool, [functionResult]));

                    toolResponse = _responseHelper.CreateAndTrackResponse(
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
                    conversationMessages.Add(new ChatMessage(ChatRole.Tool, [errorResult]));

                    toolResponse = _responseHelper.CreateErrorResponse(
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
            // Streaming mode - use StreamingHelper
            await foreach (var streamUpdateWithCost in context.ChatClientManager.GetStreamingResponseAsync(
                new[] { finalPrompt },
                cancellationToken: cancellationToken))
            {
                await foreach (var streamResponse in _streamingHelper.ProcessChunkAsync(
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

            // Extract multi-modal contents from LLM response
            var finalResponseMessage = responseWithCost.Response.Messages?.FirstOrDefault();
            var finalContents = finalResponseMessage?.Contents?
                .Where(c => c is DataContent or UriContent)
                .ToList();

            yield return _responseHelper.CreateFinalResponse(
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
                yield return _responseHelper.CreateBudgetExceededResponse(
                    sceneName: null,
                    maxBudget: settings.MaxBudget.Value,
                    totalCost: context.TotalCost,
                    currency: _chatClientManager.Currency);
            }
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
            return _sceneMatchingHelper.FindSceneByFuzzyMatch(selectedSceneName, _sceneFactory);
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
