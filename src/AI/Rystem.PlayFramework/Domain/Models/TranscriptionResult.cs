namespace Rystem.PlayFramework;

/// <summary>
/// Result of a speech-to-text transcription, including the detected language.
/// </summary>
/// <param name="Text">Transcribed text.</param>
/// <param name="DetectedLanguage">
/// ISO 639-1 language code detected by the STT model (e.g., "italian", "english", "it", "en").
/// May be <c>null</c> if the model did not return language information.
/// </param>
/// <param name="DurationSeconds">
/// Duration of the audio in seconds as reported by the STT model (verbose format).
/// Used for accurate STT cost calculation. Null if the model did not return duration info.
/// </param>
public sealed record TranscriptionResult(string Text, string? DetectedLanguage = null, double? DurationSeconds = null);
