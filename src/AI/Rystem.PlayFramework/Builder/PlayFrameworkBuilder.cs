using Microsoft.Extensions.DependencyInjection;
using Rystem.PlayFramework.Helpers;
using Rystem.PlayFramework.Telemetry;

namespace Rystem.PlayFramework;

/// <summary>
/// Fluent builder for configuring PlayFramework.
/// </summary>
public sealed class PlayFrameworkBuilder
{
    internal IServiceCollection Services { get; }
    internal AnyOf<string?, Enum>? Name { get; }
    internal PlayFrameworkSettings Settings { get; } = new();
    internal List<SceneConfiguration> Scenes { get; } = [];
    internal List<ActorConfiguration> MainActors { get; } = [];
    internal bool HasCustomPlanner { get; set; }
    internal bool HasCustomSummarizer { get; set; }
    internal bool HasCustomDirector { get; set; }
    internal bool HasCustomCache { get; set; }
    internal bool HasCustomJsonService { get; set; }
    internal bool HasCustomTransientErrorDetector { get; set; }
    internal bool HasCustomMemory { get; set; }
    internal bool HasCustomMemoryStorage { get; set; }
    internal Type? CustomMemoryType { get; set; }
    internal Type? CustomMemoryStorageType { get; set; }

    internal PlayFrameworkBuilder(IServiceCollection services, AnyOf<string?, Enum>? name = null)
    {
        Services = services;
        Name = name;
    }

    /// <summary>
    /// Enables planning with default planner.
    /// </summary>
    public PlayFrameworkBuilder WithPlanning()
    {
        Settings.Planning.Enabled = true;
        return this;
    }

    /// <summary>
    /// Configures planning settings.
    /// </summary>
    public PlayFrameworkBuilder WithPlanning(Action<PlanningSettings> configure)
    {
        Settings.Planning.Enabled = true;
        configure(Settings.Planning);
        return this;
    }

    /// <summary>
    /// Enables summarization with default summarizer.
    /// </summary>
    public PlayFrameworkBuilder WithSummarization()
    {
        Settings.Summarization.Enabled = true;
        return this;
    }

    /// <summary>
    /// Configures summarization settings.
    /// </summary>
    public PlayFrameworkBuilder WithSummarization(Action<SummarizationSettings> configure)
    {
        Settings.Summarization.Enabled = true;
        configure(Settings.Summarization);
        return this;
    }

    /// <summary>
    /// Enables director with default implementation.
    /// </summary>
    public PlayFrameworkBuilder WithDirector()
    {
        Settings.Director.Enabled = true;
        return this;
    }

    /// <summary>
    /// Configures director settings.
    /// </summary>
    public PlayFrameworkBuilder WithDirector(Action<DirectorSettings> configure)
    {
        Settings.Director.Enabled = true;
        configure(Settings.Director);
        return this;
    }

    /// <summary>
    /// Internal method for configuring settings (used by extension methods).
    /// </summary>
    internal PlayFrameworkBuilder ConfigureSettings(Action<PlayFrameworkSettings> configure)
    {
        configure(Settings);
        return this;
    }

    #region Load Balancing & Retry

    /// <summary>
    /// Configures primary chat client pool for load balancing.
    /// </summary>
    /// <param name="clientNames">Named chat clients to use (must be registered with AddKeyedSingleton or AddFactory)</param>
    /// <param name="mode">Load balancing mode (default: RoundRobin)</param>
    public PlayFrameworkBuilder WithChatClients(IEnumerable<string> clientNames, LoadBalancingMode mode = LoadBalancingMode.RoundRobin)
    {
        Settings.ChatClientNames = clientNames.ToList();
        Settings.LoadBalancingMode = mode;
        return this;
    }

    /// <summary>
    /// Configures fallback chat client chain (used when all primary clients fail).
    /// </summary>
    /// <param name="clientNames">Fallback client names in order of preference</param>
    /// <param name="mode">Fallback mode (default: Sequential)</param>
    public PlayFrameworkBuilder WithFallbackClients(IEnumerable<string> clientNames, FallbackMode mode = FallbackMode.Sequential)
    {
        Settings.FallbackChatClientNames = clientNames.ToList();
        Settings.FallbackMode = mode;
        return this;
    }

    /// <summary>
    /// Configures retry behavior for transient errors.
    /// </summary>
    /// <param name="maxAttempts">Maximum retry attempts per client (default: 3)</param>
    /// <param name="baseDelaySeconds">Base delay for exponential backoff (default: 1.0)</param>
    public PlayFrameworkBuilder WithRetry(int maxAttempts = 3, double baseDelaySeconds = 1.0)
    {
        Settings.MaxRetryAttempts = maxAttempts;
        Settings.RetryBaseDelaySeconds = baseDelaySeconds;
        return this;
    }

    #endregion

    #region Telemetry

    /// <summary>
    /// Configures telemetry settings.
    /// </summary>
    public PlayFrameworkBuilder WithTelemetry(Action<TelemetrySettings> configure)
    {
        configure(Settings.Telemetry);
        return this;
    }

