using System.Text.Json.Serialization;

namespace Rystem.PlayFramework;

/// <summary>
/// Represents a batch of pending client interactions saved to context for resumption.
/// Replaces the scattered _pending_commands, _continuation_sceneName, _continuation_callId, etc.
/// </summary>
public sealed class ClientInteractionBatch
{
    /// <summary>
    /// Scene name where the client interactions were triggered.
    /// Used to route back to the correct scene on resume.
    /// </summary>
    public required string SceneName { get; init; }

    /// <summary>
    /// All pending client interactions in this batch.
    /// Each one maps InteractionId (server-generated GUID) to CallId (OpenAI original).
    /// </summary>
    public required List<PendingClientInteraction> Interactions { get; init; }
}

/// <summary>
/// A single pending client interaction within a batch.
/// Maintains the mapping between server-generated InteractionId and OpenAI's CallId.
/// </summary>
public sealed class PendingClientInteraction
{
    /// <summary>
    /// Server-generated unique ID for client tracking (the one sent to the client).
    /// </summary>
    public required string InteractionId { get; init; }

    /// <summary>
    /// OpenAI's original call ID from FunctionCallContent.
    /// Required for building correct FunctionResultContent on resume.
    /// </summary>
    public required string CallId { get; init; }

    /// <summary>
    /// Original tool name from FunctionCallContent (OpenAI format).
    /// Required for building correct FunctionResultContent on resume.
    /// </summary>
    public required string ToolName { get; init; }

    /// <summary>
    /// Whether this is a Command (fire-and-forget) or a ClientTool (requires response).
    /// </summary>
    public required bool IsCommand { get; init; }

    /// <summary>
    /// Feedback mode for Commands. Determines auto-complete behavior on resume.
    /// </summary>
    public required CommandFeedbackMode FeedbackMode { get; init; }
}
