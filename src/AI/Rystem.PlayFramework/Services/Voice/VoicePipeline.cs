using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Rystem.PlayFramework.Services.Voice;

/// <summary>
/// Orchestrates the voice pipeline: STT → PlayFramework → sentence chunking → TTS.
/// Streams audio chunks back as each sentence is synthesized.
/// </summary>
internal sealed class VoicePipeline
{
    private readonly ISceneManager _sceneManager;
    private readonly IVoiceAdapter _voiceAdapter;
    private readonly VoiceSettings _voiceSettings;
    private readonly ILogger? _logger;

    public VoicePipeline(
        ISceneManager sceneManager,
        IVoiceAdapter voiceAdapter,
        VoiceSettings voiceSettings,
        ILogger? logger = null)
    {
        _sceneManager = sceneManager;
        _voiceAdapter = voiceAdapter;
        _voiceSettings = voiceSettings;
        _logger = logger;
    }

    /// <summary>
    /// Processes an audio input through the full voice pipeline:
    /// 1. Transcribe audio → text (STT)
    /// 2. Send text to PlayFramework with streaming enabled
    /// 3. Accumulate streaming chunks into sentences
    /// 4. Synthesize each sentence → audio (TTS)
    /// 5. Yield audio chunks as they become available
    /// </summary>
    public async IAsyncEnumerable<VoiceResponse> ProcessAsync(
        ReadOnlyMemory<byte> audioData,
        string? fileName,
        Dictionary<string, object>? metadata,
        SceneRequestSettings? settings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 1. STT: Transcribe audio to text
        _logger?.LogInformation("Voice pipeline: transcribing audio ({Bytes} bytes)", audioData.Length);

        var transcriptionResult = await _voiceAdapter.TranscribeAsync(audioData, fileName, cancellationToken);
        var transcript = transcriptionResult.Text;
        var detectedLanguage = transcriptionResult.DetectedLanguage;

        _logger?.LogInformation("Voice pipeline: transcribed -> \"{Transcript}\" (lang={Language})", transcript, detectedLanguage);

        // Yield transcript event
        yield return VoiceResponse.Transcription(transcript);

        if (string.IsNullOrWhiteSpace(transcript))
        {
            yield return VoiceResponse.Completed();
            yield break;
        }

        // 2. Execute through PlayFramework with streaming
        settings ??= new SceneRequestSettings();
        settings.EnableStreaming = true;

        // Mark this request as voice mode so the SceneManager injects the voice style instruction
        settings.IsVoiceMode = true;

        // Inject language instruction as a system-level instruction (not in user message)
        if (!string.IsNullOrWhiteSpace(_voiceSettings.LanguageInstruction)
            && !string.IsNullOrWhiteSpace(detectedLanguage))
        {
            var instruction = _voiceSettings.LanguageInstruction.Replace("{language}", detectedLanguage);
            settings.AdditionalSystemInstructions ??= [];
            settings.AdditionalSystemInstructions.Add(instruction);
        }

        var accumulator = new SentenceAccumulator(_voiceSettings);

        await foreach (var response in _sceneManager.ExecuteAsync(transcript, metadata, settings, cancellationToken))
        {
            // Forward non-streaming events (tool calls, status updates, etc.)
            if (response.Status != AiResponseStatus.Streaming)
            {
                yield return VoiceResponse.FromSceneResponse(response);

                // If it's a completed status with a final message, synthesize it
                if (response.Status == AiResponseStatus.Completed && !string.IsNullOrEmpty(response.Message))
                {
                    var remaining = accumulator.Flush();
                    if (remaining is not null)
                    {
                        var audio = await SynthesizeSafe(remaining, cancellationToken);
                        if (audio is not null)
                            yield return VoiceResponse.AudioChunk(audio.Value, remaining);
                    }
                }
                continue;
            }

            // 3. Accumulate streaming chunks into sentences
            var chunk = response.StreamingChunk;
            if (string.IsNullOrEmpty(chunk))
                continue;

            var sentence = accumulator.Append(chunk);
            if (sentence is not null)
            {
                // 4. TTS: Synthesize the sentence
                var audio = await SynthesizeSafe(sentence, cancellationToken);
                if (audio is not null)
                    yield return VoiceResponse.AudioChunk(audio.Value, sentence);
            }

            // If streaming is complete, flush remaining buffer
            if (response.IsStreamingComplete)
            {
                var remaining = accumulator.Flush();
                if (remaining is not null)
                {
                    var audio = await SynthesizeSafe(remaining, cancellationToken);
                    if (audio is not null)
                        yield return VoiceResponse.AudioChunk(audio.Value, remaining);
                }
            }
        }

        yield return VoiceResponse.Completed();
    }

    private async Task<ReadOnlyMemory<byte>?> SynthesizeSafe(string text, CancellationToken ct)
    {
        try
        {
            _logger?.LogDebug("Voice pipeline: synthesizing \"{Text}\"", text);
            return await _voiceAdapter.SynthesizeAsync(text, ct);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Voice pipeline: TTS failed for \"{Text}\"", text);
            return null;
        }
    }
}
