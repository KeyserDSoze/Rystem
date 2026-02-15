using Microsoft.Extensions.AI;

namespace Rystem.PlayFramework;

/// <summary>
/// Concrete DTO for client-side tool content results.
/// Maps directly to the TypeScript AIContent interface: { type, text, data, mediaType }.
/// Used in <see cref="ClientInteractionResult.Contents"/> for JSON deserialization
/// (since <see cref="AIContent"/> is abstract and cannot be deserialized directly).
/// </summary>
public sealed class ClientContentItem
{
    /// <summary>
    /// Content type: "text" or "data".
    /// </summary>
    public string Type { get; set; } = "text";

    /// <summary>
    /// Text content (when Type is "text").
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Base64-encoded binary data (when Type is "data").
    /// </summary>
    public string? Data { get; set; }

    /// <summary>
    /// MIME type (e.g., "image/jpeg", "audio/webm").
    /// </summary>
    public string? MediaType { get; set; }

    /// <summary>
    /// Converts to native <see cref="AIContent"/> for LLM processing.
    /// </summary>
    public AIContent ToAIContent()
    {
        return Type.ToLowerInvariant() switch
        {
            "text" => new TextContent(Text ?? string.Empty),
            "data" => new DataContent(
                Convert.FromBase64String(Data ?? string.Empty),
                MediaType),
            _ => new TextContent(Text ?? Data ?? string.Empty)
        };
    }
}
