namespace Rystem.PlayFramework.Adapters;

/// <summary>
/// Determines how audio content is handled in chat messages.
/// </summary>
public enum AudioMode
{
    /// <summary>
    /// Audio content is not processed — passed through as-is to the model.
    /// </summary>
    None,

    /// <summary>
    /// Audio is sent inline as <c>input_audio</c> for models that natively support audio input
    /// (e.g., <c>gpt-4o-audio-preview</c>).
    /// The bridge SDK handles the conversion automatically.
    /// </summary>
    MultiModal,

    /// <summary>
    /// Audio is transcribed to text via a speech-to-text model (e.g., Whisper)
    /// before being sent to the chat model.
    /// Requires <see cref="AdapterSettings.SpeechToTextDeployment"/> to be configured.
    /// </summary>
    SpeechToText
}
