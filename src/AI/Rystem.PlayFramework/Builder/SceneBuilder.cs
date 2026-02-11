using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework;

/// <summary>
/// Builder for configuring a scene.
/// </summary>
public sealed class SceneBuilder
{
    private readonly SceneConfiguration _config;
    private readonly IServiceCollection _services;

    internal SceneBuilder(SceneConfiguration config, IServiceCollection services)
    {
        _config = config;
        _services = services;
    }

    /// <summary>
    /// Sets the scene name.
    /// </summary>
    public SceneBuilder WithName(string name)
    {
        _config.Name = name;
        return this;
    }

    /// <summary>
    /// Sets the scene description.
    /// </summary>
    public SceneBuilder WithDescription(string description)
    {
        _config.Description = description;
        return this;
    }

    /// <summary>
    /// Adds service methods as tools.
    /// </summary>
    public SceneBuilder WithService<TService>(Action<ServiceToolBuilder<TService>> configure) where TService : class
    {
        var builder = new ServiceToolBuilder<TService>(_config);
        configure(builder);
        return this;
    }

    /// <summary>
    /// Configures actors for this scene.
    /// </summary>
    public SceneBuilder WithActors(Action<ActorBuilder> configure)
    {
        var builder = new ActorBuilder(_config);
        configure(builder);
        return this;
    }
}

/// <summary>
/// Internal configuration for a scene.
/// </summary>
internal sealed class SceneConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ServiceToolConfiguration> ServiceTools { get; set; } = [];
    public List<ActorConfiguration> Actors { get; set; } = [];
}
