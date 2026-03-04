namespace Rystem.PlayFramework.Adapters.FoundryLocal;

/// <summary>
/// Configuration for the Foundry Local voice adapter (local STT + TTS models).
/// </summary>
public sealed class VoiceAdapterSettings
{
    /// <summary>
    /// Model alias for the local speech-to-text model (e.g., <c>"whisper"</c>).
    /// Run <c>foundry model list</c> to see available models.
    /// Required.
    /// </summary>
    public string SttModel { get; set; } = "whisper";

    /// <summary>
    /// Model alias for the local text-to-speech model (e.g., <c>"tts"</c>).
    /// Run <c>foundry model list</c> to see available models.
    /// Required.
    /// </summary>
    public string TtsModel { get; set; } = "tts";

    /// <summary>
    /// TTS voice to use (depends on the TTS model).
    /// Default: <c>"alloy"</c>.
    /// </summary>
    public string TtsVoice { get; set; } = "alloy";

    /// <summary>
    /// TTS output audio format.
    /// Default: <c>"mp3"</c>.
    /// </summary>
    public string TtsOutputFormat { get; set; } = "mp3";

    /// <summary>
    /// TTS speech speed multiplier (0.25 to 4.0).
    /// Default: 1.0.
    /// </summary>
    public float TtsSpeed { get; set; } = 1.0f;

    /// <summary>
    /// URL where the Foundry Local web service listens.
    /// Should match the URL used in <see cref="FoundryLocalSettings.WebServiceUrl"/>.
    /// Default: <c>"http://127.0.0.1:5272"</c>.
    /// </summary>
    public string WebServiceUrl { get; set; } = "http://127.0.0.1:5272";

    /// <summary>
    /// Optional callback invoked during model download to report progress (0–100%).
    /// </summary>
    public Action<float>? OnDownloadProgress { get; set; }
}
