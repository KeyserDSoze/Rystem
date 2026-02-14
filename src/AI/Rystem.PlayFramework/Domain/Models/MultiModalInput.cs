using Microsoft.Extensions.AI;

namespace Rystem.PlayFramework;

/// <summary>
/// Multi-modal input supporting text, images, audio, and files.
/// Uses Microsoft.Extensions.AI native types (DataContent, UriContent).
/// </summary>
public sealed class MultiModalInput
{
    /// <summary>
    /// Text message (may be null for content-only requests).
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    /// Multi-modal contents (images, audio, files as DataContent/UriContent).
    /// </summary>
    public List<AIContent> Contents { get; init; } = [];

    /// <summary>
    /// Creates input with text only.
    /// </summary>
    public static MultiModalInput FromText(string text) => new() { Text = text };

    /// <summary>
    /// Creates input with image from URL.
    /// </summary>
    /// <param name="text">Text prompt/question.</param>
    /// <param name="imageUrl">Image URL.</param>
    /// <param name="mimeType">MIME type (default: image/png).</param>
    public static MultiModalInput FromImageUrl(string text, string imageUrl, string mimeType = "image/png")
        => new()
        {
            Text = text,
            Contents = [new UriContent(new Uri(imageUrl), mimeType)]
        };

    /// <summary>
    /// Creates input with image from bytes.
    /// </summary>
    /// <param name="text">Text prompt/question.</param>
    /// <param name="imageBytes">Image data.</param>
    /// <param name="mimeType">MIME type (default: image/png).</param>
    public static MultiModalInput FromImageBytes(string text, byte[] imageBytes, string mimeType = "image/png")
        => new()
        {
            Text = text,
            Contents = [new DataContent(imageBytes, mimeType)]
        };

    /// <summary>
    /// Creates input with audio from URL.
    /// </summary>
    /// <param name="text">Text prompt/question.</param>
    /// <param name="audioUrl">Audio URL.</param>
    /// <param name="mimeType">MIME type (default: audio/mp3).</param>
    public static MultiModalInput FromAudioUrl(string text, string audioUrl, string mimeType = "audio/mp3")
        => new()
        {
            Text = text,
            Contents = [new UriContent(new Uri(audioUrl), mimeType)]
        };

    /// <summary>
    /// Creates input with audio from bytes.
    /// </summary>
    /// <param name="text">Text prompt/question.</param>
    /// <param name="audioBytes">Audio data.</param>
    /// <param name="mimeType">MIME type (default: audio/mp3).</param>
    public static MultiModalInput FromAudioBytes(string text, byte[] audioBytes, string mimeType = "audio/mp3")
        => new()
        {
            Text = text,
            Contents = [new DataContent(audioBytes, mimeType)]
        };

    /// <summary>
    /// Creates input with file from URL.
    /// </summary>
    /// <param name="text">Text prompt/question.</param>
    /// <param name="fileUrl">File URL.</param>
    /// <param name="mimeType">MIME type (default: application/pdf).</param>
    public static MultiModalInput FromFileUrl(string text, string fileUrl, string mimeType = "application/pdf")
        => new()
        {
            Text = text,
            Contents = [new UriContent(new Uri(fileUrl), mimeType)]
        };

    /// <summary>
    /// Creates input with file from bytes.
    /// </summary>
    /// <param name="text">Text prompt/question.</param>
    /// <param name="fileBytes">File data.</param>
    /// <param name="mimeType">MIME type (default: application/pdf).</param>
    public static MultiModalInput FromFileBytes(string text, byte[] fileBytes, string mimeType = "application/pdf")
        => new()
        {
            Text = text,
            Contents = [new DataContent(fileBytes, mimeType)]
        };

    /// <summary>
    /// Converts to ChatMessage with text + contents.
    /// </summary>
    /// <param name="role">Chat role.</param>
    public ChatMessage ToChatMessage(ChatRole? role = null)
    {
        var contents = new List<AIContent>();

        // Add text if present
        if (!string.IsNullOrWhiteSpace(Text))
        {
            contents.Add(new TextContent(Text));
        }

        // Add multi-modal contents
        contents.AddRange(Contents);

        return new ChatMessage(role ?? ChatRole.User, contents);
    }
}
