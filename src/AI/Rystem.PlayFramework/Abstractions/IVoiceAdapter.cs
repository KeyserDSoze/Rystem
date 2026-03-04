namespace Rystem.PlayFramework;

/// <summary>
/// Abstraction for voice operations: speech-to-text (STT) and text-to-speech (TTS).
/// Implement this interface in adapter packages (e.g., Rystem.PlayFramework.Adapters).
/// Register via <c>services.AddFactory&lt;IVoiceAdapter&gt;(...)</c>.
/// </summary>
public interface IVoiceAdapter
{
    /// <summary>
    /// Transcribes audio data to text using a speech-to-text model.
    /// </summary>
    /// <param name="audioData">Raw audio bytes (MP3, WAV, OGG, FLAC, M4A, WEBM).</param>
    /// <param name="fileName">
    /// Optional file name with extension (e.g., "recording.mp3").
    /// The extension helps the STT model determine the audio format.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transcribed text.</returns>
    Task<string> TranscribeAsync(
        ReadOnlyMemory<byte> audioData,
        string? fileName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates speech audio from text using a text-to-speech model.
    /// </summary>
    /// <param name="text">Text to convert to speech.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Audio bytes in the configured output format.</returns>
    Task<ReadOnlyMemory<byte>> SynthesizeAsync(
        string text,
        CancellationToken cancellationToken = default);
}
