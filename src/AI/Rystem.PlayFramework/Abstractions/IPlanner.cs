namespace Rystem.PlayFramework;

/// <summary>
/// Interface for planning scene execution.
/// </summary>
public interface IPlanner
{
    /// <summary>
    /// Creates an execution plan based on current context.
    /// </summary>
    /// <param name="context">Current scene context.</param>
    /// <param name="settings">Request settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution plan.</returns>
    Task<ExecutionPlan> CreatePlanAsync(
        SceneContext context,
        SceneRequestSettings settings,
        CancellationToken cancellationToken = default);
}