    #endregion

    /// <summary>
    /// Sets the default execution mode for all requests.
    /// Can be overridden per-request via SceneRequestSettings.ExecutionMode.
    /// </summary>
    /// <param name="mode">Default execution mode (Direct, Planning, or DynamicChaining)</param>
    public PlayFrameworkBuilder WithExecutionMode(SceneExecutionMode mode)
    {
        Settings.DefaultExecutionMode = mode;

        // If Planning mode is selected, enable planning automatically
        if (mode == SceneExecutionMode.Planning)
        {
            Settings.Planning.Enabled = true;
        }

        return this;
    }

    /// <summary>
    /// Adds a main actor (executed before any scene).
    /// </summary>
    public PlayFrameworkBuilder AddMainActor(string message, bool cacheForSubsequentCalls = false)
    {
        MainActors.Add(new ActorConfiguration
        {
            StaticMessage = message,
            CacheForSubsequentCalls = cacheForSubsequentCalls
        });
        return this;
    }

    /// <summary>
    /// Adds a dynamic main actor.
    /// </summary>
    public PlayFrameworkBuilder AddMainActor(Func<SceneContext, string> messageFactory, bool cacheForSubsequentCalls = false)
    {
        MainActors.Add(new ActorConfiguration
        {
            MessageFactory = messageFactory,
            CacheForSubsequentCalls = cacheForSubsequentCalls
        });
        return this;
    }

    /// <summary>
    /// Adds an async dynamic main actor.
    /// </summary>
    public PlayFrameworkBuilder AddMainActor(Func<SceneContext, CancellationToken, Task<string>> asyncMessageFactory, bool cacheForSubsequentCalls = false)
    {
        MainActors.Add(new ActorConfiguration
        {
            AsyncMessageFactory = asyncMessageFactory,
            CacheForSubsequentCalls = cacheForSubsequentCalls
        });
        return this;
    }

    /// <summary>
    /// Adds a custom actor type as main actor.
    /// </summary>
    public PlayFrameworkBuilder AddMainActor<TActor>(bool cacheForSubsequentCalls = false) where TActor : class, IActor
    {
        MainActors.Add(new ActorConfiguration
        {
            ActorType = typeof(TActor),
            CacheForSubsequentCalls = cacheForSubsequentCalls
        });
        return this;
    }

    /// <summary>
    /// Configures caching.
    /// </summary>
    public PlayFrameworkBuilder AddCache(Action<CacheBuilder> configure)
    {
        var builder = new CacheBuilder(this);
        configure(builder);
        return this;
    }

    /// <summary>
    /// Enables cost tracking with default settings (USD currency, costs must be configured).
    /// </summary>
    public PlayFrameworkBuilder WithCostTracking()
    {
        Settings.CostTracking.Enabled = true;
        return this;
    }

    /// <summary>
    /// Configures cost tracking with specific currency and token costs.
    /// </summary>
    /// <param name="currency">Currency code (e.g., "USD", "EUR", "GBP")</param>
    /// <param name="inputCostPer1K">Cost per 1,000 input tokens</param>
    /// <param name="outputCostPer1K">Cost per 1,000 output tokens</param>
    /// <param name="cachedInputCostPer1K">Cost per 1,000 cached input tokens (optional, defaults to inputCost * 0.1)</param>
    public PlayFrameworkBuilder WithCostTracking(
        string currency,
        decimal inputCostPer1K,
        decimal outputCostPer1K,
        decimal? cachedInputCostPer1K = null)
    {
        Settings.CostTracking.Enabled = true;
        Settings.CostTracking.Currency = currency;
        Settings.CostTracking.InputTokenCostPer1K = inputCostPer1K;
        Settings.CostTracking.OutputTokenCostPer1K = outputCostPer1K;
        Settings.CostTracking.CachedInputTokenCostPer1K = cachedInputCostPer1K ?? (inputCostPer1K * 0.1m);
        return this;
    }

    /// <summary>
    /// Configures cost tracking with a configuration action.
    /// </summary>
    public PlayFrameworkBuilder WithCostTracking(Action<TokenCostSettings> configure)
    {
        Settings.CostTracking.Enabled = true;
        configure(Settings.CostTracking);
        return this;
    }

    /// <summary>
    /// Adds model-specific cost configuration (e.g., different costs for GPT-4 vs GPT-3.5).
    /// </summary>
    /// <param name="modelId">Model identifier (e.g., "gpt-4", "gpt-3.5-turbo")</param>
    /// <param name="inputCostPer1K">Cost per 1,000 input tokens for this model</param>
    /// <param name="outputCostPer1K">Cost per 1,000 output tokens for this model</param>
    /// <param name="cachedInputCostPer1K">Cost per 1,000 cached input tokens (optional)</param>
    public PlayFrameworkBuilder WithModelCosts(
        string modelId,
        decimal inputCostPer1K,
        decimal outputCostPer1K,
        decimal? cachedInputCostPer1K = null)
    {
        Settings.CostTracking.ModelCosts[modelId] = new ModelCostSettings
        {
            ModelId = modelId,
            InputTokenCostPer1K = inputCostPer1K,
            OutputTokenCostPer1K = outputCostPer1K,
            CachedInputTokenCostPer1K = cachedInputCostPer1K ?? (inputCostPer1K * 0.1m)
        };
        return this;
    }

