using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework.Test.Tests;

public sealed class AzureOpenAIVoiceLiveGateTests : PlayFrameworkTestBase
{
    public AzureOpenAIVoiceLiveGateTests() : base(useRealAzureOpenAI: true)
    {
    }

    [AzureLiveFact("AZURE_OPENAI_STT_DEPLOYMENT", "AZURE_OPENAI_TTS_DEPLOYMENT")]
    [Trait("Category", "AzureOpenAIVoice")]
    [Trait("Category", "AzureOpenAIApiKey")]
    public async Task Tts_GeneratesMp3Audio()
    {
        using var provider = LiveGateTestHelpers.BuildVoiceProvider(AzureEndpoint!, AzureApiKey!);
        var voice = provider.GetRequiredService<IFactory<IVoiceAdapter>>().Create();
        Assert.NotNull(voice);
        using var timeout = LiveGateTestHelpers.CreateTimeout();

        var audio = await voice.SynthesizeAsync("Rystem voice gate is working.", timeout.Token);

        Assert.True(audio.Length > 100);
    }

    [AzureLiveFact("AZURE_OPENAI_STT_DEPLOYMENT", "AZURE_OPENAI_TTS_DEPLOYMENT")]
    [Trait("Category", "AzureOpenAIVoice")]
    [Trait("Category", "AzureOpenAIApiKey")]
    public async Task Stt_TranscribesAudioGeneratedByTts()
    {
        using var provider = LiveGateTestHelpers.BuildVoiceProvider(AzureEndpoint!, AzureApiKey!);
        var voice = provider.GetRequiredService<IFactory<IVoiceAdapter>>().Create();
        Assert.NotNull(voice);
        using var timeout = LiveGateTestHelpers.CreateTimeout();
        var audio = await voice.SynthesizeAsync("The verification phrase is cobalt seven.", timeout.Token);

        var transcription = await voice.TranscribeAsync(audio, "rystem-live-gate.mp3", timeout.Token);

        Assert.Contains("cobalt", transcription.Text, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            transcription.Text.Contains("seven", StringComparison.OrdinalIgnoreCase)
            || transcription.Text.Contains('7'),
            $"Expected the transcription to contain 'seven' or '7', but received: {transcription.Text}");
    }
}
