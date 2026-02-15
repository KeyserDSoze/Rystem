using Rystem.PlayFramework.Configuration;
using Rystem.PlayFramework.Mcp;

namespace Rystem.PlayFramework;

/// <summary>
/// Represents a scene with tools and actors.
/// </summary>
public interface IScene
{
    /// <summary>
    /// Scene name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Scene description for AI selection.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// MCP server references configured for this scene.
    /// </summary>
    IReadOnlyList<McpServerReference> McpServerReferences { get; }

    /// <summary>
    /// Client-side tool definitions registered via OnClient().
    /// Null if no client tools are configured.
    /// </summary>
    IReadOnlyList<ClientInteractionDefinition>? ClientInteractionDefinitions { get; }

    /// <summary>
    /// Cache expiration time for continuation tokens.
    /// Only relevant when OnClient() is used.
    /// </summary>
    TimeSpan CacheExpiration { get; }

    /// <summary>
    /// Gets all tools available in this scene.
    /// </summary>
    IEnumerable<ISceneTool> GetTools();

    /// <summary>
    /// Gets all actors in this scene.
    /// </summary>
    IEnumerable<IActor> GetActors();

    /// <summary>
    /// Executes all actors and adds their context to the chat client.
    /// </summary>
    Task ExecuteActorsAsync(
        SceneContext context,
        CancellationToken cancellationToken = default);
}
