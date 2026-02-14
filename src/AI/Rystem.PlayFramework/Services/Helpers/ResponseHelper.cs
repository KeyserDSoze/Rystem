using Microsoft.Extensions.AI;
using Rystem.PlayFramework;

namespace Rystem.PlayFramework.Services.Helpers;

/// <summary>
/// Implementation of response helper for PlayFramework.
/// Standardizes creation of AiSceneResponse objects.
/// </summary>
internal sealed class ResponseHelper : IResponseHelper
{
    /// <inheritdoc />
    public AiSceneResponse CreateStreamingResponse(
        string? sceneName,
        string streamingChunk,
        string message,
        bool isStreamingComplete,
        decimal? totalCost)
    {
        return new AiSceneResponse
        {
            Status = isStreamingComplete ? AiResponseStatus.Running : AiResponseStatus.Streaming,
            SceneName = sceneName,
            StreamingChunk = streamingChunk,
            Message = message,
            IsStreamingComplete = isStreamingComplete,
            TotalCost = totalCost ?? 0
        };
    }

    /// <inheritdoc />
    public AiSceneResponse CreateFinalResponse(
        string? sceneName,
        string? message,
        SceneContext context,
        int? inputTokens = null,
        int? outputTokens = null,
        int? cachedInputTokens = null,
        decimal? cost = null,
        string? streamingChunk = null,
        bool? isStreamingComplete = null,
        IEnumerable<AIContent>? contents = null)
    {
        var response = new AiSceneResponse
        {
            Status = AiResponseStatus.Running,
            SceneName = sceneName,
            Message = message,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            CachedInputTokens = cachedInputTokens,
            Cost = cost,
            TotalCost = context.AddCost(cost ?? 0),
            Contents = contents
        };

        if (streamingChunk != null)
        {
            response.StreamingChunk = streamingChunk;
        }

        if (isStreamingComplete.HasValue)
        {
            response.IsStreamingComplete = isStreamingComplete.Value;
        }

        // Track in context
        context.Responses.Add(response);

        return response;
    }

    /// <inheritdoc />
    public AiSceneResponse CreateErrorResponse(
        string? sceneName,
        string message,
        string? errorMessage,
        SceneContext context,
        int? inputTokens = null,
        int? outputTokens = null,
        int? cachedInputTokens = null,
        decimal? cost = null,
        IEnumerable<AIContent>? contents = null)
    {
        var response = new AiSceneResponse
        {
            Status = AiResponseStatus.Error,
            SceneName = sceneName,
            Message = message,
            ErrorMessage = errorMessage,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            CachedInputTokens = cachedInputTokens,
            Cost = cost,
            TotalCost = context.AddCost(cost ?? 0),
            Contents = contents
        };

        // Track in context
        context.Responses.Add(response);

        return response;
    }

    /// <inheritdoc />
    public AiSceneResponse CreateBudgetExceededResponse(
        string? sceneName,
        decimal maxBudget,
        decimal totalCost,
        string currency)
    {
        return new AiSceneResponse
        {
            Status = AiResponseStatus.BudgetExceeded,
            SceneName = sceneName,
            Message = $"Budget limit of {maxBudget:F6} {currency} exceeded. Total cost: {totalCost:F6}",
            ErrorMessage = "Maximum budget reached",
            TotalCost = totalCost
        };
    }

    /// <inheritdoc />
    public AiSceneResponse CreateStatusResponse(
        AiResponseStatus status,
        string? message = null,
        decimal? cost = null)
    {
        return new AiSceneResponse
        {
            Status = status,
            Message = message,
            Cost = cost,
            TotalCost = cost ?? 0
        };
    }

    /// <inheritdoc />
    public AiSceneResponse CreateAndTrackResponse(
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
        IEnumerable<AIContent>? contents = null)
    {
        var response = new AiSceneResponse
        {
            Status = status,
            SceneName = sceneName,
            Message = message,
            ErrorMessage = errorMessage,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            CachedInputTokens = cachedInputTokens,
            Cost = cost,
            TotalCost = context.AddCost(cost ?? 0),
            FunctionName = functionName,
            Contents = contents
        };

        // Track in context
        context.Responses.Add(response);

        return response;
    }
}
