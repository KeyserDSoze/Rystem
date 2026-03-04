namespace Rystem.PlayFramework;

/// <summary>
/// Settings for the voice pipeline (STT → PlayFramework → TTS).
/// Configured via <see cref="PlayFrameworkBuilder.WithVoice"/>.
/// </summary>
public sealed class VoiceSettings
{
    /// <summary>
    /// Whether the voice pipeline is enabled.
    /// </summary>
    internal bool Enabled { get; set; }

    /// <summary>
    /// Characters that act as sentence delimiters for TTS chunking.
    /// When streaming tokens are accumulated, a TTS chunk is sent
    /// each time a delimiter is encountered.
    /// Default: <c>".!?\n"</c>
    /// </summary>
    public string SentenceDelimiters { get; set; } = ".!?\n";

    /// <summary>
    /// Minimum number of characters accumulated before a sentence
    /// is sent to TTS, even if a delimiter is found.
    /// Prevents TTS of very short fragments like "OK." or "Sì.".
    /// Default: 20
    /// </summary>
    public int MinCharsBeforeTts { get; set; } = 20;

    /// <summary>
    /// Maximum number of characters to accumulate before forcing a TTS chunk,
    /// even if no delimiter has been found. Prevents unbounded buffering.
    /// Default: 500
    /// </summary>
    public int MaxCharsBeforeTts { get; set; } = 500;

    /// <summary>
    /// Instruction template appended to the transcribed message to tell the LLM
    /// which language to respond in. Use <c>{language}</c> as a placeholder for
    /// the language detected by the STT model (e.g., "italian", "english").
    /// Set to <c>null</c> or empty to disable.
    /// Default: <c>"IMPORTANT: You must respond in {language}."</c>
    /// </summary>
    public string? LanguageInstruction { get; set; } =
        "IMPORTANT: You must respond in {language}.";
}
