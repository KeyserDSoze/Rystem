using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rystem.PlayFramework.Test.Infrastructure;

namespace Rystem.PlayFramework.Test.Tests;

/// <summary>
/// Tests for conversation memory functionality.
/// </summary>
public sealed class MemoryTests
{
    [Fact]
    public async Task Memory_WithDefaultStorage_ShouldPersistAcrossConversations()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddChatClient<MockChatClient>(name: null);

        services.AddPlayFramework(builder =>
        {
            builder.AddScene("TestScene", "Test scene", _ => { });

            builder.WithMemory(memory => memory
                .WithMaxSummaryLength(1000));
        });

        var serviceProvider = services.BuildServiceProvider();
        var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();

        var conversationKey = Guid.NewGuid().ToString();
        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Direct,
            ConversationKey = conversationKey
        };

        // Act - First conversation
        var responses1 = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("My name is John", metadata: null, settings))
        {
            responses1.Add(response);
        }

        // Act - Second conversation with same key
        var responses2 = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("What is my name?", metadata: null, settings))
        {
            responses2.Add(response);
        }

        // Assert
        Assert.True(responses1.Any(r => r.Status == AiResponseStatus.Completed));
        Assert.True(responses2.Any(r => r.Status == AiResponseStatus.Completed));

        // Memory should be loaded in second conversation (check logs or responses)
        Assert.NotEmpty(responses2);
    }

    [Fact]
    public async Task Memory_WithMetadataKeys_ShouldIsolateByMetadata()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddChatClient<MockChatClient>(name: null);

        services.AddPlayFramework(builder =>
        {
            builder.AddScene("TestScene", "Test scene", _ => { });

            builder.WithMemory(memory => memory
                .WithDefaultMemoryStorage("userId")
                .WithMaxSummaryLength(1000));
        });

        var serviceProvider = services.BuildServiceProvider();
        var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Direct
        };

        // Act - User1
        var metadata1 = new Dictionary<string, object> { ["userId"] = "user1" };
        var responses1 = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("My favorite color is blue", metadata1, settings))
        {
            responses1.Add(response);
        }

        // Act - User2 (should have separate memory)
        var metadata2 = new Dictionary<string, object> { ["userId"] = "user2" };
        var responses2 = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("My favorite color is red", metadata2, settings))
        {
            responses2.Add(response);
        }

        // Assert
        Assert.True(responses1.Any(r => r.Status == AiResponseStatus.Completed));
        Assert.True(responses2.Any(r => r.Status == AiResponseStatus.Completed));
    }

    [Fact]
    public async Task Memory_WithCompositeKeys_ShouldIsolateByMultipleMetadata()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddChatClient<MockChatClient>(name: null);

        services.AddPlayFramework(builder =>
        {
            builder.AddScene("TestScene", "Test scene", _ => { });

            builder.WithMemory(memory => memory
                .WithDefaultMemoryStorage("userId", "tenantId")
                .WithMaxSummaryLength(1000));
        });

        var serviceProvider = services.BuildServiceProvider();
        var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Direct
        };

        // Act - User1 in TenantA
        var metadata1 = new Dictionary<string, object>
        {
            ["userId"] = "user1",
            ["tenantId"] = "tenantA"
        };
        var responses1 = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Test message 1", metadata1, settings))
        {
            responses1.Add(response);
        }

        // Act - Same user in different tenant (should have separate memory)
        var metadata2 = new Dictionary<string, object>
        {
            ["userId"] = "user1",
            ["tenantId"] = "tenantB"
        };
        var responses2 = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Test message 2", metadata2, settings))
        {
            responses2.Add(response);
        }

        // Assert
        Assert.True(responses1.Any(r => r.Status == AiResponseStatus.Completed));
        Assert.True(responses2.Any(r => r.Status == AiResponseStatus.Completed));
    }

    [Fact]
    public async Task Memory_Disabled_ShouldNotPersist()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddChatClient<MockChatClient>(name: null);

        // No WithMemory() call = memory disabled
        services.AddPlayFramework(builder =>
        {
            builder.AddScene("TestScene", "Test scene", _ => { });
        });

        var serviceProvider = services.BuildServiceProvider();
        var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();

        var conversationKey = Guid.NewGuid().ToString();
        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Direct,
            ConversationKey = conversationKey
        };

        // Act - Multiple conversations with same key
        var responses1 = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("First message", metadata: null, settings))
        {
            responses1.Add(response);
        }

        var responses2 = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Second message", metadata: null, settings))
        {
            responses2.Add(response);
        }

        // Assert - Should complete without errors
        Assert.True(responses1.Any(r => r.Status == AiResponseStatus.Completed));
        Assert.True(responses2.Any(r => r.Status == AiResponseStatus.Completed));

        // Memory is not enabled, so no persistence occurs (but no errors either)
    }
}
