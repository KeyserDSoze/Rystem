using Microsoft.Extensions.AI;
using Rystem.PlayFramework;

namespace Rystem.PlayFramework.Services.Helpers;

/// <summary>
/// Helper service for creating standardized AiSceneResponse objects.
/// Eliminates code duplication in response creation.
/// </summary>
internal interface IResponseHelper
{
    /// <summary>
    /// Creates a streaming response (intermediate chunk).
    /// </summary>
    AiSceneResponse CreateStreamingResponse(
        string? sceneName,
        string streamingChunk,
        string message,
        bool isStreamingComplete,
        decimal? totalCost);

    /// <summary>
    /// Creates a final response with costs and token tracking.
    /// </summary>
    AiSceneResponse CreateFinalResponse(
        string? sceneName,
        string? message,
        SceneContext context,
        int? inputTokens = null,
        int? outputTokens = null,
        int? cachedInputTokens = null,
        decimal? cost = null,
        string? streamingChunk = null,
        bool? isStreamingComplete = null,
        IEnumerable<AIContent>? contents = null);

    /// <summary>
    /// Creates an error response with tracking.
    /// </summary>
    AiSceneResponse CreateErrorResponse(
        string? sceneName,
        string message,
        string? errorMessage,
        SceneContext context,
        int? inputTokens = null,
        int? outputTokens = null,
        int? cachedInputTokens = null,
        decimal? cost = null,
        IEnumerable<AIContent>? contents = null);

    /// <summary>
    /// Creates a budget exceeded response.
    /// </summary>
    AiSceneResponse CreateBudgetExceededResponse(
        string? sceneName,
        decimal maxBudget,
        decimal totalCost,
        string currency);

    /// <summary>
    /// Creates a status response (no tracking, no costs).
    /// </summary>
    AiSceneResponse CreateStatusResponse(
        AiResponseStatus status,
        string? message = null,
        decimal? cost = null);

    /// <summary>
    /// Creates a response and tracks it in context.
    /// </summary>
    AiSceneResponse CreateAndTrackResponse(
        SceneContext context,
        AiResponseStatus status,
        string? sceneName = null,
        string? message = null,
        string? errorMessage = null,
        int? inputTokens = null,
        int? outputTokens = null,
        int? cachedInputTokens = null,
        decimal? cost = null,
        string? functionName = null,
        IEnumerable<AIContent>? contents = null);
}
