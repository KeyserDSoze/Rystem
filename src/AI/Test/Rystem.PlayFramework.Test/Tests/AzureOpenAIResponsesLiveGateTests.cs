using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace Rystem.PlayFramework.Test.Tests;

public sealed class AzureOpenAIResponsesLiveGateTests : PlayFrameworkTestBase
{
    public AzureOpenAIResponsesLiveGateTests() : base(useRealAzureOpenAI: true)
    {
    }

    [AzureLiveFact]
    [Trait("Category", "AzureOpenAIResponses")]
    [Trait("Category", "AzureOpenAIApiKey")]
    public async Task Responses_NonStreaming_ReturnsTextAndUsage()
    {
        var client = ServiceProvider.GetRequiredService<IChatClient>();
        using var timeout = LiveGateTestHelpers.CreateTimeout();

        var response = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "Reply with exactly RESPONSES_OK")],
            cancellationToken: timeout.Token);

        Assert.Contains("RESPONSES_OK", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.True(response.Usage?.TotalTokenCount > 0);
    }

    [AzureLiveFact]
    [Trait("Category", "AzureOpenAIResponses")]
    [Trait("Category", "AzureOpenAIApiKey")]
    public async Task Responses_Streaming_ReturnsText()
    {
        var client = ServiceProvider.GetRequiredService<IChatClient>();
        using var timeout = LiveGateTestHelpers.CreateTimeout();
        var text = new StringBuilder();

        await foreach (var update in client.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "Reply with exactly RESPONSES_STREAM_OK")],
            cancellationToken: timeout.Token))
        {
            text.Append(update.Text);
        }

        Assert.Contains("RESPONSES_STREAM_OK", text.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [AzureLiveFact]
    [Trait("Category", "AzureOpenAIResponses")]
    [Trait("Category", "AzureOpenAIApiKey")]
    public async Task Responses_ProducesRequiredFunctionCall()
    {
        var client = ServiceProvider.GetRequiredService<IChatClient>();
        using var timeout = LiveGateTestHelpers.CreateTimeout();

        var response = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "Use the live_gate tool with value RESPONSES_TOOL_OK.")],
            LiveGateTestHelpers.CreateToolOptions(),
            timeout.Token);

        var call = Assert.Single(response.Messages.SelectMany(x => x.Contents).OfType<FunctionCallContent>());
        Assert.Equal("live_gate", call.Name);
        Assert.Equal("RESPONSES_TOOL_OK", call.Arguments?["value"]?.ToString());
    }
}
