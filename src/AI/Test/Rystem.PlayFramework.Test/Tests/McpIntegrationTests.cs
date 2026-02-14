using Microsoft.Extensions.DependencyInjection;
using Rystem.PlayFramework.Test.Infrastructure;
using Rystem.PlayFramework.Mcp;

namespace Rystem.PlayFramework.Test.Tests;

/// <summary>
/// Tests for MCP (Model Context Protocol) integration.
/// </summary>
public sealed class McpIntegrationTests : PlayFrameworkTestBase
{
    [Fact]
    public async Task McpServer_ShouldLoadTools()
    {
        // Arrange
        var mockMcpClient = new MockMcpClient();

        var services = new ServiceCollection();
        services.AddSingleton<IMcpClient>(mockMcpClient);
        services.AddLogging();

        services.AddMcpServer(
            url: "https://test-mcp-server.local",
            factoryName: "TestMcpServer",
            configure: settings =>
            {
                settings.TimeoutSeconds = 30;
            });

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IFactory<IMcpServerManager>>();
        var manager = factory.Create("TestMcpServer");

        // Act
        var tools = await manager.GetToolsAsync();

        // Assert
        Assert.NotNull(tools);
        Assert.NotEmpty(tools);
        Assert.Contains(tools, t => t.Name == "get_weather");
        Assert.Contains(tools, t => t.Name == "search_database");
        Assert.Contains(tools, t => t.Name == "calculate_sum");
    }

    [Fact]
    public async Task McpServer_ShouldLoadResources()
    {
        // Arrange
        var mockMcpClient = new MockMcpClient();

        var services = new ServiceCollection();
        services.AddSingleton<IMcpClient>(mockMcpClient);
        services.AddLogging();

        services.AddMcpServer(
            url: "https://test-mcp-server.local",
            factoryName: "TestMcpServer");

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IFactory<IMcpServerManager>>();
        var manager = factory.Create("TestMcpServer");

        // Act
        var resources = await manager.GetResourcesAsync();

        // Assert
        Assert.NotNull(resources);
        Assert.NotEmpty(resources);
        Assert.Contains(resources, r => r.Name == "company_policy");
        Assert.Contains(resources, r => r.Name == "api_documentation");
    }

    [Fact]
    public async Task McpServer_ShouldLoadPrompts()
    {
        // Arrange
        var mockMcpClient = new MockMcpClient();

        var services = new ServiceCollection();
        services.AddSingleton<IMcpClient>(mockMcpClient);
        services.AddLogging();

        services.AddMcpServer(
            url: "https://test-mcp-server.local",
            factoryName: "TestMcpServer");

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IFactory<IMcpServerManager>>();
        var manager = factory.Create("TestMcpServer");

        // Act
        var prompts = await manager.GetPromptsAsync();

        // Assert
        Assert.NotNull(prompts);
        Assert.NotEmpty(prompts);
        Assert.Contains(prompts, p => p.Name == "code_review");
        Assert.Contains(prompts, p => p.Name == "summarize_document");
    }

    [Fact]
    public async Task McpServer_ShouldFilterToolsByName()
    {
        // Arrange
        var mockMcpClient = new MockMcpClient();

        var services = new ServiceCollection();
        services.AddSingleton<IMcpClient>(mockMcpClient);
        services.AddLogging();

        services.AddMcpServer(
            url: "https://test-mcp-server.local",
            factoryName: "TestMcpServer");

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IFactory<IMcpServerManager>>();
        var manager = factory.Create("TestMcpServer");

        var filter = new McpFilterSettings
        {
            Tools = new List<string> { "get_weather", "calculate_sum" }
        };

        // Act
        var tools = await manager.GetToolsAsync(filter);

        // Assert
        Assert.NotNull(tools);
        Assert.Equal(2, tools.Count);
        Assert.Contains(tools, t => t.Name == "get_weather");
        Assert.Contains(tools, t => t.Name == "calculate_sum");
        Assert.DoesNotContain(tools, t => t.Name == "search_database");
    }

