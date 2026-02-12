using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Rystem.PlayFramework;

/// <summary>
/// Extension methods for IServiceCollection to configure PlayFramework.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds PlayFramework services to the DI container.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configure">Configuration action.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddPlayFramework(
        this IServiceCollection services,
        Action<PlayFrameworkBuilder> configure)
    {
        return AddPlayFramework(services, null, configure);
    }

    /// <summary>
    /// Adds PlayFramework services with a specific key for factory-based resolution.
    /// Use IFactory&lt;ISceneManager&gt; to resolve by key.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="name">Factory name (can be string or enum).</param>
    /// <param name="configure">Configuration action.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddPlayFramework(
        this IServiceCollection services,
        AnyOf<string?, Enum>? name,
        Action<PlayFrameworkBuilder> configure)
    {
        var builder = new PlayFrameworkBuilder(services, name);
        configure(builder);

        // Ensure all IFactory<T> types that SceneManager depends on are registered
        // This allows them to be injected even when the service itself is not registered
        services.AddEngineFactory<ISceneFactory>();
        services.AddEngineFactory<IChatClient>();
        services.AddEngineFactory<PlayFrameworkSettings>();
        services.AddEngineFactory<List<SceneConfiguration>>();
        services.AddEngineFactory<List<ActorConfiguration>>();
        services.AddEngineFactory<ICostCalculator>();
        services.AddEngineFactory<IPlanner>();
        services.AddEngineFactory<ISummarizer>();
        services.AddEngineFactory<IDirector>();
        services.AddEngineFactory<ICacheService>();
        services.AddEngineFactory<IJsonService>();

        // Register SceneManager with factory pattern
        services.AddFactory<ISceneManager, SceneManager>(name, ServiceLifetime.Transient);

        // Register SceneFactory with factory pattern (used by SceneManager)
        services.AddFactory<ISceneFactory>((sp, _) =>
        {
            var scenesFactory = sp.GetRequiredService<IFactory<List<SceneConfiguration>>>();
            var scenes = scenesFactory.Create(name) ?? [];
            return new SceneFactory(scenes, sp);
        }, name, ServiceLifetime.Transient);

        // Register settings with factory pattern (Singleton) - using instance overload
        services.AddFactory(builder.Settings, name, ServiceLifetime.Singleton);

        // Register scenes configuration with factory pattern (Singleton)
        services.AddFactory(builder.Scenes, name, ServiceLifetime.Singleton);

        // Register main actors with factory pattern (Singleton)
        services.AddFactory(builder.MainActors, name, ServiceLifetime.Singleton);

        // Register cost calculator with factory pattern (Singleton)
        var costCalculator = builder.Settings.CostTracking.Enabled
            ? new CostCalculator(builder.Settings.CostTracking)
            : new CostCalculator(new TokenCostSettings { Enabled = false });
        services.AddFactory<ICostCalculator>(costCalculator, name, ServiceLifetime.Singleton);

        // Register default planner if not customized and planning is enabled
        if (!builder.HasCustomPlanner && builder.Settings.Planning.Enabled)
        {
            services.AddFactory<IPlanner, DeterministicPlanner>(name, ServiceLifetime.Transient);
        }

        // Register default summarizer if not customized and summarization is enabled
        if (!builder.HasCustomSummarizer && builder.Settings.Summarization.Enabled)
        {
            services.AddFactory<ISummarizer, DefaultSummarizer>(name, ServiceLifetime.Transient);
        }

        // Register default director if not customized and director is enabled
        if (!builder.HasCustomDirector && builder.Settings.Director.Enabled)
        {
            services.AddFactory<IDirector, MainDirector>(name, ServiceLifetime.Transient);
        }

        // Register default cache service if not customized and cache is enabled
        if (!builder.HasCustomCache && builder.Settings.Cache.Enabled)
        {
            services.AddFactory<ICacheService, CacheService>(name, ServiceLifetime.Transient);
        }

        // Register default JSON service if not customized
        if (!builder.HasCustomJsonService)
        {
            services.AddFactory<IJsonService, DefaultJsonService>(name, ServiceLifetime.Singleton);
        }

        // Register actor types from scenes (Transient)
        foreach (var scene in builder.Scenes)
        {
            foreach (var actor in scene.Actors.Where(a => a.ActorType != null))
            {
                services.TryAddTransient(actor.ActorType!);
            }
        }

        // Register main actor types (Transient)
        foreach (var actor in builder.MainActors.Where(a => a.ActorType != null))
        {
            services.TryAddTransient(actor.ActorType!);
        }

        // Register IPlayFramework wrapper (always Transient)
        services.TryAddTransient<IPlayFramework, PlayFramework>();

        return services;
    }
}
