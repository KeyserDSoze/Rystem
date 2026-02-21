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
    /// <param name="factoryName">The factory name for resolving dependencies.</param>
    /// <param name="context">The scene execution context.</param>
    /// <param name="settings">Request settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    IAsyncEnumerable<AiSceneResponse> ExecuteAsync(
        AnyOf<string?, Enum>? factoryName,
        SceneContext context,
        SceneRequestSettings settings,
        CancellationToken cancellationToken = default);
}
