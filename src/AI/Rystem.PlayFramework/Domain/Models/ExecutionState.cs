using System.Text.Json.Serialization;

namespace Rystem.PlayFramework;

/// <summary>
/// Represents the execution state that can be saved and restored from cache.
/// This enables resuming conversations without restarting the entire execution flow.
/// </summary>
public sealed class ExecutionState
{
    /// <summary>
    /// Current execution phase when the state was saved.
    /// </summary>
    public ExecutionPhase Phase { get; set; } = ExecutionPhase.NotStarted;

    /// <summary>
    /// The execution mode that was being used.
    /// </summary>
    public SceneExecutionMode? ExecutionMode { get; set; }

    /// <summary>
    /// Currently active scene name (if in the middle of scene execution).
    /// </summary>
    public string? CurrentSceneName { get; set; }

    /// <summary>
    /// Ordered list of executed scene names.
    /// </summary>
    public List<string> ExecutedSceneOrder { get; set; } = [];

    /// <summary>
    /// Map of scene names to their execution contexts (tools executed).
    /// </summary>
    public Dictionary<string, List<SceneRequestContext>> ExecutedScenes { get; set; } = [];

    /// <summary>
    /// Set of executed tool keys for quick lookup.
    /// </summary>
    public HashSet<string> ExecutedTools { get; set; } = [];

    /// <summary>
    /// Results from executed scenes (for dynamic chaining).
    /// </summary>
    public Dictionary<string, string> SceneResults { get; set; } = [];

    /// <summary>
    /// Accumulated cost from previous execution.
    /// </summary>
    public decimal AccumulatedCost { get; set; }

    /// <summary>
    /// Serializable properties (continuation data, etc.).
    /// </summary>
    public Dictionary<string, string> SerializableProperties { get; set; } = [];

    /// <summary>
    /// Timestamp when the state was saved.
    /// </summary>
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates an ExecutionState from a SceneContext.
    /// </summary>
    public static ExecutionState FromContext(SceneContext context, ExecutionPhase phase, string? currentSceneName = null)
    {
        var state = new ExecutionState
        {
            Phase = phase,
            CurrentSceneName = currentSceneName,
            ExecutedSceneOrder = [.. context.ExecutedSceneOrder],
            ExecutedTools = [.. context.ExecutedTools],
            SceneResults = new Dictionary<string, string>(context.SceneResults, StringComparer.OrdinalIgnoreCase),
            AccumulatedCost = context.TotalCost,
            SavedAt = DateTime.UtcNow
        };

        // Copy executed scenes
        foreach (var (sceneName, tools) in context.ExecutedScenes)
        {
            state.ExecutedScenes[sceneName] = [.. tools];
        }

        // Copy serializable properties (only string keys with serializable values)
        foreach (var (key, value) in context.Properties)
        {
            if (key is string stringKey && value != null)
            {
                try
                {
                    state.SerializableProperties[stringKey] = value.ToString() ?? "";
                }
                catch
                {
                    // Skip non-serializable values
                }
            }
        }

        return state;
    }

    /// <summary>
    /// Applies this execution state to a SceneContext.
    /// </summary>
    public void ApplyToContext(SceneContext context)
    {
        // Restore executed scenes
        context.ExecutedSceneOrder.Clear();
        context.ExecutedSceneOrder.AddRange(ExecutedSceneOrder);

        context.ExecutedTools.Clear();
        foreach (var tool in ExecutedTools)
        {
            context.ExecutedTools.Add(tool);
        }

        context.ExecutedScenes.Clear();
        foreach (var (sceneName, tools) in ExecutedScenes)
        {
            context.ExecutedScenes[sceneName] = [.. tools];
        }

        context.SceneResults.Clear();
        foreach (var (sceneName, result) in SceneResults)
        {
            context.SceneResults[sceneName] = result;
        }

        // Restore accumulated cost
        context.TotalCost = AccumulatedCost;

        // Restore serializable properties
        foreach (var (key, value) in SerializableProperties)
        {
            context.Properties[key] = value;
        }
    }
}

/// <summary>
/// Represents the phase of execution when state was saved.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExecutionPhase
{
    /// <summary>
    /// Execution has not started.
    /// </summary>
    NotStarted,

    /// <summary>
    /// Context was initialized, main actors executed.
    /// </summary>
    Initialized,

    /// <summary>
    /// Scene selection was performed.
    /// </summary>
    SceneSelected,

    /// <summary>
    /// Currently executing a scene.
    /// </summary>
    ExecutingScene,

    /// <summary>
    /// Awaiting client interaction.
    /// </summary>
    AwaitingClient,

    /// <summary>
    /// Scene execution completed.
    /// </summary>
    SceneCompleted,

    /// <summary>
    /// Dynamic chaining in progress.
    /// </summary>
    Chaining,

    /// <summary>
    /// Generating final response.
    /// </summary>
    GeneratingFinalResponse,

    /// <summary>
    /// Execution completed successfully.
    /// </summary>
    Completed
}
