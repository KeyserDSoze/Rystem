using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
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
        // Test works with MockChatClient which returns "Mock response"
        Assert.Contains("Mock", messageText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OpenAiSettings_ShouldBind_WhenConfigured()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenAi:ApiKey"] = "test-key",
                ["OpenAi:Endpoint"] = "https://name.openai.azure.com/",
                ["OpenAi:ModelName"] = "test-deployment"
            })
            .Build();
        var settings = new OpenAiSettings();

        configuration.GetSection("OpenAi").Bind(settings);

        Assert.Equal("test-key", settings.ApiKey);
        Assert.Equal("https://name.openai.azure.com/", settings.Endpoint);
        Assert.Equal("test-deployment", settings.ModelName);
    }
}
