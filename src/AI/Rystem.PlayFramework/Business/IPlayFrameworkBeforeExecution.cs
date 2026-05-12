namespace Rystem.PlayFramework;

/// <summary>
/// Hook that runs once before the scene stream starts.
/// Can allow, deny, or short-circuit the execution pipeline.
/// </summary>
public interface IPlayFrameworkBeforeExecution
{
    /// <summary>
    /// Called before PlayFramework starts streaming.
    /// </summary>
    /// <param name="context">Shared execution context for this request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <see cref="PlayFrameworkGuardResult.Allow()"/> to proceed,
    /// <see cref="PlayFrameworkGuardResult.Deny(int, string?)"/> to block with an HTTP error,
    /// or <see cref="PlayFrameworkGuardResult.ShortCircuit(AiSceneResponse)"/> to return a synthetic SSE response.
    /// </returns>
    Task<PlayFrameworkGuardResult> BeforeExecutionAsync(
        PlayFrameworkExecutionContext context,
        CancellationToken cancellationToken = default);
}
