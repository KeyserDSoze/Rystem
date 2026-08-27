using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Rystem.PlayFramework.Adapters;

namespace Rystem.PlayFramework.Test.Tests;

public sealed class AzureOpenAISpeechToTextChatLiveGateTests : PlayFrameworkTestBase
{
    public AzureOpenAISpeechToTextChatLiveGateTests()
        : base(
            useRealAzureOpenAI: true,
            configureAdapter: settings =>
            {
                settings.AudioMode = AudioMode.SpeechToText;
                settings.SpeechToTextDeployment =
                    AzureLiveTestConfiguration.Resolve("AZURE_OPENAI_STT_DEPLOYMENT");
            })
    {
    }

    [AzureLiveFact("AZURE_OPENAI_STT_DEPLOYMENT", "AZURE_OPENAI_TTS_DEPLOYMENT")]
    [Trait("Category", "AzureOpenAIVoice")]
    [Trait("Category", "AzureOpenAIApiKey")]
    public async Task SpeechToTextChatClient_TranscribesBeforeChatRequest()
    {
        using var voiceProvider = LiveGateTestHelpers.BuildVoiceProvider(AzureEndpoint!, AzureApiKey!);
        var voice = voiceProvider.GetRequiredService<IFactory<IVoiceAdapter>>().Create();
        Assert.NotNull(voice);
        using var timeout = LiveGateTestHelpers.CreateTimeout();
        var audio = await voice.SynthesizeAsync(
            "The wrapper verification phrase is amber nine.", timeout.Token);
        var chatClient = ServiceProvider.GetRequiredService<IChatClient>();
        var audioContent = new DataContent(audio, "audio/mpeg") { Name = "wrapper-gate.mp3" };

        var response = await chatClient.GetResponseAsync(
            [new ChatMessage(ChatRole.User,
                [new TextContent("Repeat only the verification phrase from the attached audio."), audioContent])],
            cancellationToken: timeout.Token);

        Assert.Contains("amber", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            response.Text.Contains("nine", StringComparison.OrdinalIgnoreCase)
            || response.Text.Contains('9'),
            $"Expected the response to contain 'nine' or '9', but received: {response.Text}");
    }
}
