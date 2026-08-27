using Microsoft.Extensions.Logging;
using OpenAI.Audio;
using System.ClientModel;

namespace Rystem.PlayFramework.Adapters;

/// <summary>
/// Azure OpenAI implementation of <see cref="IVoiceAdapter"/>.
/// Uses Whisper for STT and TTS-1 for speech synthesis.
/// </summary>
internal sealed class AzureOpenAIVoiceAdapter : IVoiceAdapter
{
    private readonly AudioClient _sttClient;
    private readonly AudioClient _ttsClient;
    private readonly VoiceAdapterSettings _settings;
    private readonly ILogger<AzureOpenAIVoiceAdapter>? _logger;
    private int _requiresSimpleTranscriptionFormat;

    public AzureOpenAIVoiceAdapter(
        AudioClient sttClient,
        AudioClient ttsClient,
        VoiceAdapterSettings settings,
        ILogger<AzureOpenAIVoiceAdapter>? logger = null)
    {
        _sttClient = sttClient;
        _ttsClient = ttsClient;
        _settings = settings;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TranscriptionResult> TranscribeAsync(
        ReadOnlyMemory<byte> audioData,
        string? fileName = null,
        CancellationToken cancellationToken = default)
    {
        fileName ??= "recording.wav";
        if (!Path.HasExtension(fileName))
            fileName += ".wav";

        _logger?.LogDebug("Transcribing audio: {FileName} ({Bytes} bytes)", fileName, audioData.Length);

        var bytes = audioData.ToArray();
        var responseFormat = Volatile.Read(ref _requiresSimpleTranscriptionFormat) == 1
            ? AudioTranscriptionFormat.Simple
            : AudioTranscriptionFormat.Verbose;
        AudioTranscription transcription;
        try
        {
            transcription = await TranscribeAsync(bytes, fileName, responseFormat, cancellationToken);
        }
        catch (ClientResultException exception) when (
            responseFormat == AudioTranscriptionFormat.Verbose
            && IsUnsupportedVerboseResponse(exception))
        {
            Volatile.Write(ref _requiresSimpleTranscriptionFormat, 1);
            _logger?.LogWarning(
                exception,
                "The STT deployment does not support verbose_json; falling back to json. " +
                "Detected language and duration will not be available for this deployment.");
            transcription = await TranscribeAsync(
                bytes, fileName, AudioTranscriptionFormat.Simple, cancellationToken);
        }

        var text = transcription.Text;
        var language = transcription.Language;
        var durationSeconds = transcription.Duration?.TotalSeconds;

        _logger?.LogInformation("Transcription result ({Chars} chars, lang={Language}, duration={Duration}s): \"{Preview}\"",
            text.Length, language, durationSeconds?.ToString("F1") ?? "?", text.Length > 100 ? text[..100] + "..." : text);

        return new TranscriptionResult(text, language, durationSeconds);
    }

    private async Task<AudioTranscription> TranscribeAsync(
        byte[] audioData,
        string fileName,
        AudioTranscriptionFormat responseFormat,
        CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(audioData);
        var options = new AudioTranscriptionOptions { ResponseFormat = responseFormat };
        var result = await _sttClient.TranscribeAudioAsync(
            stream, fileName, options, cancellationToken);
        return result.Value;
    }

    private static bool IsUnsupportedVerboseResponse(ClientResultException exception) =>
        exception.Status == 400
        && exception.Message.Contains("response_format", StringComparison.OrdinalIgnoreCase)
        && exception.Message.Contains("verbose_json", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public async Task<ReadOnlyMemory<byte>> SynthesizeAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Synthesizing speech: \"{Preview}\" (voice={Voice}, format={Format})",
            text.Length > 80 ? text[..80] + "..." : text,
            _settings.TtsVoice,
            _settings.TtsOutputFormat);

        var voice = GetVoice(_settings.TtsVoice);
        var options = new SpeechGenerationOptions
        {
            SpeedRatio = _settings.TtsSpeed,
            ResponseFormat = GetOutputFormat(_settings.TtsOutputFormat)
        };

        var result = await _ttsClient.GenerateSpeechAsync(text, voice, options, cancellationToken);

        var audioBytes = result.Value.ToArray();

        _logger?.LogDebug("Synthesized {Bytes} bytes of audio", audioBytes.Length);

        return new ReadOnlyMemory<byte>(audioBytes);
    }

    private static GeneratedSpeechVoice GetVoice(string voice) => voice.ToLowerInvariant() switch
    {
        "alloy" => GeneratedSpeechVoice.Alloy,
        "echo" => GeneratedSpeechVoice.Echo,
        "fable" => GeneratedSpeechVoice.Fable,
        "onyx" => GeneratedSpeechVoice.Onyx,
        "nova" => GeneratedSpeechVoice.Nova,
        "shimmer" => GeneratedSpeechVoice.Shimmer,
        _ => GeneratedSpeechVoice.Alloy
    };

    private static GeneratedSpeechFormat GetOutputFormat(string format) => format.ToLowerInvariant() switch
    {
        "mp3" => GeneratedSpeechFormat.Mp3,
        "opus" => GeneratedSpeechFormat.Opus,
        "aac" => GeneratedSpeechFormat.Aac,
        "flac" => GeneratedSpeechFormat.Flac,
        "wav" => GeneratedSpeechFormat.Wav,
        "pcm" => GeneratedSpeechFormat.Pcm,
        _ => GeneratedSpeechFormat.Mp3
    };
}
