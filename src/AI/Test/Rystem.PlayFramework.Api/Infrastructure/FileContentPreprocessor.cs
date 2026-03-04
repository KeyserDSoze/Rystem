using Microsoft.Extensions.AI;
using System.Text;

namespace Rystem.PlayFramework.Api.Infrastructure;

/// <summary>
/// Delegating IChatClient that preprocesses non-image DataContent into TextContent.
/// Azure OpenAI chat completions only support "text" and "image_url" content types,
/// so binary files (PDF, audio, etc.) must be converted to text before reaching the API.
/// </summary>
public sealed class FileContentPreprocessor(IChatClient innerClient) : DelegatingChatClient(innerClient)
{
    private static readonly HashSet<string> s_imageMediaTypes =
    [
        "image/png", "image/jpeg", "image/jpg", "image/gif",
        "image/webp", "image/bmp", "image/tiff", "image/svg+xml"
    ];

    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var processed = PreprocessMessages(messages);
        return base.GetResponseAsync(processed, options, cancellationToken);
    }

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var processed = PreprocessMessages(messages);
        return base.GetStreamingResponseAsync(processed, options, cancellationToken);
    }

    private static List<ChatMessage> PreprocessMessages(IEnumerable<ChatMessage> messages)
    {
        var result = new List<ChatMessage>();
        foreach (var message in messages)
        {
            if (!message.Contents.OfType<DataContent>().Any(IsNonImageData))
            {
                result.Add(message);
                continue;
            }

            // Rebuild contents: convert non-image DataContent → TextContent
            var newContents = new List<AIContent>();
            foreach (var content in message.Contents)
            {
                if (content is DataContent data && IsNonImageData(data))
                {
                    newContents.Add(ConvertToText(data));
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

    private static bool IsNonImageData(DataContent data)
    {
        var mediaType = data.MediaType?.ToLowerInvariant() ?? string.Empty;
        return !mediaType.StartsWith("image/") || !s_imageMediaTypes.Contains(mediaType);
    }

    private static TextContent ConvertToText(DataContent data)
    {
        var mediaType = data.MediaType ?? "unknown";
        var name = data.AdditionalProperties?.TryGetValue("name", out var n) == true ? n?.ToString() : null;

        // Try to decode the bytes as UTF-8 text
        var bytes = data.Data is { Length: > 0 } d ? d.ToArray() : [];
        var text = TryDecodeAsText(bytes, mediaType);

        var header = !string.IsNullOrEmpty(name)
            ? $"[Attached file: {name} ({mediaType})]"
            : $"[Attached file ({mediaType})]";

        return new TextContent($"{header}\n{text}");
    }

    private static string TryDecodeAsText(byte[] bytes, string mediaType)
    {
        if (bytes.Length == 0)
            return "(empty file)";

        // For known text-based types, decode as UTF-8
        if (IsTextBasedMediaType(mediaType))
        {
            return Encoding.UTF8.GetString(bytes);
        }

        // For binary files (PDF, audio, video, etc.), indicate they can't be read directly
        return $"(Binary content, {bytes.Length:N0} bytes — content extraction not supported in this configuration)";
    }

    private static bool IsTextBasedMediaType(string mediaType)
    {
        var lower = mediaType.ToLowerInvariant();
        return lower.StartsWith("text/")
            || lower.Contains("json")
            || lower.Contains("xml")
            || lower.Contains("yaml")
            || lower.Contains("yml")
            || lower.Contains("csv")
            || lower.Contains("markdown")
            || lower.Contains("html")
            || lower.Contains("javascript")
            || lower.Contains("typescript")
            || lower.Contains("plain");
    }
}
