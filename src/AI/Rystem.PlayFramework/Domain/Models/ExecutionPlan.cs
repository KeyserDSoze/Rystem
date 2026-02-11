namespace Rystem.PlayFramework;

/// <summary>
/// Execution plan created by the planner.
/// </summary>
public sealed class ExecutionPlan
{
    /// <summary>
    /// Whether execution is needed (false = direct answer available).
    /// </summary>
    public bool NeedsExecution { get; set; }

    /// <summary>
    /// Reasoning behind the plan decision.
    /// </summary>
    public string? Reasoning { get; set; }

    /// <summary>
    /// Ordered list of steps to execute.
    /// </summary>
    public List<PlanStep> Steps { get; set; } = [];

    /// <summary>
    /// Timestamp when plan was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// A single step in the execution plan.
/// </summary>
public sealed class PlanStep
{
    /// <summary>
    /// Step number (1-based).
    /// </summary>
    public int StepNumber { get; set; }

    /// <summary>
    /// Scene to execute.
    /// </summary>
    public required string SceneName { get; set; }

    /// <summary>
    /// Purpose of this step.
    /// </summary>
    public string? Purpose { get; set; }

    /// <summary>
    /// Expected tools to be called in this step.
    /// </summary>
    public List<string> ExpectedTools { get; set; } = [];

    /// <summary>
    /// Step number this step depends on (null if no dependency).
    /// </summary>
    public int? DependsOnStep { get; set; }

    /// <summary>
    /// Whether this step has been completed.
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Result of executing this step.
    /// </summary>
    public string? Result { get; set; }
}
