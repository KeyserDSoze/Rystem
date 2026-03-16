using Microsoft.Extensions.AI;
using Rystem.PlayFramework.Configuration;

namespace Rystem.PlayFramework.Services.Helpers;

/// <summary>
/// Centralizes all tool/function call management:
/// - Deduplication of tool calls (OpenAI bug)
/// - Execution of server-side tools
/// - Detection and request creation for client-side tools
/// - Conversation sanitization for LLM API compliance
/// 
/// Works directly on SceneContext to maintain conversation state.
/// </summary>
public interface IToolExecutionManager
{
    /// <summary>
    /// Process and execute tool calls from LLM response.
    /// Handles both server-side and client-side tools.
    /// - Deduplicates tool calls
    /// - Executes server tools and adds results to context
    /// - Creates ClientInteractionRequest for client tools
    /// - Saves pending tools when awaiting client
    /// </summary>
    /// <param name="context">Scene context with conversation history</param>
    /// <param name="functionCalls">Tool calls from LLM response (already deduplicated)</param>
    /// <param name="sceneTools">Available scene tools</param>
    /// <param name="mcpTools">Available MCP tools</param>
    /// <param name="clientInteractionDefinitions">Client tool definitions</param>
    /// <param name="sceneName">Current scene name for logging</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of responses (status updates, client requests, etc.)</returns>
    IAsyncEnumerable<ToolExecutionResult> ExecuteToolCallsAsync(
        SceneContext context,
        List<FunctionCallContent> functionCalls,
        List<ISceneTool> sceneTools,
        List<AIFunction> mcpTools,
        IReadOnlyList<ClientInteractionDefinition>? clientInteractionDefinitions,
        string sceneName,
        IJsonService? jsonService = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resume execution after receiving client interaction result.
    /// - Injects client result into conversation
    /// - Executes pending server tools saved before yield break
    /// </summary>
    /// <param name="context">Scene context</param>
    /// <param name="clientResults">Results from client interactions</param>
    /// <param name="sceneTools">Available scene tools</param>
    /// <param name="mcpTools">Available MCP tools</param>
    /// <param name="sceneName">Current scene name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of responses from executing pending tools</returns>
    IAsyncEnumerable<ToolExecutionResult> ResumeAfterClientResponseAsync(
        SceneContext context,
        List<ClientInteractionResult> clientResults,
        List<ISceneTool> sceneTools,
        List<AIFunction> mcpTools,
        string sceneName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves all pending client interactions from a ClientInteractionBatch.
    /// Unified method that handles Commands (all FeedbackModes) and ClientTools.
    /// - Maps client results to original OpenAI CallIds
    /// - Auto-completes Commands with OnError when no client result provided
    /// - Reports errors for Commands with Always or ClientTools when no result provided
    /// - Adds FunctionResultContent to conversation history with correct CallId
    /// </summary>
    /// <param name="context">Scene context containing the batch</param>
    /// <param name="clientResults">Results from client (may be null for auto-complete scenarios)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of error/status results</returns>
    IAsyncEnumerable<ToolExecutionResult> ResolveClientInteractionsAsync(
        SceneContext context,
        List<ClientInteractionResult>? clientResults,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the client interaction batch JSON from the distributed cache for the given conversation key.
    /// Returns null if no batch exists or cache is not available.
    /// </summary>
    /// <param name="conversationKey">The conversation key to look up</param>
    /// <returns>Batch JSON string, or null if not found</returns>
    Task<string?> LoadBatchFromCacheAsync(string? conversationKey);

    /// <summary>
    /// Deduplicates tool calls in a ChatResponse (for non-streaming).
    /// OpenAI sometimes returns duplicate tool_calls with different CallIds.
    /// </summary>
    /// <param name="response">ChatResponse from LLM</param>
    /// <returns>Response with deduplicated tool calls</returns>
    ChatResponse DeduplicateToolCalls(ChatResponse response);

    /// <summary>
    /// Deduplicates accumulated tool calls (for streaming).
    /// </summary>
    /// <param name="toolCalls">Accumulated function calls from stream</param>
    /// <returns>Deduplicated list</returns>
    List<FunctionCallContent> DeduplicateToolCalls(List<FunctionCallContent> toolCalls);
}

/// <summary>
/// Result of tool execution - can be status update, error, or client request.
/// </summary>
public sealed class ToolExecutionResult
{
    /// <summary>
    /// Status of this execution step.
    /// </summary>
    public ToolExecutionStatus Status { get; init; }

    /// <summary>
    /// Human-readable message about this step.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Tool name being executed (if applicable).
    /// </summary>
    public string? ToolName { get; init; }

    /// <summary>
    /// Tool result (for completed server tools).
    /// </summary>
    public object? ToolResult { get; init; }

    /// <summary>
    /// Client interaction request (when Status is AwaitingClient).
    /// </summary>
    public ClientInteractionRequest? ClientRequest { get; init; }

    /// <summary>
    /// Error message (when Status is Error).
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Status of tool execution step.
/// </summary>
public enum ToolExecutionStatus
{
    /// <summary>Tool execution started</summary>
    Started,

    /// <summary>Server tool completed successfully</summary>
    Completed,

    /// <summary>Awaiting client to execute client-side tool (requires response)</summary>
    AwaitingClient,

    /// <summary>Client-side command execution (fire-and-forget, optional response)</summary>
    CommandClient,

    /// <summary>Tool execution failed</summary>
    Error
}
