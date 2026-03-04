using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI.Audio;

namespace Rystem.PlayFramework.Adapters.FoundryLocal;

/// <summary>
/// DelegatingChatClient that intercepts audio <see cref="DataContent"/> in chat messages,
/// transcribes them via a local speech-to-text model, and replaces them with <see cref="TextContent"/>.
/// </summary>
public sealed class SpeechToTextChatClient : DelegatingChatClient
{
    private readonly AudioClient _audioClient;
    private readonly ILogger<SpeechToTextChatClient>? _logger;

    public SpeechToTextChatClient(
        IChatClient innerClient,
        AudioClient audioClient,
        ILogger<SpeechToTextChatClient>? logger = null)
        : base(innerClient)
    {
        _audioClient = audioClient;
        _logger = logger;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var processed = await TranscribeAudioInMessagesAsync(messages, cancellationToken);
        return await base.GetResponseAsync(processed, options, cancellationToken);
    }

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var processed = TranscribeAudioInMessagesAsync(messages, cancellationToken)
            .GetAwaiter().GetResult();
        return base.GetStreamingResponseAsync(processed, options, cancellationToken);
    }

    private async Task<List<ChatMessage>> TranscribeAudioInMessagesAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        var result = new List<ChatMessage>();
        foreach (var message in messages)
        {
            if (!message.Contents.OfType<DataContent>().Any(IsAudioContent))
            {
                result.Add(message);
                continue;
            }

            var newContents = new List<AIContent>();
            foreach (var content in message.Contents)
            {
                if (content is DataContent data && IsAudioContent(data))
                {
                    var text = await TranscribeAsync(data, cancellationToken);
                    newContents.Add(new TextContent($"[Transcribed audio] {text}"));
                }
                else
                {
                    newContents.Add(content);
                }
            }

            result.Add(new ChatMessage(message.Role, newContents));
        }
        return result;
    }

    private async Task<string> TranscribeAsync(DataContent data, CancellationToken cancellationToken)
    {
        var bytes = data.Data is { Length: > 0 } d ? d.ToArray() : [];

        var fileName = data.Name;
        if (string.IsNullOrEmpty(fileName)
            && data.AdditionalProperties?.TryGetValue("name", out var n) == true)
        {
            fileName = n?.ToString();
        }
        fileName ??= $"audio_{Guid.NewGuid():N}";
        if (!Path.HasExtension(fileName) && !string.IsNullOrEmpty(data.MediaType))
        {
            fileName += GetAudioExtension(data.MediaType);
        }

        using var stream = new MemoryStream(bytes);
        var result = await _audioClient.TranscribeAudioAsync(stream, fileName);

        _logger?.LogInformation(
            "Transcribed audio {FileName} ({Bytes} bytes) -> {Length} chars",
            fileName, bytes.Length, result.Value.Text.Length);

        return result.Value.Text;
    }

    private static bool IsAudioContent(DataContent data)
    {
        var mediaType = data.MediaType ?? string.Empty;
        return mediaType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetAudioExtension(string mediaType) => mediaType.ToLowerInvariant() switch
    {
        "audio/mpeg" or "audio/mp3" => ".mp3",
        "audio/wav" or "audio/wave" or "audio/x-wav" => ".wav",
        "audio/ogg" => ".ogg",
        "audio/flac" => ".flac",
        "audio/mp4" or "audio/m4a" or "audio/x-m4a" => ".m4a",
        "audio/webm" => ".webm",
        "audio/aac" => ".aac",
        _ => ".wav"
    };
}
