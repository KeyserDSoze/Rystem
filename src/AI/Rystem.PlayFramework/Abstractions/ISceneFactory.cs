using Microsoft.Extensions.AI;

namespace Rystem.PlayFramework;

/// <summary>
/// Factory for creating scenes.
/// </summary>
public interface ISceneFactory
{
    /// <summary>
    /// Gets all registered scene names.
    /// </summary>
    IReadOnlyList<string> SceneNames { get; }
    /// <summary>
    /// Gets all registered scenes.
    /// </summary>
    IReadOnlyList<IScene> Scenes { get; }
    /// <summary>
    /// Gets all registered scenes as AiTool.
    /// </summary>
    IReadOnlyList<AITool> ScenesAsAiTool { get; }
    /// <summary>
    /// Get scene by name
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    IScene? TryGetScene(string name);
}
