namespace Rystem.PlayFramework.Services.ExecutionModes;

/// <summary>
/// Handles the execution logic of a single scene.
/// </summary>
internal interface ISceneExecutor
{
    /// <summary>
    /// Executes a specific scene with tools, actors, and MCP integrations.
    /// </summary>
    IAsyncEnumerable<AiSceneResponse> ExecuteSceneAsync(
        SceneContext context,
        IScene scene,
        SceneRequestSettings settings,
        CancellationToken cancellationToken = default);
}