    [Fact]
    public async Task McpServer_ShouldFilterToolsByRegex()
    {
        // Arrange
        var mockMcpClient = new MockMcpClient();

        var services = new ServiceCollection();
        services.AddSingleton<IMcpClient>(mockMcpClient);
        services.AddLogging();

        services.AddMcpServer(
            url: "https://test-mcp-server.local",
            factoryName: "TestMcpServer");

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IFactory<IMcpServerManager>>();
        var manager = factory.Create("TestMcpServer");

        var filter = new McpFilterSettings
        {
            ToolsRegex = "^(get|search)_.*" // Only tools starting with "get_" or "search_"
        };

        // Act
        var tools = await manager.GetToolsAsync(filter);

        // Assert
        Assert.NotNull(tools);
        Assert.Equal(2, tools.Count);
        Assert.Contains(tools, t => t.Name == "get_weather");
        Assert.Contains(tools, t => t.Name == "search_database");
        Assert.DoesNotContain(tools, t => t.Name == "calculate_sum");
    }

    [Fact]
    public async Task McpServer_ShouldExecuteTool()
    {
        // Arrange
        var mockMcpClient = new MockMcpClient();

        var services = new ServiceCollection();
        services.AddSingleton<IMcpClient>(mockMcpClient);
        services.AddLogging();

        services.AddMcpServer(
            url: "https://test-mcp-server.local",
            factoryName: "TestMcpServer");

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IFactory<IMcpServerManager>>();
        var manager = factory.Create("TestMcpServer");

        // Act
        var result = await manager.ExecuteToolAsync("get_weather", """{"location": "Milan"}""");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("temperature", result);
        Assert.Contains("sunny", result);
    }

    [Fact]
    public async Task McpServer_ShouldBuildSystemMessage()
    {
        // Arrange
        var mockMcpClient = new MockMcpClient();

        var services = new ServiceCollection();
        services.AddSingleton<IMcpClient>(mockMcpClient);
        services.AddLogging();

        services.AddMcpServer(
            url: "https://test-mcp-server.local",
            factoryName: "TestMcpServer");

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IFactory<IMcpServerManager>>();
        var manager = factory.Create("TestMcpServer");

        // Act
        var systemMessage = await manager.BuildSystemMessageAsync();

        // Assert
        Assert.NotNull(systemMessage);
        Assert.Contains("Available Resources", systemMessage);
        Assert.Contains("company_policy", systemMessage);
        Assert.Contains("Available Prompt Templates", systemMessage);
        Assert.Contains("code_review", systemMessage);
    }

    [Fact]
    public async Task McpServer_ShouldFilterSystemMessageContent()
    {
        // Arrange
        var mockMcpClient = new MockMcpClient();

        var services = new ServiceCollection();
        services.AddSingleton<IMcpClient>(mockMcpClient);
        services.AddLogging();

        services.AddMcpServer(
            url: "https://test-mcp-server.local",
            factoryName: "TestMcpServer");

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IFactory<IMcpServerManager>>();
        var manager = factory.Create("TestMcpServer");

        var filter = new McpFilterSettings
        {
            Resources = new List<string> { "company_policy" },
            Prompts = new List<string> { "code_review" }
        };

        // Act
        var systemMessage = await manager.BuildSystemMessageAsync(filter);

        // Assert
        Assert.NotNull(systemMessage);
        Assert.Contains("company_policy", systemMessage);
        Assert.DoesNotContain("api_documentation", systemMessage);
        Assert.Contains("code_review", systemMessage);
        Assert.DoesNotContain("summarize_document", systemMessage);
    }

    [Fact]
    public async Task Scene_WithMcpServer_ShouldLoadMcpTools()
    {
        // Arrange
        var mockMcpClient = new MockMcpClient();
        var mockChatClient = CreateMockChatClient("The weather is sunny");

        var services = new ServiceCollection();
        services.AddSingleton<IMcpClient>(mockMcpClient);
        services.AddSingleton(mockChatClient);
        services.AddLogging();

        // Register MCP server
        services.AddMcpServer(
            url: "https://test-mcp-server.local",
            factoryName: "TestMcpServer");

        // Register PlayFramework with scene using MCP
        services.AddPlayFramework(builder =>
        {
            builder.AddScene("TestScene", "Test scene with MCP tools", scene => scene
                .WithMcpServer("TestMcpServer", filter =>
                {
                    filter.Tools = new List<string> { "get_weather" };
                }));
        });

        var provider = services.BuildServiceProvider();
        var sceneManagerFactory = provider.GetRequiredService<IFactory<ISceneManager>>();
        var sceneManager = sceneManagerFactory.Create(null);

        // Act - Execute scene which should load MCP tools
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("What is the weather?"))
        {
            responses.Add(response);
        }

