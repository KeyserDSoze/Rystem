namespace Rystem.PlayFramework.Adapters.FoundryLocal;

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
    /// Audio is sent inline for models that natively support audio input.
    /// Use this when the local model supports multi-modal audio.
    /// </summary>
    MultiModal,

    /// <summary>
    /// Audio is transcribed to text via a local speech-to-text model
    /// before being sent to the chat model.
    /// Requires <see cref="FoundryLocalSettings.SpeechToTextModel"/> to be configured.
    /// </summary>
    SpeechToText
}
