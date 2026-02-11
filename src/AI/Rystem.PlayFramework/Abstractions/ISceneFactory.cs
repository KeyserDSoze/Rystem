namespace Rystem.PlayFramework;

/// <summary>
/// Factory for creating scenes.
/// </summary>
public interface ISceneFactory
{
    /// <summary>
    /// Creates a scene by name.
    /// </summary>
    IScene Create(string sceneName);

    /// <summary>
    /// Gets all registered scene names.
    /// </summary>
    IEnumerable<string> GetSceneNames();
}
