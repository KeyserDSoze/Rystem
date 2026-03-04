using Microsoft.Extensions.Logging;
using OpenAI.Audio;

namespace Rystem.PlayFramework.Adapters.FoundryLocal;

/// <summary>
/// Foundry Local implementation of <see cref="IVoiceAdapter"/>.
/// Uses locally-running STT and TTS models via the OpenAI-compatible API.
/// </summary>
internal sealed class FoundryLocalVoiceAdapter : IVoiceAdapter
{
    private readonly AudioClient _sttClient;
    private readonly AudioClient _ttsClient;
    private readonly VoiceAdapterSettings _settings;
    private readonly ILogger<FoundryLocalVoiceAdapter>? _logger;

    public FoundryLocalVoiceAdapter(
        AudioClient sttClient,
        AudioClient ttsClient,
        VoiceAdapterSettings settings,
        ILogger<FoundryLocalVoiceAdapter>? logger = null)
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

        _logger?.LogDebug("Foundry Local: transcribing audio {FileName} ({Bytes} bytes)", fileName, audioData.Length);

        using var stream = new MemoryStream(audioData.ToArray());
        var options = new AudioTranscriptionOptions
        {
            ResponseFormat = AudioTranscriptionFormat.Verbose
        };
        var result = await _sttClient.TranscribeAudioAsync(stream, fileName, options, cancellationToken);

        var text = result.Value.Text;
        var language = result.Value.Language;

        _logger?.LogInformation("Foundry Local: transcribed ({Chars} chars, lang={Language}): \"{Preview}\"",
            text.Length, language, text.Length > 100 ? text[..100] + "..." : text);

        return new TranscriptionResult(text, language);
    }

    /// <inheritdoc />
    public async Task<ReadOnlyMemory<byte>> SynthesizeAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Foundry Local: synthesizing speech ({Chars} chars, voice={Voice})",
            text.Length, _settings.TtsVoice);

        var voice = new GeneratedSpeechVoice(_settings.TtsVoice);
        var options = new SpeechGenerationOptions
        {
            SpeedRatio = _settings.TtsSpeed,
            ResponseFormat = GetOutputFormat(_settings.TtsOutputFormat)
        };

        var result = await _ttsClient.GenerateSpeechAsync(text, voice, options, cancellationToken);

        var audioBytes = result.Value.ToArray();

        _logger?.LogDebug("Foundry Local: synthesized {Bytes} bytes of audio", audioBytes.Length);

        return new ReadOnlyMemory<byte>(audioBytes);
    }

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
