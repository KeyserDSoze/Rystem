using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Rystem.PlayFramework.Adapters;

namespace Rystem.PlayFramework.Test.Tests;

public sealed class AzureOpenAIAudioInlineLiveGateTests : PlayFrameworkTestBase
{
    public AzureOpenAIAudioInlineLiveGateTests()
        : base(
            useRealAzureOpenAI: true,
            deploymentEnvironmentVariable: "AZURE_OPENAI_AUDIO_DEPLOYMENT",
            configureAdapter: settings =>
            {
                settings.UseResponsesApi = false;
                settings.AudioMode = AudioMode.MultiModal;
            })
    {
    }

    [AzureLiveFact(
        "AZURE_OPENAI_AUDIO_DEPLOYMENT",
        RequiresDefaultDeployment = false,
        Skip =
            "AudioMode.MultiModal: the configured audio-capable chat deployment consistently replies that " +
            "it cannot access/listen to the attached audio even though the request contains input_audio. " +
            "Root cause requires a separate deployment/configuration investigation. Remove this Skip once " +
            "the gap is understood; the assertions below are the actual regression gate.")]
    [Trait("Category", "AzureOpenAIAudioInline")]
    [Trait("Category", "AzureOpenAIApiKey")]
    public async Task AudioInline_RepeatsEmbeddedVerificationPhrase()
    {
        // Embedding an unpredictable spoken phrase forces the model to actually process the audio:
        // there is no way to produce "violet eight" from the text prompt alone. Skipped for now until
        // the underlying deployment/configuration gap is fixed.
        using var voiceProvider = LiveGateTestHelpers.BuildVoiceProvider(AzureEndpoint!, AzureApiKey!);
        var voice = voiceProvider.GetRequiredService<IFactory<IVoiceAdapter>>().Create();
        Assert.NotNull(voice);
        var client = ServiceProvider.GetRequiredService<IChatClient>();
        using var timeout = LiveGateTestHelpers.CreateTimeout();
        var audio = await voice.SynthesizeAsync("The code words are violet eight.", timeout.Token);
        var audioContent = new DataContent(audio, "audio/mpeg") { Name = "inline-gate.mp3" };

        var response = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User,
                [new TextContent("Transcribe exactly what is spoken in the attached audio."), audioContent])],
            cancellationToken: timeout.Token);

        Assert.Contains("violet", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            response.Text.Contains("eight", StringComparison.OrdinalIgnoreCase) || response.Text.Contains('8'),
            $"Expected the response to contain 'eight' or '8', but received: {response.Text}");
    }
}
