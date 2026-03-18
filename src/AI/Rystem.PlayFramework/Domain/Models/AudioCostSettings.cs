namespace Rystem.PlayFramework;

/// <summary>
/// Pricing configuration for STT (speech-to-text) and TTS (text-to-speech) operations.
/// </summary>
public sealed class AudioCostSettings
{
    /// <summary>Enable cost tracking. Default: true.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// STT cost per minute of audio (e.g., 0.006 for Azure Whisper at $0.006/min).
    /// </summary>
    public decimal SttCostPerMinute { get; set; }

    /// <summary>
    /// TTS cost per 1000 characters of input text (e.g., 0.015 for Azure TTS-1 at $0.015/1K chars).
    /// </summary>
    public decimal TtsCostPerThousandChars { get; set; }
}
