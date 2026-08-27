using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace Rystem.PlayFramework.Test.Tests;

public sealed class AzureOpenAIEntraLiveGateTests : PlayFrameworkTestBase
{
    public AzureOpenAIEntraLiveGateTests() : base(useRealAzureOpenAI: true, useAzureCredential: true)
    {
    }

    [AzureLiveFact(RequiresApiKey = false)]
    [Trait("Category", "AzureOpenAIEntra")]
    public async Task Entra_ResponsesNonStreaming_ReturnsText()
    {
        // Explicit gate required by OPENAI_2_12_MIGRATION_PLAN.md (Fase 5, step 2): Entra ID must be
        // validated against Responses non-streaming specifically, not inferred from the streaming path,
        // because streaming and non-streaming responses go through different SDK conversions.
        var client = ServiceProvider.GetRequiredService<IChatClient>();
        using var timeout = LiveGateTestHelpers.CreateTimeout();

        var response = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "Reply with exactly ENTRA_OK")],
            cancellationToken: timeout.Token);

        Assert.Contains("ENTRA_OK", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [AzureLiveFact(RequiresApiKey = false)]
    [Trait("Category", "AzureOpenAIEntra")]
    public async Task Entra_ResponsesStreaming_ReturnsText()
    {
        var client = ServiceProvider.GetRequiredService<IChatClient>();
        using var timeout = LiveGateTestHelpers.CreateTimeout();
        var text = new StringBuilder();

        await foreach (var update in client.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "Reply with exactly ENTRA_STREAM_OK")],
            cancellationToken: timeout.Token))
        {
            text.Append(update.Text);
        }

        Assert.Contains("ENTRA_STREAM_OK", text.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
