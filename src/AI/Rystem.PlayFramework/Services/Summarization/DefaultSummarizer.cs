using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Rystem.PlayFramework.Telemetry;
using System.Diagnostics;

namespace Rystem.PlayFramework;

/// <summary>
/// Default summarizer implementation using IChatClient.
/// </summary>
internal sealed class DefaultSummarizer : ISummarizer
{
    private readonly IChatClient _chatClient;
    private readonly PlayFrameworkSettings _settings;
    private readonly ILogger<DefaultSummarizer> _logger;

    public DefaultSummarizer(IChatClient chatClient, PlayFrameworkSettings settings, ILogger<DefaultSummarizer> logger)
    {
        _chatClient = chatClient;
        _settings = settings;
        _logger = logger;
    }

    public bool ShouldSummarize(List<AiSceneResponse> responses)
    {
        if (!_settings.Summarization.Enabled)
        {
            return false;
        }

        // Check response count
        if (responses.Count >= _settings.Summarization.ResponseCountThreshold)
        {
            _logger.LogDebug("Summarization triggered by response count ({Count} >= {Threshold})",
                responses.Count, _settings.Summarization.ResponseCountThreshold);
            return true;
        }

        // Check character count
        var totalChars = responses
            .Where(r => !string.IsNullOrEmpty(r.Message))
            .Sum(r => r.Message!.Length);

        if (totalChars >= _settings.Summarization.CharacterThreshold)
        {
            _logger.LogDebug("Summarization triggered by character count ({Chars} >= {Threshold})",
                totalChars, _settings.Summarization.CharacterThreshold);
            return true;
        }

        return false;
    }

    public async Task<string> SummarizeAsync(
        List<AiSceneResponse> responses,
        CancellationToken cancellationToken = default)
    {
        using var activity = PlayFrameworkActivitySource.Instance.StartActivity(
            PlayFrameworkActivitySource.Activities.SummarizationSummarize, ActivityKind.Internal);
        activity?.AddEvent(new ActivityEvent(PlayFrameworkActivitySource.Events.SummarizationStarted));

        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting summarization of {Count} responses", responses.Count);

        // Build conversation history (including multi-modal content info)
        var conversationText = string.Join("\n\n", responses
            .Where(r => !string.IsNullOrEmpty(r.Message))
            .Select(r =>
            {
                var text = $"[{r.Status}] {r.Message}";
                var multiModalCount = r.Contents?.Count(c => c is DataContent or UriContent) ?? 0;
                if (multiModalCount > 0)
                {
                    text += $" [+{multiModalCount} multi-modal content(s)]";
                }
                return text;
            }));

        // Create summarization prompt
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are a conversation summarizer. Summarize the following conversation history, preserving all important information, data, and context."),
            new(ChatRole.User, $"Summarize this conversation:\n\n{conversationText}")
        };

        // Get summary
        var response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
        var summary = response.Messages?.FirstOrDefault()?.Text ?? string.Empty;

        var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
        _logger.LogInformation("Summarization completed in {Duration:F1}ms. Summary length: {Length} chars",
            duration, summary.Length);

        activity?.SetTag(PlayFrameworkActivitySource.Tags.SummaryLength, summary.Length);
        activity?.AddEvent(new ActivityEvent(PlayFrameworkActivitySource.Events.SummarizationCompleted));
        activity?.SetStatus(ActivityStatusCode.Ok);

        return summary;
    }
}
