using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework.Test;

/// <summary>
/// Basic integration tests for PlayFramework with Azure OpenAI.
/// </summary>
public sealed class BasicPlayFrameworkTests : PlayFrameworkTestBase
{
    [Fact]
    public async Task ChatClient_ShouldBeRegistered_AndRespond()
    {
        // Arrange
        var chatClient = ServiceProvider.GetRequiredService<IChatClient>();

        // Act
        var response = await chatClient.GetResponseAsync(new[]
        {
            new ChatMessage(ChatRole.User, "Say 'Hello, World!' and nothing else.")
        });

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Messages);
        Assert.NotEmpty(response.Messages);

        var messageText = response.Messages.FirstOrDefault()?.Text;
        Assert.NotNull(messageText);
        Assert.Contains("Hello", messageText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OpenAiSettings_ShouldBeLoaded()
    {
        // Assert
        Assert.NotNull(OpenAiSettings);
        Assert.False(string.IsNullOrWhiteSpace(OpenAiSettings.ApiKey));
        Assert.False(string.IsNullOrWhiteSpace(OpenAiSettings.AzureResourceName));
        Assert.False(string.IsNullOrWhiteSpace(OpenAiSettings.ModelName));
    }
}

