namespace Rystem.PlayFramework;

/// <summary>
/// Interface for actors that provide dynamic context.
/// </summary>
public interface IActor
{
    /// <summary>
    /// Executes the actor and returns system message(s).
    /// </summary>
    /// <param name="context">Current scene context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Actor response with system message.</returns>
    Task<ActorResponse> PlayAsync(
        SceneContext context,
        CancellationToken cancellationToken = default);
}
