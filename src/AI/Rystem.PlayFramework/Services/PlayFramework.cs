using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework;

/// <summary>
/// Default implementation of IPlayFramework that provides keyed access to ISceneManager.
/// </summary>
internal sealed class PlayFramework : IPlayFramework
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IFactory<ISceneManager> _sceneManagerFactory;

    public PlayFramework(
        IServiceProvider serviceProvider,
        IFactory<ISceneManager> sceneManagerFactory)
    {
        _serviceProvider = serviceProvider;
        _sceneManagerFactory = sceneManagerFactory;
    }

    /// <inheritdoc/>
    public ISceneManager? Create(AnyOf<string?, Enum>? name = null)
    {
        return _sceneManagerFactory.Create(name);
    }

    /// <inheritdoc/>
    public ISceneManager? CreateOrDefault(AnyOf<string?, Enum>? name = null)
    {
        var manager = _sceneManagerFactory.Create(name) ?? _serviceProvider.GetService<ISceneManager>();
        return manager ?? throw new InvalidOperationException($"No {name} or default PlayFramework configuration found. Register a default configuration using AddPlayFramework() with or without a key.");
    }

    /// <inheritdoc/>
    public bool Exists(AnyOf<string?, Enum>? name = null)
    {
        return _sceneManagerFactory.Exists(name);
    }
}