    /// <summary>
    /// Adds client-specific cost configuration for per-client cost tracking.
    /// Use this when different Azure regions or contracts have different pricing.
    /// Client costs take priority over model costs.
    /// </summary>
    /// <param name="clientName">Client name as registered in DI (e.g., "gpt4o-east", "gpt4o-west", "claude-fallback")</param>
    /// <param name="inputCostPer1K">Cost per 1,000 input tokens for this client</param>
    /// <param name="outputCostPer1K">Cost per 1,000 output tokens for this client</param>
    /// <param name="cachedInputCostPer1K">Cost per 1,000 cached input tokens (optional)</param>
    public PlayFrameworkBuilder WithClientCosts(
        string clientName,
        decimal inputCostPer1K,
        decimal outputCostPer1K,
        decimal? cachedInputCostPer1K = null)
    {
        Settings.CostTracking.ClientCosts[clientName] = new ClientCostSettings
        {
            ClientName = clientName,
            InputTokenCostPer1K = inputCostPer1K,
            OutputTokenCostPer1K = outputCostPer1K,
            CachedInputTokenCostPer1K = cachedInputCostPer1K ?? (inputCostPer1K * 0.1m)
        };
        return this;
    }

    /// <summary>
    /// Adds a scene with required name, description, and configuration.
    /// </summary>
    /// <param name="name">Scene name (required)</param>
    /// <param name="description">Scene description (required)</param>
    /// <param name="configure">Action to configure actors, tools, and other scene settings (required, use empty lambda if no configuration needed)</param>
    public PlayFrameworkBuilder AddScene(string name, string description, Action<SceneBuilder> configure)
    {
        var sceneConfig = new SceneConfiguration
        {
            Name = ToolNameNormalizer.Normalize(name),
            Description = description
        };

        var builder = new SceneBuilder(sceneConfig, Services);
        configure(builder);

        Scenes.Add(sceneConfig);
        return this;
    }

    /// <summary>
    /// Uses a custom planner.
    /// </summary>
    public PlayFrameworkBuilder AddCustomPlanner<TPlanner>() where TPlanner : class, IPlanner
    {
        Services.AddFactory<IPlanner, TPlanner>(Name, ServiceLifetime.Transient);
        HasCustomPlanner = true;
        return this;
    }

    /// <summary>
    /// Uses a custom summarizer.
    /// </summary>
    public PlayFrameworkBuilder AddCustomSummarizer<TSummarizer>() where TSummarizer : class, ISummarizer
    {
        Services.AddFactory<ISummarizer, TSummarizer>(Name, ServiceLifetime.Transient);
        HasCustomSummarizer = true;
        return this;
    }
    /// <summary>
    /// Uses a custom summarizer.
    /// </summary>
    public PlayFrameworkBuilder AddContext<TContext>() where TContext : class, IContext
    {
        Services.AddFactory<IContext, TContext>(Name, ServiceLifetime.Transient);
        return this;
    }

    /// <summary>
    /// Uses a custom director.
    /// </summary>
    public PlayFrameworkBuilder AddCustomDirector<TDirector>() where TDirector : class, IDirector
    {
        Services.AddFactory<IDirector, TDirector>(Name, ServiceLifetime.Transient);
        HasCustomDirector = true;
        return this;
    }

    /// <summary>
    /// Uses a custom JSON service.
    /// </summary>
    public PlayFrameworkBuilder AddCustomJsonService<TJsonService>() where TJsonService : class, IJsonService
    {
        Services.AddFactory<IJsonService, TJsonService>(Name, ServiceLifetime.Singleton);
        HasCustomJsonService = true;
        return this;
    }

    /// <summary>
    /// Uses a custom JSON service with a factory.
    /// </summary>
    public PlayFrameworkBuilder AddCustomJsonService(Func<IServiceProvider, IJsonService> factory)
    {
        Func<IServiceProvider, object?, IJsonService> factoryFunc = (sp, _) => factory(sp);
        Services.AddFactory(factoryFunc, Name, ServiceLifetime.Singleton);
        HasCustomJsonService = true;
        return this;
    }
}

/// <summary>
/// Internal configuration for an actor.
/// </summary>
internal sealed class ActorConfiguration
{
    public string? StaticMessage { get; set; }
    public Func<SceneContext, string>? MessageFactory { get; set; }
    public Func<SceneContext, CancellationToken, Task<string>>? AsyncMessageFactory { get; set; }
    public Type? ActorType { get; set; }
    public bool CacheForSubsequentCalls { get; set; }
}
