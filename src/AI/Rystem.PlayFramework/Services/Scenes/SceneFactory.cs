namespace Rystem.PlayFramework;

/// <summary>
/// Factory for creating scenes.
/// </summary>
internal sealed class SceneFactory : ISceneFactory
{
    internal readonly List<SceneConfiguration> _configurations;
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, IScene> _cache = new(StringComparer.OrdinalIgnoreCase);

    public SceneFactory(
        List<SceneConfiguration> configurations,
        IServiceProvider serviceProvider)
    {
        _configurations = configurations;
        _serviceProvider = serviceProvider;
    }

    public IScene Create(string sceneName)
    {
        // Check cache first
        if (_cache.TryGetValue(sceneName, out var cached))
        {
            return cached;
        }

        // Find configuration
        var config = _configurations.FirstOrDefault(c => 
            c.Name.Equals(sceneName, StringComparison.OrdinalIgnoreCase));

        if (config == null)
        {
            throw new InvalidOperationException($"Scene '{sceneName}' not found");
        }

        // Create and cache scene
        var scene = new Scene(config, _serviceProvider);
        _cache[sceneName] = scene;

        return scene;
    }

    public IEnumerable<string> GetSceneNames()
    {
        return _configurations.Select(c => c.Name);
    }
}
