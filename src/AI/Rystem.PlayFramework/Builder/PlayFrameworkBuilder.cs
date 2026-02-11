using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework;

/// <summary>
/// Fluent builder for configuring PlayFramework.
/// </summary>
public sealed class PlayFrameworkBuilder
{
    internal IServiceCollection Services { get; }
    internal PlayFrameworkSettings Settings { get; } = new();
    internal List<SceneConfiguration> Scenes { get; } = [];
    internal List<ActorConfiguration> MainActors { get; } = [];
    internal bool HasCustomPlanner { get; set; }
    internal bool HasCustomSummarizer { get; set; }
    internal bool HasCustomDirector { get; set; }
    internal bool HasCustomCache { get; set; }

    internal PlayFrameworkBuilder(IServiceCollection services)
    {
        Services = services;
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
    /// Configures all settings.
    /// </summary>
    public PlayFrameworkBuilder Configure(Action<PlayFrameworkSettings> configure)
    {
        configure(Settings);
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
    /// Adds a scene.
    /// </summary>
    public PlayFrameworkBuilder AddScene(Action<SceneBuilder> configure)
    {
        var sceneConfig = new SceneConfiguration();
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
        Services.AddScoped<IPlanner, TPlanner>();
        HasCustomPlanner = true;
        return this;
    }

    /// <summary>
    /// Uses a custom summarizer.
    /// </summary>
    public PlayFrameworkBuilder AddCustomSummarizer<TSummarizer>() where TSummarizer : class, ISummarizer
    {
        Services.AddScoped<ISummarizer, TSummarizer>();
        HasCustomSummarizer = true;
        return this;
    }

    /// <summary>
    /// Uses a custom director.
    /// </summary>
    public PlayFrameworkBuilder AddCustomDirector<TDirector>() where TDirector : class, IDirector
    {
        Services.AddScoped<IDirector, TDirector>();
        HasCustomDirector = true;
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
