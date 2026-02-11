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
        var builder = new PlayFrameworkBuilder(services);
        configure(builder);

        // Register settings as singleton
        services.AddSingleton(builder.Settings);

        // Register scene configurations
        services.AddSingleton(builder.Scenes);
        services.AddSingleton(builder.MainActors);

        // Register core services
        services.TryAddScoped<ISceneManager, SceneManager>();
        services.TryAddScoped<ISceneFactory, SceneFactory>();

        // Register default implementations if not customized
        if (!builder.HasCustomPlanner && builder.Settings.Planning.Enabled)
        {
            services.TryAddScoped<IPlanner, DeterministicPlanner>();
        }

        if (!builder.HasCustomSummarizer && builder.Settings.Summarization.Enabled)
        {
            services.TryAddScoped<ISummarizer, DefaultSummarizer>();
        }

        if (!builder.HasCustomDirector && builder.Settings.Director.Enabled)
        {
            services.TryAddScoped<IDirector, MainDirector>();
        }

        if (!builder.HasCustomCache && builder.Settings.Cache.Enabled)
        {
            services.TryAddScoped<ICacheService, CacheService>();
        }

        // Register actor types from scenes
        foreach (var scene in builder.Scenes)
        {
            foreach (var actor in scene.Actors.Where(a => a.ActorType != null))
            {
                services.TryAddScoped(actor.ActorType!);
            }
        }

        // Register main actor types
        foreach (var actor in builder.MainActors.Where(a => a.ActorType != null))
        {
            services.TryAddScoped(actor.ActorType!);
        }

        return services;
    }
}
