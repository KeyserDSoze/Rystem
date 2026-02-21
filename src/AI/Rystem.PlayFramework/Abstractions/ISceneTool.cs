using System.Text.Json;
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

    AITool ToolDescription { get; }
    /// <summary>
    /// Executes the tool with given arguments.
    /// </summary>
    Task<object?> ExecuteAsync(
        string arguments,
        SceneContext context,
        CancellationToken cancellationToken);
}
