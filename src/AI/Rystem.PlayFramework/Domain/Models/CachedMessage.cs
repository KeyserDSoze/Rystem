using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using Rystem.PlayFramework.Helpers;

namespace Rystem.PlayFramework;

/// <summary>
/// Serializable representation of a TrackedMessage for cache storage.
/// Converts complex ChatMessage contents to a flat, serializable format.
/// </summary>
public sealed class CachedMessage
{
    /// <summary>
    /// Business type flags.
    /// </summary>
    public MessageBusinessType BusinessType { get; set; }

    /// <summary>
    /// Label for debugging.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// The role of the message (User, Assistant, System, Tool).
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Text content of the message (if any).
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Serialized contents for complex messages (function calls, function results, etc.).
    /// </summary>
    public List<CachedContent>? Contents { get; set; }

    /// <summary>
    /// Additional properties from the original message.
    /// </summary>
    public Dictionary<string, object?>? AdditionalProperties { get; set; }

    /// <summary>
    /// Creates a CachedMessage from a TrackedMessage.
    /// </summary>
    public static CachedMessage FromTrackedMessage(TrackedMessage tracked)
    {
        var message = tracked.Message;
        var cached = new CachedMessage
        {
            BusinessType = tracked.BusinessType,
            Label = tracked.Label,
            Role = message.Role.Value,
            Text = message.Text,
            AdditionalProperties = message.AdditionalProperties?.ToDictionary(x => x.Key, x => x.Value)
        };

        // Convert complex contents to serializable format
        if (message.Contents != null && message.Contents.Count > 0)
        {
            cached.Contents = [];
            foreach (var content in message.Contents)
            {
                cached.Contents.Add(CachedContent.FromAIContent(content));
            }
        }

        return cached;
    }

    /// <summary>
    /// Converts back to a TrackedMessage.
    /// </summary>
    public TrackedMessage ToTrackedMessage()
    {
        var role = Role switch
        {
            "user" => ChatRole.User,
            "assistant" => ChatRole.Assistant,
            "system" => ChatRole.System,
            "tool" => ChatRole.Tool,
            _ => new ChatRole(Role)
        };

        ChatMessage message;

        if (Contents != null && Contents.Count > 0)
        {
            // Rebuild contents from cached format
            var aiContents = Contents.Select(c => c.ToAIContent()).ToList();
            message = new ChatMessage(role, aiContents);
        }
        else if (!string.IsNullOrEmpty(Text))
        {
            message = new ChatMessage(role, Text);
        }
        else
        {
            message = new ChatMessage(role, string.Empty);
        }

        // Restore additional properties
        if (AdditionalProperties != null)
        {
            message.AdditionalProperties = new AdditionalPropertiesDictionary(AdditionalProperties);
        }

        return new TrackedMessage
        {
            BusinessType = BusinessType,
            Label = Label,
            Message = message
        };
    }
}

/// <summary>
/// Serializable representation of AIContent.
/// </summary>
public sealed class CachedContent
{
    /// <summary>
    /// Type discriminator: "text", "functionCall", "functionResult", "data", "uri".
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Text content (for TextContent).
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Function/tool name (for FunctionCallContent, FunctionResultContent).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Call ID (for FunctionCallContent, FunctionResultContent).
    /// </summary>
    public string? CallId { get; set; }

    /// <summary>
    /// Serialized arguments (for FunctionCallContent).
    /// </summary>
    public string? ArgumentsJson { get; set; }

    /// <summary>
    /// Serialized result (for FunctionResultContent).
    /// </summary>
    public string? ResultJson { get; set; }

    /// <summary>
    /// Media type (for DataContent).
    /// </summary>
    public string? MediaType { get; set; }

    /// <summary>
    /// Base64 encoded data (for DataContent).
    /// </summary>
    public string? DataBase64 { get; set; }

    /// <summary>
    /// URI (for UriContent).
    /// </summary>
    public string? Uri { get; set; }

    /// <summary>
    /// Creates a CachedContent from an AIContent.
    /// </summary>
    public static CachedContent FromAIContent(AIContent content)
    {
        return content switch
        {
            TextContent text => new CachedContent
            {
                Type = "text",
                Text = text.Text
            },
            FunctionCallContent functionCall => new CachedContent
            {
                Type = "functionCall",
                Name = functionCall.Name,
                CallId = functionCall.CallId,
                ArgumentsJson = functionCall.Arguments != null
                    ? JsonSerializer.Serialize(functionCall.Arguments, JsonHelper.JsonSerializerOptions)
                    : null
            },
            FunctionResultContent functionResult => new CachedContent
            {
                Type = "functionResult",
                CallId = functionResult.CallId,
                ResultJson = functionResult.Result != null
                    ? JsonSerializer.Serialize(functionResult.Result, JsonHelper.JsonSerializerOptions)
                    : null
            },
            DataContent data => new CachedContent
            {
                Type = "data",
                MediaType = data.MediaType,
                DataBase64 = data.Data.Length > 0 ? Convert.ToBase64String(data.Data.ToArray()) : null
            },
            UriContent uri => new CachedContent
            {
                Type = "uri",
                Uri = uri.Uri?.ToString(),
                MediaType = uri.MediaType
            },
            _ => new CachedContent
            {
                Type = "unknown",
                Text = content.ToString()
            }
        };
    }

    /// <summary>
    /// Converts back to an AIContent.
    /// </summary>
    public AIContent ToAIContent()
    {
        return Type switch
        {
            "text" => new TextContent(Text ?? string.Empty),
            "functionCall" => new FunctionCallContent(CallId ?? string.Empty, Name ?? string.Empty)
            {
                Arguments = !string.IsNullOrEmpty(ArgumentsJson)
                    ? JsonSerializer.Deserialize<Dictionary<string, object?>>(ArgumentsJson, JsonHelper.JsonSerializerOptions)
                    : null
            },
            "functionResult" => new FunctionResultContent(CallId ?? string.Empty, Name ?? string.Empty)
            {
                Result = !string.IsNullOrEmpty(ResultJson)
                    ? JsonSerializer.Deserialize<object>(ResultJson, JsonHelper.JsonSerializerOptions)
                    : null
            },
            "data" => new DataContent(
                !string.IsNullOrEmpty(DataBase64) ? Convert.FromBase64String(DataBase64) : [],
                MediaType ?? "application/octet-stream"),
            "uri" => new UriContent(!string.IsNullOrEmpty(Uri) ? new Uri(Uri) : new Uri("about:blank"), MediaType),
            _ => new TextContent(Text ?? string.Empty)
        };
    }
}
