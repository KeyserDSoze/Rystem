using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Rystem.PlayFramework;
using System.Runtime.CompilerServices;

namespace Rystem.PlayFramework.Services.Helpers;

/// <summary>
/// Implementation of streaming helper for PlayFramework.
/// Handles optimistic streaming with progressive function call detection.
/// </summary>
internal sealed class StreamingHelper : IStreamingHelper
{
    private readonly ILogger<StreamingHelper> _logger;
    private readonly IResponseHelper _responseHelper;

    public StreamingHelper(ILogger<StreamingHelper> logger, IResponseHelper responseHelper)
    {
        _logger = logger;
        _responseHelper = responseHelper;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<StreamingResult> ProcessOptimisticStreamAsync(
        SceneContext context,
        List<ChatMessage> conversationMessages,
        ChatOptions chatOptions,
        string? sceneName,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var accumulatedText = new System.Text.StringBuilder();
        var accumulatedFunctionCalls = new List<FunctionCallContent>();
        var hasDetectedFunctionCall = false;
        var streamedToUser = false;
        decimal? totalCost = null;
        int? totalInputTokens = null;
        int? totalOutputTokens = null;
        int? totalCachedInputTokens = null;

        await foreach (var streamUpdateWithCost in context.ChatClientManager.GetStreamingResponseAsync(
            conversationMessages,
            chatOptions,
            cancellationToken))
        {
            var chunk = streamUpdateWithCost.Update;

            // Detect function calls in this chunk
            var chunkFunctionCalls = chunk.Contents?
                .OfType<FunctionCallContent>()
                .ToList() ?? [];

            if (chunkFunctionCalls.Any())
            {
                // Function call detected! Stop visible streaming
                hasDetectedFunctionCall = true;
                accumulatedFunctionCalls.AddRange(chunkFunctionCalls);

                _logger.LogDebug("Function call detected during streaming in scene {SceneName}, switching to silent accumulation",
                    sceneName);
            }

            // Accumulate text content
            if (chunk.Text != null)
            {
                accumulatedText.Append(chunk.Text);
            }

            // Stream to user ONLY if no function calls detected yet
            if (!hasDetectedFunctionCall && chunk.Text != null)
            {
                streamedToUser = true;
                yield return new StreamingResult
                {
                    AccumulatedText = accumulatedText.ToString(),
                    FunctionCalls = [],
                    StreamedToUser = true,
                    TotalCost = context.TotalCost,
                    TotalInputTokens = totalInputTokens,
                    TotalOutputTokens = totalOutputTokens,
                    TotalCachedInputTokens = totalCachedInputTokens
                };
            }

            // Track usage and costs from ChatUpdateWithCost
            if (streamUpdateWithCost.InputTokens > 0)
            {
                totalInputTokens = (totalInputTokens ?? 0) + streamUpdateWithCost.InputTokens;
            }
            if (streamUpdateWithCost.OutputTokens > 0)
            {
                totalOutputTokens = (totalOutputTokens ?? 0) + streamUpdateWithCost.OutputTokens;
            }
            if (streamUpdateWithCost.CachedInputTokens > 0)
            {
                totalCachedInputTokens = (totalCachedInputTokens ?? 0) + streamUpdateWithCost.CachedInputTokens;
            }

            if (streamUpdateWithCost.EstimatedCost > 0)
            {
                totalCost = (totalCost ?? 0) + streamUpdateWithCost.EstimatedCost;
            }

            // Check if streaming is complete
            if (chunk.FinishReason != null)
            {
                // Build final message for conversation history
                var contents = new List<AIContent>();
                if (!string.IsNullOrEmpty(accumulatedText.ToString()))
                {
                    contents.Add(new TextContent(accumulatedText.ToString()));
                }
                contents.AddRange(accumulatedFunctionCalls);

                yield return new StreamingResult
                {
                    FinalMessage = new ChatMessage(ChatRole.Assistant, contents),
                    AccumulatedText = accumulatedText.ToString(),
                    FunctionCalls = accumulatedFunctionCalls,
                    StreamedToUser = streamedToUser,
                    TotalCost = totalCost,
                    TotalInputTokens = totalInputTokens,
                    TotalOutputTokens = totalOutputTokens,
                    TotalCachedInputTokens = totalCachedInputTokens
                };
                yield break;
            }
        }

        // Handle case where streaming completed without final message
        var finalContents = new List<AIContent>();
        if (!string.IsNullOrEmpty(accumulatedText.ToString()))
        {
            finalContents.Add(new TextContent(accumulatedText.ToString()));
        }
        finalContents.AddRange(accumulatedFunctionCalls);

        yield return new StreamingResult
        {
            FinalMessage = new ChatMessage(ChatRole.Assistant, finalContents),
            AccumulatedText = accumulatedText.ToString(),
            FunctionCalls = accumulatedFunctionCalls,
            StreamedToUser = streamedToUser,
            TotalCost = totalCost,
            TotalInputTokens = totalInputTokens,
            TotalOutputTokens = totalOutputTokens,
            TotalCachedInputTokens = totalCachedInputTokens
        };
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<AiSceneResponse> ProcessChunkAsync(
        ChatResponseUpdate streamChunk,
        string? sceneName,
        SceneContext context)
    {
        // Accumulate complete message (stored in context for tracking)
        var contextKey = $"streaming_message_{sceneName ?? "final"}";
        if (!context.Properties.TryGetValue(contextKey, out var accumulatedObj))
        {
            accumulatedObj = new System.Text.StringBuilder();
            context.Properties[contextKey] = accumulatedObj;
        }
        var accumulated = (System.Text.StringBuilder)accumulatedObj;

        // Get the text from this chunk
        var chunkText = streamChunk.Text ?? string.Empty;
        accumulated.Append(chunkText);

        // Check if this is the final chunk (has completion reason)
        var isComplete = streamChunk.FinishReason != null;

        var streamResponse = _responseHelper.CreateStreamingResponse(
            sceneName: sceneName,
            streamingChunk: chunkText,
            message: accumulated.ToString(),
            isStreamingComplete: isComplete,
            totalCost: context.TotalCost);

        if (isComplete)
        {
            // Clean up accumulated text
            context.Properties.Remove(contextKey);
        }

        yield return streamResponse;
        await Task.CompletedTask; // Satisfy async requirement
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<AiSceneResponse> SimulateStreamAsync(
        ChatMessage responseMessage,
        ChatResponseWithCost responseWithCost,
        string? sceneName,
        SceneContext context,
        SceneRequestSettings settings,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var text = responseMessage.Text ?? string.Empty;
        var accumulatedText = new System.Text.StringBuilder();

        // Simulate streaming by splitting into words
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < words.Length; i++)
        {
            var word = i == 0 ? words[i] : $" {words[i]}";
            accumulatedText.Append(word);

            var isLastChunk = i == words.Length - 1;

            if (isLastChunk)
            {
                // Final chunk with costs
                var finalResponse = _responseHelper.CreateFinalResponse(
                    sceneName: sceneName,
                    message: accumulatedText.ToString(),
                    context: context,
                    inputTokens: responseWithCost.InputTokens,
                    outputTokens: responseWithCost.OutputTokens,
                    cachedInputTokens: responseWithCost.CachedInputTokens,
                    cost: responseWithCost.CalculatedCost,
                    streamingChunk: string.Empty,
                    isStreamingComplete: true);

                yield return finalResponse;

                // Check budget exceeded
                if (settings.MaxBudget.HasValue && context.TotalCost > settings.MaxBudget.Value)
                {
                    yield return _responseHelper.CreateBudgetExceededResponse(
                        sceneName: sceneName,
                        maxBudget: settings.MaxBudget.Value,
                        totalCost: context.TotalCost,
                        currency: context.ChatClientManager.Currency);
                }
            }
            else
            {
                // Intermediate chunk
                var streamResponse = _responseHelper.CreateStreamingResponse(
                    sceneName: sceneName,
                    streamingChunk: word,
                    message: accumulatedText.ToString(),
                    isStreamingComplete: false,
                    totalCost: context.TotalCost);

                yield return streamResponse;
            }

            // Small delay to simulate streaming
            await Task.Delay(5, cancellationToken);
        }
    }
}
