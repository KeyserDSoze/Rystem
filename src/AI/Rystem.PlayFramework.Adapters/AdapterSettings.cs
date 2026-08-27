using Azure.Identity;

namespace Rystem.PlayFramework.Adapters;

/// <summary>
/// Configuration settings for Azure OpenAI / OpenAI adapter.
/// </summary>
public sealed class AdapterSettings
{
    /// <summary>
    /// Azure OpenAI resource endpoint URI. Both the resource root and an endpoint ending in
    /// <c>/openai/v1</c> are accepted; resource roots are normalized automatically.
    /// </summary>
    public Uri? Endpoint { get; set; }

    /// <summary>API key for authentication. Mutually exclusive with <see cref="UseAzureCredential"/>.</summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// When true, uses <see cref="DefaultAzureCredential"/> (Managed Identity / Azure CLI / etc.)
    /// instead of an API key. Requires <c>Azure.Identity</c> package.
    /// </summary>
    public bool UseAzureCredential { get; set; }

    /// <summary>Azure OpenAI deployment name, sent as the v1 API <c>model</c> value.</summary>
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

    /// <summary>
    /// Determines how audio content in chat messages is handled.
    /// <list type="bullet">
    ///   <item><see cref="AudioMode.None"/>: audio is passed through as-is (default).</item>
    ///   <item><see cref="AudioMode.MultiModal"/>: audio is sent inline as <c>input_audio</c> — requires a model that supports it (e.g., <c>gpt-4o-audio-preview</c>).</item>
    ///   <item><see cref="AudioMode.SpeechToText"/>: audio is transcribed via Whisper and injected as text — requires <see cref="SpeechToTextDeployment"/>.</item>
    /// </list>
    /// </summary>
    public AudioMode AudioMode { get; set; } = AudioMode.None;

    /// <summary>
    /// Azure OpenAI deployment name for the speech-to-text model (e.g., <c>"whisper"</c>).
    /// Required when <see cref="AudioMode"/> is <see cref="AudioMode.SpeechToText"/>.
    /// </summary>
    public string? SpeechToTextDeployment { get; set; }

    /// <summary>
    /// Overrides the Azure OpenAI Audio Transcriptions <c>api-version</c> used for
    /// <see cref="SpeechToTextDeployment"/>. When null (default), the adapter uses a version broad
    /// enough to cover both classic Whisper deployments and newer <c>gpt-4o-transcribe</c> /
    /// <c>gpt-4o-mini-transcribe</c> deployments. Set this only if a specific resource or deployment
    /// requires a different <c>api-version</c>.
    /// </summary>
    public string? SpeechToTextApiVersion { get; set; }

    /// <summary>
    /// Optional per-adapter cost tracking settings. When set, the adapter is wrapped in a
    /// <c>CostTrackingChatClient</c> that adds the calculated cost to each response.
    /// When null, this adapter does not add cost information.
    /// </summary>
    public TokenCostSettings? CostTracking { get; set; }
}
