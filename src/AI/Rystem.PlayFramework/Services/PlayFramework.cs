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
        return _sceneManagerFactory.Create(name) ?? _serviceProvider.GetService<ISceneManager>();
    }

    /// <inheritdoc/>
    public ISceneManager GetDefault()
    {
        var manager = _sceneManagerFactory.Create(null);
        if (manager == null)
        {
            throw new InvalidOperationException("No default PlayFramework configuration found. Register a default configuration using AddPlayFramework() without a key.");
        }
        return manager;
    }

    /// <inheritdoc/>
    public ISceneManager Get(AnyOf<string?, Enum> name)
    {
        var manager = _sceneManagerFactory.Create(name);
        if (manager == null)
        {
            throw new InvalidOperationException($"PlayFramework configuration '{name}' not found. Make sure to register it using AddPlayFramework(\"{name}\", ...).");
        }
        return manager;
    }

    /// <inheritdoc/>
    public bool Exists(AnyOf<string?, Enum>? name = null)
    {
        return _sceneManagerFactory.Exists(name);
    }
}
