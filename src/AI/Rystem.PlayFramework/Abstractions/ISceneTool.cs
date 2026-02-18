using Microsoft.Extensions.AI;

namespace Rystem.PlayFramework;

/// <summary>
/// Represents a tool that can be called in a scene.
/// </summary>
public interface ISceneTool
{
    /// <summary>
    /// Tool name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Tool description.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Converts this tool to an AITool for Microsoft.Extensions.AI.
    /// </summary>
    AITool ToAITool();

    /// <summary>
    /// Executes the tool with given arguments.
    /// </summary>
    Task<object?> ExecuteAsync(
        string arguments,
        SceneContext context,
        CancellationToken cancellationToken = default);
}
