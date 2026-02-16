using Microsoft.Extensions.AI;

namespace Rystem.PlayFramework.Api.Models;

/// <summary>
/// Request model for PlayFramework HTTP API.
/// </summary>
public sealed class PlayFrameworkRequest
{
    /// <summary>
    /// User message (text).
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Multi-modal contents (images, audio, files).
    /// Use this for advanced scenarios with multiple content types.
    /// </summary>
    public List<ContentItem>? Contents { get; set; }

    /// <summary>
    /// Request metadata (userId, tenantId, sessionId, etc.).
    /// Used for rate limiting, telemetry, and custom logic.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Request-specific settings (override defaults).
    /// </summary>
    public SceneRequestSettings? Settings { get; set; }

    /// <summary>
    /// Conversation key for resuming execution after client interaction.
    /// When present with ClientInteractionResults, server loads continuation state from cache.
    /// </summary>
    public string? ConversationKey { get; set; }

    /// <summary>
    /// Results from client-side tool executions.
    /// Used to resume execution after AwaitingClient status.
    /// </summary>
    public List<ClientInteractionResult>? ClientInteractionResults { get; set; }
}

/// <summary>
/// Represents a single content item (text, image, audio, file, URI).
/// </summary>
public sealed class ContentItem
{
    /// <summary>
    /// Content type: "text", "image", "audio", "video", "file", "uri"
    /// </summary>
    public string Type { get; set; } = "text";

    /// <summary>
    /// Text content (for type "text").
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Base64-encoded content (for type "image", "audio", "video", "file").
    /// </summary>
    public string? Data { get; set; }

    /// <summary>
    /// Media type / MIME type (e.g., "image/png", "audio/mp3").
    /// </summary>
    public string? MediaType { get; set; }

    /// <summary>
    /// URI content (for type "uri").
    /// </summary>
    public string? Uri { get; set; }

    /// <summary>
    /// Optional name/filename.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Converts to AIContent for PlayFramework execution.
    /// </summary>
    internal AIContent ToAIContent()
    {
        return Type.ToLowerInvariant() switch
        {
            "text" => new TextContent(Text ?? string.Empty),
            "image" or "audio" or "video" or "file" => new DataContent(
                Convert.FromBase64String(Data ?? string.Empty),
                MediaType),
            "uri" => new UriContent(new Uri(Uri ?? "about:blank"), MediaType),
            _ => throw new InvalidOperationException($"Unsupported content type: {Type}")
        };
    }
}
