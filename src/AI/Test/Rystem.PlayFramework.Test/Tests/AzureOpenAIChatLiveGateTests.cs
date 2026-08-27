using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace Rystem.PlayFramework.Test.Tests;

public sealed class AzureOpenAIChatLiveGateTests : PlayFrameworkTestBase
{
    public AzureOpenAIChatLiveGateTests()
        : base(
            useRealAzureOpenAI: true,
            deploymentEnvironmentVariable: "AZURE_OPENAI_CHAT_DEPLOYMENT",
            configureAdapter: settings => settings.UseResponsesApi = false)
    {
    }

    [AzureLiveFact("AZURE_OPENAI_CHAT_DEPLOYMENT", RequiresDefaultDeployment = false)]
    [Trait("Category", "AzureOpenAIChat")]
    [Trait("Category", "AzureOpenAIApiKey")]
    public async Task Chat_NonStreaming_ReturnsTextAndUsage()
    {
        var client = ServiceProvider.GetRequiredService<IChatClient>();
        using var timeout = LiveGateTestHelpers.CreateTimeout();

        var response = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "Reply with exactly CHAT_OK")],
            cancellationToken: timeout.Token);

        Assert.Contains("CHAT_OK", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.True(response.Usage?.TotalTokenCount > 0);
    }

    [AzureLiveFact("AZURE_OPENAI_CHAT_DEPLOYMENT", RequiresDefaultDeployment = false)]
    [Trait("Category", "AzureOpenAIChat")]
    [Trait("Category", "AzureOpenAIApiKey")]
    public async Task Chat_Streaming_ReturnsText()
    {
        var client = ServiceProvider.GetRequiredService<IChatClient>();
        using var timeout = LiveGateTestHelpers.CreateTimeout();
        var text = new StringBuilder();

        await foreach (var update in client.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "Reply with exactly CHAT_STREAM_OK")],
            cancellationToken: timeout.Token))
        {
            text.Append(update.Text);
        }

        Assert.Contains("CHAT_STREAM_OK", text.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [AzureLiveFact("AZURE_OPENAI_CHAT_DEPLOYMENT", RequiresDefaultDeployment = false)]
    [Trait("Category", "AzureOpenAIChat")]
    [Trait("Category", "AzureOpenAIApiKey")]
    public async Task Chat_ProducesRequiredFunctionCall()
    {
        var client = ServiceProvider.GetRequiredService<IChatClient>();
        using var timeout = LiveGateTestHelpers.CreateTimeout();

        var response = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "Use the live_gate tool with value CHAT_TOOL_OK.")],
            LiveGateTestHelpers.CreateToolOptions(),
            timeout.Token);

        var call = Assert.Single(response.Messages.SelectMany(x => x.Contents).OfType<FunctionCallContent>());
        Assert.Equal("live_gate", call.Name);
        Assert.Equal("CHAT_TOOL_OK", call.Arguments?["value"]?.ToString());
    }
}
