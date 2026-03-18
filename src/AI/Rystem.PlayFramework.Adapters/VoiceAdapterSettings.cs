using Azure.Identity;

namespace Rystem.PlayFramework.Adapters;

/// <summary>
/// Configuration for the Azure OpenAI voice adapter (STT + TTS).
/// </summary>
public sealed class VoiceAdapterSettings
{
    /// <summary>Azure OpenAI endpoint URI. If null, reuses the endpoint from <see cref="AdapterSettings"/>.</summary>
    public Uri? Endpoint { get; set; }

    /// <summary>API key. If null, reuses the key from <see cref="AdapterSettings"/>.</summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// When true, uses <see cref="DefaultAzureCredential"/> for authentication.
    /// </summary>
    public bool UseAzureCredential { get; set; }

    /// <summary>
    /// Deployment name for the speech-to-text model (e.g., <c>"whisper"</c>).
    /// Required.
    /// </summary>
    public string SttDeployment { get; set; } = "whisper";

    /// <summary>
    /// Deployment name for the text-to-speech model (e.g., <c>"tts-1"</c>, <c>"tts-1-hd"</c>).
    /// Required.
    /// </summary>
    public string TtsDeployment { get; set; } = "tts-1";

    /// <summary>
    /// TTS voice to use (e.g., <c>"alloy"</c>, <c>"echo"</c>, <c>"fable"</c>, <c>"onyx"</c>, <c>"nova"</c>, <c>"shimmer"</c>).
    /// Default: <c>"alloy"</c>.
    /// </summary>
    public string TtsVoice { get; set; } = "alloy";

    /// <summary>
    /// TTS output audio format.
    /// Default: <c>"mp3"</c>. Supported: <c>"mp3"</c>, <c>"opus"</c>, <c>"aac"</c>, <c>"flac"</c>, <c>"wav"</c>, <c>"pcm"</c>.
    /// </summary>
    public string TtsOutputFormat { get; set; } = "mp3";

    /// <summary>
    /// TTS speech speed multiplier (0.25 to 4.0).
    /// Default: 1.0.
    /// </summary>
    public float TtsSpeed { get; set; } = 1.0f;

    /// <summary>
    /// Optional cost tracking settings for STT and TTS operations.
    /// When set, an <see cref="IAudioCostCalculator"/> is registered and used by the voice pipeline
    /// to calculate and report audio costs alongside LLM token costs.
    /// </summary>
    public AudioCostSettings? CostTracking { get; set; }
}
