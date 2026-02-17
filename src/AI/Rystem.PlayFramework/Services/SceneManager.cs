using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rystem.PlayFramework.Mcp;
using Rystem.PlayFramework.Services;
using Rystem.PlayFramework.Services.ExecutionModes;
using Rystem.PlayFramework.Services.Helpers;
using Rystem.PlayFramework.Telemetry;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;

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
    private readonly IFactory<IRateLimiter>? _rateLimiterFactory;
    private readonly IFactory<IMemory>? _memoryFactory;
    private readonly IFactory<IContext>? _contextFactory;
    private readonly IFactory<IMemoryStorage>? _memoryStorageFactory;
    private readonly IFactory<IExecutionModeHandler> _executionModeHandlerFactory;
    private readonly IPlayFrameworkCache _playFrameworkCache;
    private readonly IClientInteractionHandler _clientInteractionHandler;

    // Resolved dependencies (set via SetFactoryName)
    private string? _factoryName;
    private ISceneFactory _sceneFactory = null!;
    private IChatClientManager _chatClientManager = null!;
    private PlayFrameworkSettings _settings = null!;
    private List<ActorConfiguration> _mainActors = null!;
    private IPlanner? _planner;
    private ISummarizer? _summarizer;
    private IDirector? _director;
    private IRateLimiter? _rateLimiter;
    private IMemory? _memory;
    private IMemoryStorage? _memoryStorage;
    private IContext? _context;

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
        IFactory<IExecutionModeHandler> executionModeHandlerFactory,
        IPlayFrameworkCache playFrameworkCache,
        IClientInteractionHandler clientInteractionHandler,
        IFactory<IRateLimiter>? rateLimiterFactory = null,
        IFactory<IMemory>? memoryFactory = null,
        IFactory<IMemoryStorage>? memoryStorageFactory = null,
        IFactory<IContext>? contextFactory = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _sceneFactoryFactory = sceneFactoryFactory;
        _chatClientManagerFactory = chatClientManagerFactory;
        _settingsFactory = settingsFactory;
        _mainActorsFactory = mainActorsFactory;
        _plannerFactory = plannerFactory;
        _contextFactory = contextFactory;
        _summarizerFactory = summarizerFactory;
        _directorFactory = directorFactory;
        _executionModeHandlerFactory = executionModeHandlerFactory;
        _playFrameworkCache = playFrameworkCache;
        _clientInteractionHandler = clientInteractionHandler;
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
        _logger.LogInformation("SceneManager initialized successfully - Scenes: {SceneCount}, ChatClients: {ChatClientCount}, FallbackMode: {FallbackMode}, Planner: {HasPlanner}, Summarizer: {HasSummarizer}, Director: {HasDirector}, Cache: {CacheEnabled}, RateLimit: {HasRateLimit}, Memory: {HasMemory} (Factory: {FactoryName})",
            availableScenes, _settings.ChatClientNames?.Count ?? 1, _settings.FallbackMode, _planner != null, _summarizer != null, _director != null, _settings.Cache.Enabled, _rateLimiter != null, _memory != null, _factoryName);

        _context = _contextFactory?.Create(name);
    }

    public async IAsyncEnumerable<AiSceneResponse> ExecuteAsync(
        MultiModalInput input,
        Dictionary<string, object>? metadata = null,
        SceneRequestSettings? settings = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        #region tracking activity
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

        void Tracking(AiSceneResponse response)
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
        }
        #endregion

        settings ??= new SceneRequestSettings();

        // Initialize
        yield return YieldStatus(AiResponseStatus.Initializing, "Initializing context");

        // Create minimal context with just ConversationKey
        var context = new SceneContext
        {
            ServiceProvider = _serviceProvider,
            Input = input,
            Metadata = metadata ?? [],
            ChatClientManager = _chatClientManager,
            ConversationKey = settings.ConversationKey ?? Guid.NewGuid().ToString(),
            CacheBehavior = settings.CacheBehavior
        };

        await foreach (var initizializeResponse in InitializePlayFrameworkAsync(context, settings, cancellationToken))
        {
            Tracking(initizializeResponse);
            yield return initizializeResponse;
        }

        if (context.ExecutionPhase != ExecutionPhase.Break)
        {
            // Execute without try-catch (yield return restriction)
            await foreach (var response in ExecuteInternalAsync(context, settings, cancellationToken))
            {
                Tracking(response);
                yield return response;
            }
        }


        // Finalization (always executes)
        await foreach (var finalizeResponse in FinalizePlayFrameworkAsync(context, settings, cancellationToken))
        {
            Tracking(finalizeResponse);
            yield return finalizeResponse;
        }

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
        // Allow empty message when resuming from client interaction (has ConversationKey + ClientInteractionResults)
        var isResumingFromClientInteraction = !string.IsNullOrEmpty(settings?.ConversationKey)
            && settings?.ClientInteractionResults is { Count: > 0 };

        if (!isResumingFromClientInteraction)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(message);
        }
        return ExecuteAsync(MultiModalInput.FromText(message ?? ""), metadata, settings, cancellationToken);
    }

    /// <summary>
    /// Initializes a new context with IContext service and MainActors.
    /// Only called for brand new conversations (not when resuming from cache).
    /// </summary>
    private async Task InitializeNewContextAsync(
        SceneContext context,
        CancellationToken cancellationToken)
    {
        // Get context result from IContext service (e.g., user info from JWT, system info)
        object? contextResult = null;
        if (_context != null)
        {
            var settings = new SceneRequestSettings { ConversationKey = context.ConversationKey };
            contextResult = await _context.RetrieveAsync(context, settings, cancellationToken);
        }

        // Execute main actors and collect their outputs
        var mainActorOutputs = new List<string>();
        foreach (var actorConfig in _mainActors)
        {
            var actor = ActorFactory.Create(actorConfig, _serviceProvider);
            var response = await actor.PlayAsync(context, cancellationToken);

            if (!string.IsNullOrWhiteSpace(response.Message))
            {
                mainActorOutputs.Add(response.Message);
            }
        }

        // Build the initial context system message (Context + MainActors combined)
        context.BuildInitialContext(contextResult, mainActorOutputs);

        _logger.LogDebug("New context initialized with {MainActorCount} main actors, context result: {HasContext} (Factory: {FactoryName})",
            mainActorOutputs.Count, contextResult != null, _factoryName);
    }

    private async IAsyncEnumerable<AiSceneResponse> InitializePlayFrameworkAsync(SceneContext context,
        SceneRequestSettings settings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Load from cache (includes conversation history, execution state, and continuation data)
        var isResuming = false;
        if (_settings.Cache.Enabled && context.ConversationKey != null)
        {
            yield return YieldStatus(AiResponseStatus.LoadingCache, "Loading from cache");
            await _playFrameworkCache.LoadAsync(context, cancellationToken);

            if (context.IsResuming)
            {
                isResuming = true;
                _logger.LogInformation(
                    "Resuming conversation '{ConversationKey}' from phase {Phase} - Scenes: {SceneCount}, Tools: {ToolCount}",
                    context.ConversationKey,
                    context.RestoredExecutionState!.Phase,
                    context.ExecutedSceneOrder.Count,
                    context.ExecutedTools.Count);
            }
        }

        // Initialize context only if NOT resuming from cache
        if (!isResuming)
        {
            yield return YieldStatus(AiResponseStatus.Initializing, "Building initial context");
            await InitializeNewContextAsync(context, cancellationToken);
        }

        // Always add the new user message (whether new or resuming)
        context.AddUserMessage(context.Input);

        // Check if resuming from client interaction (ConversationKey + ClientInteractionResults)
        var isResumingFromClientInteraction = settings.ClientInteractionResults is { Count: > 0 }
            && context.Properties.ContainsKey("_continuation_sceneName");

        if (isResumingFromClientInteraction)
        {
            _logger.LogInformation("Resuming from client interaction for conversation '{ConversationKey}'", context.ConversationKey);

            // Validate client interaction results
            foreach (var result in settings.ClientInteractionResults!)
            {
                if (!_clientInteractionHandler.ValidateResult(result))
                {
                    yield return YieldAndTrack(context, new AiSceneResponse
                    {
                        Status = AiResponseStatus.Error,
                        ErrorMessage = $"Invalid client interaction result for '{result.InteractionId}': {result.Error}",
                        Message = "Client interaction failed. Please try again."
                    });
                    context.ExecutionPhase = ExecutionPhase.Break;
                    yield break;
                }

                _logger.LogInformation("Received client interaction result for '{InteractionId}' with {Count} contents",
                    result.InteractionId, result.Contents?.Count ?? 0);
            }

            // Route directly to the scene from continuation, bypassing scene selection
            settings.ExecutionMode = SceneExecutionMode.Scene;
            settings.SceneName = context.GetProperty<string>("_continuation_sceneName");
        }

        // Rate limiting check
        if (_rateLimiter != null && _settings.RateLimiting?.Enabled == true)
        {
            yield return YieldStatus(AiResponseStatus.Initializing, "Checking rate limit");

            var rateLimitKey = BuildRateLimitKey(context.Metadata);
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

                // Rate limit exceeded - will save cache and send Completed at the end
                // Skip all execution modes and go directly to cache save
                context.ExecutionPhase = ExecutionPhase.Break;
                yield break;
            }

            _logger.LogDebug("Rate limit check passed for key '{Key}'. Remaining: {Remaining}, Reset: {ResetTime}",
                rateLimitKey, checkResult!.RemainingTokens, checkResult.ResetTime);
        }

        // Load previous memory if enabled
        ConversationMemory? previousMemory = null;
        if (_memory != null && _memoryStorage != null && _settings.Memory?.Enabled == true && context.ConversationKey != null)
        {
            yield return YieldStatus(AiResponseStatus.LoadingCache, "Loading conversation memory");

            previousMemory = await _memoryStorage.GetAsync(context.ConversationKey, context.Metadata, settings, cancellationToken);

            if (previousMemory != null)
            {
                _logger.LogInformation(
                    "Previous memory loaded for conversation '{Key}': {ConversationCount} conversations, summary length: {Length} (Factory: {FactoryName})",
                    context.ConversationKey, previousMemory.ConversationCount, previousMemory.Summary?.Length ?? 0, _factoryName);

                // Add memory context to conversation (after InitialContext, before user message)
                context.AddMemoryContext(previousMemory);
            }
            else
            {
                _logger.LogDebug("No previous memory found for conversation '{Key}' (Factory: {FactoryName})",
                    context.ConversationKey, _factoryName);
            }
        }

        // Check if summarization needed for loaded data
        if (_settings.Cache.Enabled && context.ConversationKey != null)
        {
            var messagesToResume = context.ConversationHistory.Where(m => m.ShouldResume).ToList();
            if (_summarizer != null && messagesToResume.Count > _settings.Summarization.ResponseCountThreshold)
            {
                yield return YieldStatus(AiResponseStatus.Summarizing, "Summarizing cached conversation");

                // Convert TrackedMessages to responses for summarizer
                var responsesForSummary = messagesToResume
                    .Select(m => new AiSceneResponse { Message = m.Message.Text })
                    .ToList();

                var summary = await _summarizer.SummarizeAsync(responsesForSummary, cancellationToken);

                // Apply summary to context (marks resumable as summarized, adds summary)
                context.ApplySummary(summary);
            }
        }
    }

    private async IAsyncEnumerable<AiSceneResponse> FinalizePlayFrameworkAsync(
        SceneContext context,
        SceneRequestSettings settings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Save to cache with Completed phase
        if (settings.CacheBehavior != CacheBehavior.Avoidable && _settings.Cache.Enabled && context.ConversationKey != null)
        {
            yield return YieldStatus(AiResponseStatus.SavingCache, "Saving to cache");
            await _playFrameworkCache.SaveAsync(context, ExecutionPhase.Completed, null, cancellationToken);
        }

        // Save updated memory if enabled
        if (_memory != null && _memoryStorage != null && _settings.Memory?.Enabled == true && context.ConversationKey != null)
        {
            yield return YieldStatus(AiResponseStatus.SavingCache, "Saving conversation memory");

            // Get messages for memory
            var memoryMessages = context.GetMessagesForMemory()
                .Select(m => m.Message)
                .ToList();

            try
            {
                // Summarize and save
                var updatedMemory = await _memory.SummarizeAsync(
                    null, // Previous memory already incorporated in conversation
                    context.Input.Text ?? string.Empty,
                    memoryMessages,
                    context.Metadata,
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
        SceneContext context,
        SceneRequestSettings settings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Determine execution mode (request setting overrides default)
        var executionMode = settings.ExecutionMode ?? _settings.DefaultExecutionMode;

        // Get the appropriate handler from factory using the execution mode enum
        var handler = _executionModeHandlerFactory.Create(executionMode);
        if (handler == null)
        {
            yield return YieldAndTrack(context, new AiSceneResponse
            {
                Status = AiResponseStatus.Error,
                ErrorMessage = $"No execution mode handler registered for mode: {executionMode}"
            });

            yield break;
        }

        // Execute using the handler, passing the factory name
        await foreach (var response in handler.ExecuteAsync(_factoryName, context, settings, cancellationToken))
        {
            yield return response;
        }

        yield return YieldStatus(AiResponseStatus.Completed, "Execution completed", context.TotalCost);
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
}
