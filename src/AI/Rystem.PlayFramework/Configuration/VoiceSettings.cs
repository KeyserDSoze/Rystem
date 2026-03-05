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

    /// <summary>
    /// System instruction injected when voice mode is active to make the LLM
    /// respond in a conversational, speech-friendly style.
    /// Set to <c>null</c> or empty to disable.
    /// </summary>
    public string? VoiceStyleInstruction { get; set; } =
        "VOICE MODE ACTIVE: The user is interacting via voice (speech-to-text input, text-to-speech output). You MUST follow these rules:\n" +
        "1. Respond in a natural, conversational tone as if you were speaking aloud. Use short, clear sentences.\n" +
        "2. NEVER use tables, bullet lists, numbered lists, or any visual formatting (bold, italic, headers, code blocks, markdown).\n" +
        "3. NEVER use special characters, asterisks, or symbols that are not part of natural speech.\n" +
        "4. When presenting multiple items, use natural language connectors (first, then, also, finally).\n" +
        "5. Spell out numbers when they are small (three instead of 3), but use digits for large or precise numbers.\n" +
        "6. Spell out abbreviations and acronyms the first time you use them.\n" +
        "7. Keep responses concise - aim for spoken delivery under 30 seconds unless the topic demands more detail.\n" +
        "8. If the user explicitly asks for a table, list, or structured format, you MAY provide it, but prefer a conversational summary first.\n" +
        "9. Avoid parenthetical asides and footnotes - they sound unnatural when read aloud.\n" +
        "10. Use discourse markers to guide the listener (So, In other words, The key point is).";
}
