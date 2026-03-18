namespace Rystem.PlayFramework;

/// <summary>
/// Represents a response from the voice pipeline.
/// </summary>
public sealed class VoiceResponse
{
    /// <summary>Type of voice response.</summary>
    public VoiceResponseType Type { get; init; }

    /// <summary>Audio data (for AudioChunk type).</summary>
    public ReadOnlyMemory<byte>? AudioData { get; init; }

    /// <summary>Text content (transcript for Transcription, synthesized text for AudioChunk, message for SceneEvent).</summary>
    public string? Text { get; init; }

    /// <summary>Scene response (for SceneEvent type — tool calls, status updates, etc.).</summary>
    public AiSceneResponse? SceneResponse { get; init; }

    /// <summary>STT cost for this voice call (populated on Completed event).</summary>
    public decimal SttCost { get; init; }

    /// <summary>TTS cost for this voice call (populated on Completed event).</summary>
    public decimal TtsCost { get; init; }

    /// <summary>Total audio cost (STT + TTS). Populated on Completed event.</summary>
    public decimal TotalVoiceCost => SttCost + TtsCost;

    internal static VoiceResponse Transcription(string text) => new()
    {
        Type = VoiceResponseType.Transcription,
        Text = text
    };

    internal static VoiceResponse AudioChunk(ReadOnlyMemory<byte> audio, string text) => new()
    {
        Type = VoiceResponseType.AudioChunk,
        AudioData = audio,
        Text = text
    };

    internal static VoiceResponse FromSceneResponse(AiSceneResponse response) => new()
    {
        Type = VoiceResponseType.SceneEvent,
        SceneResponse = response,
        Text = response.Message
    };

    internal static VoiceResponse Completed(decimal sttCost = 0, decimal ttsCost = 0) => new()
    {
        Type = VoiceResponseType.Completed,
        SttCost = sttCost,
        TtsCost = ttsCost
    };
}

/// <summary>
/// Type of voice pipeline response.
/// </summary>
public enum VoiceResponseType
{
    /// <summary>Transcription of user's audio input.</summary>
    Transcription,

    /// <summary>Audio chunk (TTS output for a sentence).</summary>
    AudioChunk,

    /// <summary>PlayFramework event (tool call, status, etc.).</summary>
    SceneEvent,

    /// <summary>Pipeline completed.</summary>
    Completed
}
