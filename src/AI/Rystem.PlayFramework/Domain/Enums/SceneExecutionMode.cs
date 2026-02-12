namespace Rystem.PlayFramework;

/// <summary>
/// Defines how scenes are selected and executed.
/// </summary>
public enum SceneExecutionMode
{
    /// <summary>
    /// Direct execution: single scene selection without planning.
    /// Fast, suitable for simple queries.
    /// </summary>
    Direct = 0,

    /// <summary>
    /// Planning mode: creates upfront execution plan for all scenes.
    /// Requires IPlanner implementation.
    /// Suitable for complex workflows with known dependencies.
    /// </summary>
    Planning = 1,

    /// <summary>
    /// Dynamic scene chaining: scenes are selected live based on results.
    /// LLM decides after each scene whether to continue.
    /// Suitable for exploratory workflows with unknown dependencies.
    /// </summary>
    DynamicChaining = 2
}