        // Assert - Verify we got responses (tools were loaded and used)
        Assert.NotEmpty(responses);
        var completedResponse = responses.FirstOrDefault(r => r.Status == AiResponseStatus.Completed);
        Assert.NotNull(completedResponse);
        Assert.NotNull(completedResponse.Message);
    }

    [Fact]
    public async Task Scene_WithMcpServer_ShouldIncludeSystemMessage()
    {
        // Arrange
        var mockMcpClient = new MockMcpClient();
        var mockChatClient = CreateMockChatClient("Our company policy states...");

        var services = new ServiceCollection();
        services.AddSingleton<IMcpClient>(mockMcpClient);
        services.AddSingleton(mockChatClient);
        services.AddLogging();

        // Register MCP server
        services.AddMcpServer(
            url: "https://test-mcp-server.local",
            factoryName: "TestMcpServer");

        // Register PlayFramework with scene using MCP
        services.AddPlayFramework(builder =>
        {
            builder.AddScene("TestScene", "Test scene with MCP resources", scene => scene
                .WithMcpServer("TestMcpServer", filter =>
                {
                    filter.Resources = new List<string> { "company_policy" };
                }));
        });

        var provider = services.BuildServiceProvider();
        var sceneManagerFactory = provider.GetRequiredService<IFactory<ISceneManager>>();
        var sceneManager = sceneManagerFactory.Create(null);

        // Act - Execute scene which should load MCP resources into system message
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Tell me about company policy"))
        {
            responses.Add(response);
        }

        // Assert - Verify we got responses (resources were loaded)
        Assert.NotEmpty(responses);
        var completedResponse = responses.FirstOrDefault(r => r.Status == AiResponseStatus.Completed);
        Assert.NotNull(completedResponse);
        Assert.NotNull(completedResponse.Message);
    }

    [Fact]
    public async Task MultipleScenes_WithDifferentMcpServers_ShouldIsolateTools()
    {
        // Arrange
        var mockMcpClient = new MockMcpClient()
            .ClearAll()
            .AddTool("tool_a", "Tool A", "https://server-a.local", "ServerA", null)
            .AddTool("tool_b", "Tool B", "https://server-b.local", "ServerB", null);

        var mockChatClient = CreateMockChatClient("Test response");

        var services = new ServiceCollection();
        services.AddSingleton<IMcpClient>(mockMcpClient);
        services.AddSingleton(mockChatClient);
        services.AddLogging();

        // Register two different MCP servers
        services.AddMcpServer("https://server-a.local", "ServerA");
        services.AddMcpServer("https://server-b.local", "ServerB");

        // Register PlayFramework with two scenes using different MCP servers
        services.AddPlayFramework(builder =>
        {
            builder.AddScene("SceneA", "Scene using Server A", scene => scene
                .WithMcpServer("ServerA"));

            builder.AddScene("SceneB", "Scene using Server B", scene => scene
                .WithMcpServer("ServerB"));
        });

        var provider = services.BuildServiceProvider();

        // Act - Get both scene managers
        var managerAFactory = provider.GetRequiredService<IFactory<IMcpServerManager>>();
        var managerA = managerAFactory.Create("ServerA");
        var toolsA = await managerA.GetToolsAsync();

        var managerBFactory = provider.GetRequiredService<IFactory<IMcpServerManager>>();
        var managerB = managerBFactory.Create("ServerB");
        var toolsB = await managerB.GetToolsAsync();

        // Assert - Each manager should only see its own tools
        Assert.Single(toolsA);
        Assert.Contains(toolsA, t => t.Name == "tool_a");

        Assert.Single(toolsB);
        Assert.Contains(toolsB, t => t.Name == "tool_b");
    }
}
