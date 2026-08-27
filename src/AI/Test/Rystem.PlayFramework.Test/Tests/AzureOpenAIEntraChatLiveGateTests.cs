using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework.Test.Tests;

public sealed class AzureOpenAIEntraChatLiveGateTests : PlayFrameworkTestBase
{
    public AzureOpenAIEntraChatLiveGateTests()
        : base(
            useRealAzureOpenAI: true,
            useAzureCredential: true,
            deploymentEnvironmentVariable: "AZURE_OPENAI_CHAT_DEPLOYMENT",
            configureAdapter: settings => settings.UseResponsesApi = false)
    {
    }

    [AzureLiveFact(
        "AZURE_OPENAI_CHAT_DEPLOYMENT",
        RequiresApiKey = false,
        RequiresDefaultDeployment = false)]
    [Trait("Category", "AzureOpenAIEntra")]
    public async Task Entra_ChatNonStreaming_ReturnsText()
    {
        var client = ServiceProvider.GetRequiredService<IChatClient>();
        using var timeout = LiveGateTestHelpers.CreateTimeout();

        var response = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "Reply with exactly ENTRA_CHAT_OK")],
            cancellationToken: timeout.Token);

        Assert.Contains("ENTRA_CHAT_OK", response.Text, StringComparison.OrdinalIgnoreCase);
    }
}
