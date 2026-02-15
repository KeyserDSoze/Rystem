using System.Text.Json.Serialization;

namespace Rystem.PlayFramework;

/// <summary>
/// Represents the status of an AI scene response.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AiResponseStatus
{
    /// <summary>
    /// Initializing the context.
    /// </summary>
    Initializing,

    /// <summary>
    /// Loading data from cache.
    /// </summary>
    LoadingCache,

    /// <summary>
    /// Executing main actors.
    /// </summary>
    ExecutingMainActors,

    /// <summary>
    /// Creating execution plan.
    /// </summary>
    Planning,

    /// <summary>
    /// Executing a specific scene.
    /// </summary>
    ExecutingScene,

    /// <summary>
    /// AI is requesting to call a function/tool.
    /// </summary>
    FunctionRequest,

    /// <summary>
    /// Tool execution completed successfully.
    /// </summary>
    FunctionCompleted,

    /// <summary>
    /// Tool execution was skipped (already executed).
    /// </summary>
    ToolSkipped,

    /// <summary>
    /// AI is generating streaming response.
    /// </summary>
    Streaming,

    /// <summary>
    /// AI is generating streaming response.
    /// </summary>
    Running,

    /// <summary>
    /// Summarizing conversation history.
    /// </summary>
    Summarizing,

    /// <summary>
    /// Director is orchestrating multi-scene execution.
    /// </summary>
    DirectorDecision,

    /// <summary>
    /// Generating final response.
    /// </summary>
    GeneratingFinalResponse,

    /// <summary>
    /// Saving to cache.
    /// </summary>
    SavingCache,

    /// <summary>
    /// Waiting for client to execute tool and return result.
    /// Server has saved state in cache with continuation token.
    /// </summary>
    AwaitingClient,

    /// <summary>
    /// Execution completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Budget limit exceeded - execution stopped.
    /// </summary>
    BudgetExceeded,

    /// <summary>
    /// Error occurred during execution.
    /// </summary>
    Error
}
