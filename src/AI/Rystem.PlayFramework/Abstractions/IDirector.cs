namespace Rystem.PlayFramework;

/// <summary>
/// Interface for orchestrating multi-scene execution.
/// </summary>
public interface IDirector
{
    /// <summary>
    /// Decides whether execution should continue after scene execution.
    /// </summary>
    /// <param name="context">Current scene context.</param>
    /// <param name="settings">Request settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Director decision.</returns>
    Task<DirectorResponse> DirectAsync(
        SceneContext context,
        SceneRequestSettings settings,
        CancellationToken cancellationToken = default);
}
