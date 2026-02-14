using Microsoft.Extensions.AI;

namespace Rystem.PlayFramework;

/// <summary>
/// Holds all state for a single request execution.
/// </summary>
public sealed class SceneContext
{
    /// <summary>
    /// Service provider for dependency resolution.
    /// </summary>
    public required IServiceProvider ServiceProvider { get; init; }

    /// <summary>
    /// User's input message.
    /// </summary>
    public required string InputMessage { get; set; }

    /// <summary>
    /// Chat client manager with built-in retry, fallback, and cost calculation.
    /// </summary>
    public required IChatClientManager ChatClientManager { get; set; }

    /// <summary>
    /// All responses generated during execution.
    /// </summary>
    public List<AiSceneResponse> Responses { get; init; } = [];

    /// <summary>
    /// Tracks which scenes and tools have been executed (for loop prevention).
    /// Key: scene name, Value: set of executed tools with arguments.
    /// </summary>
    public Dictionary<string, HashSet<SceneRequestContext>> ExecutedScenes { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Set of executed tool keys (scene.tool.args) for quick lookup.
    /// </summary>
    public HashSet<string> ExecutedTools { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Accumulated results from each scene execution (for dynamic chaining).
    /// Key: scene name, Value: accumulated text response from scene.
    /// </summary>
    public Dictionary<string, string> SceneResults { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Ordered list of scene names as they were executed (for dynamic chaining tracking).
    /// </summary>
    public List<string> ExecutedSceneOrder { get; init; } = [];

    /// <summary>
    /// Conversation summary from previous summarization.
    /// </summary>
    public string? ConversationSummary { get; set; }

    /// <summary>
    /// Context output from main actors (passed to planner).
    /// </summary>
    public List<string> MainActorContext { get; init; } = [];

    /// <summary>
    /// Current execution plan (if planning is enabled).
    /// </summary>
    public ExecutionPlan? ExecutionPlan { get; set; }

    /// <summary>
    /// Total cost accumulated during execution.
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Cache key for storing/retrieving conversation.
    /// </summary>
    public string? CacheKey { get; set; }

    /// <summary>
    /// Cache behavior for this request.
    /// </summary>
    public CacheBehavior CacheBehavior { get; set; } = CacheBehavior.Default;

    /// <summary>
    /// Dynamic properties for extensions.
    /// </summary>
    public Dictionary<object, object> Properties { get; init; } = [];

    /// <summary>
    /// Checks if a tool has been executed with specific arguments.
    /// </summary>
    public bool HasExecutedTool(string sceneName, string toolName, string? args)
    {
        var key = $"{sceneName}.{toolName}.{args ?? "null"}";
        return ExecutedTools.Contains(key);
    }

    /// <summary>
    /// Marks a tool as executed.
    /// </summary>
    public void MarkToolExecuted(string sceneName, string toolName, string? args)
    {
        var key = $"{sceneName}.{toolName}.{args ?? "null"}";
        ExecutedTools.Add(key);

        if (!ExecutedScenes.ContainsKey(sceneName))
        {
            ExecutedScenes[sceneName] = [];
        }

        ExecutedScenes[sceneName].Add(new SceneRequestContext
        {
            ToolName = toolName,
            Arguments = args
        });
    }

    /// <summary>
    /// Adds cost to total and returns updated total.
    /// </summary>
    public decimal AddCost(decimal cost)
    {
        TotalCost += cost;
        return TotalCost;
    }

    /// <summary>
    /// Gets a property value by key.
    /// </summary>
    public T? GetProperty<T>(object key) where T : class
    {
        return Properties.TryGetValue(key, out var value) ? value as T : null;
    }

    /// <summary>
    /// Sets a property value.
    /// </summary>
    public void SetProperty(object key, object value)
    {
        Properties[key] = value;
    }
}
