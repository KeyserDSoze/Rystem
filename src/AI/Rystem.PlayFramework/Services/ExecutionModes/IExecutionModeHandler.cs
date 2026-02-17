using Rystem.PlayFramework;

namespace Rystem.PlayFramework.Services.ExecutionModes;

/// <summary>
/// Defines a handler for a specific scene execution mode.
/// </summary>
internal interface IExecutionModeHandler
{
    /// <summary>
    /// Executes the scene logic for this execution mode.
    /// </summary>
    IAsyncEnumerable<AiSceneResponse> ExecuteAsync(
        SceneContext context,
        SceneRequestSettings settings,
        CancellationToken cancellationToken = default);
}
