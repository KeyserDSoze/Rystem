using Microsoft.AI.Foundry.Local;

namespace Rystem.PlayFramework.Adapters.FoundryLocal;

/// <summary>
/// Configuration settings for Foundry Local adapter.
/// Foundry Local runs AI models locally on your device for development and testing.
/// <para>
/// Install: <c>winget install Microsoft.FoundryLocal</c><br/>
/// List models: <c>foundry model list</c>
/// </para>
/// </summary>
public sealed class FoundryLocalSettings
{
    /// <summary>
    /// Model alias (e.g., "phi-4-mini", "qwen2.5-0.5b", "gpt-oss-20b").
    /// Run <c>foundry model list</c> to see available models for your hardware.
    /// Foundry Local automatically selects the best variant for your hardware.
    /// </summary>
    public string Model { get; set; } = "phi-4-mini";

    /// <summary>
    /// Application name used by Foundry Local for identification.
    /// Default: <c>"Rystem.PlayFramework"</c>.
    /// </summary>
    public string AppName { get; set; } = "Rystem.PlayFramework";

    /// <summary>
    /// URL where the Foundry Local web service listens.
    /// Default: <c>"http://127.0.0.1:5272"</c>.
    /// The OpenAI-compatible endpoint will be at <c>{WebServiceUrl}/v1</c>.
    /// </summary>
    public string WebServiceUrl { get; set; } = "http://127.0.0.1:5272";

    /// <summary>
    /// Foundry Local SDK log level.
    /// Default: <see cref="Microsoft.AI.Foundry.Local.LogLevel.Information"/>.
    /// </summary>
    public Microsoft.AI.Foundry.Local.LogLevel FoundryLogLevel { get; set; } = Microsoft.AI.Foundry.Local.LogLevel.Information;

    /// <summary>
    /// Optional callback invoked during model download to report progress (0–100%).
    /// If null, download is silent.
    /// </summary>
    public Action<float>? OnDownloadProgress { get; set; }

    /// <summary>
    /// Determines how audio content in chat messages is handled.
    /// <list type="bullet">
    ///   <item><see cref="AudioMode.None"/>: audio is passed through as-is (default).</item>
    ///   <item><see cref="AudioMode.MultiModal"/>: audio sent inline — requires a model with native audio support.</item>
    ///   <item><see cref="AudioMode.SpeechToText"/>: audio is transcribed locally and injected as text — requires <see cref="SpeechToTextModel"/>.</item>
    /// </list>
    /// </summary>
    public AudioMode AudioMode { get; set; } = AudioMode.None;

    /// <summary>
    /// Model alias for the local speech-to-text model.
    /// Required when <see cref="AudioMode"/> is <see cref="AudioMode.SpeechToText"/>.
    /// The model will be downloaded and loaded automatically alongside the chat model.
    /// </summary>
    public string? SpeechToTextModel { get; set; }
}
