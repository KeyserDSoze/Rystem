using Microsoft.Extensions.AI;

namespace Rystem.PlayFramework;

/// <summary>
/// Default summarizer implementation using IChatClient.
/// </summary>
internal sealed class DefaultSummarizer : ISummarizer
{
    private readonly IChatClient _chatClient;
    private readonly PlayFrameworkSettings _settings;

    public DefaultSummarizer(IChatClient chatClient, PlayFrameworkSettings settings)
    {
        _chatClient = chatClient;
        _settings = settings;
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
            return true;
        }

        // Check character count
        var totalChars = responses
            .Where(r => !string.IsNullOrEmpty(r.Message))
            .Sum(r => r.Message!.Length);

        return totalChars >= _settings.Summarization.CharacterThreshold;
    }

    public async Task<string> SummarizeAsync(
        List<AiSceneResponse> responses,
        CancellationToken cancellationToken = default)
    {
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

        return response.Messages?.FirstOrDefault()?.Text ?? string.Empty;
    }
}
