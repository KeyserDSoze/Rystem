using Azure.Identity;

namespace Rystem.PlayFramework.Adapters;

/// <summary>
/// Configuration settings for Azure OpenAI / OpenAI adapter.
/// </summary>
public sealed class AdapterSettings
{
    /// <summary>Azure OpenAI or OpenAI endpoint URI.</summary>
    public Uri? Endpoint { get; set; }

    /// <summary>API key for authentication. Mutually exclusive with <see cref="UseAzureCredential"/>.</summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// When true, uses <see cref="DefaultAzureCredential"/> (Managed Identity / Azure CLI / etc.)
    /// instead of an API key. Requires <c>Azure.Identity</c> package.
    /// </summary>
    public bool UseAzureCredential { get; set; }

    /// <summary>Model deployment name (e.g. "gpt-4o", "gpt-5.2").</summary>
    public string Deployment { get; set; } = "gpt-4o";

    /// <summary>
    /// When true, uses the Responses API (<c>GetResponsesClient</c>) which supports
    /// <c>input_file</c>, <c>input_image</c>, <c>input_audio</c> natively.
    /// When false, uses the Chat Completions API (<c>GetChatClient</c>).
    /// Default: true.
    /// </summary>
    public bool UseResponsesApi { get; set; } = true;

    /// <summary>
    /// When true, wraps the IChatClient with <see cref="MultiModalChatClient"/>
    /// that uploads non-image binary files via the Files API and references them by file_id.
    /// Only effective when <see cref="UseResponsesApi"/> is true.
    /// Default: true.
    /// </summary>
    public bool EnableFileUpload { get; set; } = true;
}
