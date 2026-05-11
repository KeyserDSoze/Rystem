namespace Rystem.PlayFramework;

/// <summary>
/// Hook that runs once when a terminal status is detected
/// (<see cref="AiResponseStatus.Completed"/>, <see cref="AiResponseStatus.Error"/>,
/// <see cref="AiResponseStatus.BudgetExceeded"/>, <see cref="AiResponseStatus.Unauthorized"/>).
/// The terminal response is sent to the client first; extra items returned by this hook are
/// appended to the SSE stream afterwards.
/// </summary>
public interface IPlayFrameworkOnTerminalScene
{
    /// <summary>
    /// Called after the terminal scene has been sent to the client.
    /// </summary>
    /// <param name="terminalScene">The scene response that triggered the terminal state.</param>
    /// <param name="context">Shared execution context for this request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>null</c> or empty = no additional items.
    /// Non-empty list = additional items to append to the SSE stream (in order, bypassing after-each-scene hooks).
    /// </returns>
    Task<IEnumerable<AiSceneResponse>?> OnTerminalAsync(
        AiSceneResponse terminalScene,
        PlayFrameworkExecutionContext context,
        CancellationToken cancellationToken = default);
}
