using Microsoft.Extensions.DependencyInjection;
using Rystem.PlayFramework.Mcp;
using Xunit;

namespace Rystem.PlayFramework.Test.Tests;

/// <summary>
/// Tests for InMemoryMcpServer - in-memory MCP server for testing.
/// </summary>
public class InMemoryMcpServerTests
{
    [Fact]
    public void InMemoryMcpServer_CreateDefault_ShouldHaveTools()
    {
        // Act
        var server = InMemoryMcpServer.CreateDefault();
        var tools = server.GetToolsAsync(new McpServerSettings { Url = "test", Name = "test" }).Result;

        // Assert
        Assert.NotEmpty(tools);
        Assert.Contains(tools, t => t.Name == "get_weather");
        Assert.Contains(tools, t => t.Name == "search_database");
        Assert.Contains(tools, t => t.Name == "calculate_sum");
    }

    [Fact]
    public void InMemoryMcpServer_CreateDefault_ShouldHaveResources()
    {
        // Act
        var server = InMemoryMcpServer.CreateDefault();
        var resources = server.GetResourcesAsync(new McpServerSettings { Url = "test", Name = "test" }).Result;

        // Assert
        Assert.NotEmpty(resources);
        Assert.Contains(resources, r => r.Name == "company_policy");
        Assert.Contains(resources, r => r.Name == "api_documentation");
    }

    [Fact]
    public void InMemoryMcpServer_CreateDefault_ShouldHavePrompts()
    {
        // Act
        var server = InMemoryMcpServer.CreateDefault();
        var prompts = server.GetPromptsAsync(new McpServerSettings { Url = "test", Name = "test" }).Result;

        // Assert
        Assert.NotEmpty(prompts);
        Assert.Contains(prompts, p => p.Name == "code_review");
        Assert.Contains(prompts, p => p.Name == "summarize_document");
    }

    [Fact]
    public async Task InMemoryMcpServer_ExecuteTool_ShouldCallExecutor()
    {
        // Arrange
        var server = new InMemoryMcpServer();
        var executorCalled = false;
        var capturedArgs = "";

        server.AddTool(
            name: "test_tool",
            description: "Test tool",
            executor: async args =>
            {
                executorCalled = true;
                capturedArgs = args;
                return await Task.FromResult("Success");
            });

        var settings = new McpServerSettings { Url = "test", Name = "test" };

        // Act
        var result = await server.ExecuteToolAsync(settings, "test_tool", "{\"param\":\"value\"}");

        // Assert
        Assert.True(executorCalled);
        Assert.Equal("{\"param\":\"value\"}", capturedArgs);
        Assert.Equal("Success", result);
    }

    [Fact]
    public async Task InMemoryMcpServer_ReadResource_ShouldReturnContent()
    {
        // Arrange
        var server = new InMemoryMcpServer();
        server.AddResource(
            name: "test_resource",
            uri: "internal://test",
            description: "Test resource",
            content: "Resource content here");

        var settings = new McpServerSettings { Url = "test", Name = "test" };

        // Act
        var content = await server.ReadResourceAsync(settings, "internal://test");

        // Assert
        Assert.Equal("Resource content here", content);
    }

    [Fact]
    public async Task InMemoryMcpServer_ReadResource_WithLoader_ShouldCallLoader()
    {
        // Arrange
        var server = new InMemoryMcpServer();
        var loaderCalled = false;

        server.AddResource(
            name: "test_resource",
            uri: "internal://test",
            description: "Test resource",
            loader: async () =>
            {
                loaderCalled = true;
                return await Task.FromResult("Loaded content");
            });

        var settings = new McpServerSettings { Url = "test", Name = "test" };

        // Act
        var content = await server.ReadResourceAsync(settings, "internal://test");

        // Assert
        Assert.True(loaderCalled);
        Assert.Equal("Loaded content", content);
    }

    [Fact]
    public void InMemoryMcpServer_AddTool_ShouldBeFluentApi()
    {
        // Act
        var server = new InMemoryMcpServer()
            .AddTool("tool1", "First tool")
            .AddTool("tool2", "Second tool")
            .AddTool("tool3", "Third tool");

        var tools = server.GetToolsAsync(new McpServerSettings { Url = "test", Name = "test" }).Result;

        // Assert
        Assert.Equal(3, tools.Count);
    }

    [Fact]
    public void InMemoryMcpServer_Clear_ShouldRemoveAllItems()
    {
        // Arrange
        var server = InMemoryMcpServer.CreateDefault();

        // Act
        server.Clear();
        var tools = server.GetToolsAsync(new McpServerSettings { Url = "test", Name = "test" }).Result;
        var resources = server.GetResourcesAsync(new McpServerSettings { Url = "test", Name = "test" }).Result;
        var prompts = server.GetPromptsAsync(new McpServerSettings { Url = "test", Name = "test" }).Result;

        // Assert
        Assert.Empty(tools);
        Assert.Empty(resources);
        Assert.Empty(prompts);
    }

    [Fact]
    public void AddInMemoryMcpServer_ShouldRegisterWithDI()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddInMemoryMcpServer("TestServer", server =>
        {
            server.Clear();
            server.AddTool("custom_tool", "Custom tool for testing");
        });

        var provider = services.BuildServiceProvider();
        var mcpManagerFactory = provider.GetRequiredService<IFactory<IMcpServerManager>>();
        var mcpManager = mcpManagerFactory.Create("TestServer");

        // Assert
        Assert.NotNull(mcpManager);
        var tools = mcpManager.GetToolsAsync().Result;
        Assert.Single(tools);
        Assert.Equal("custom_tool", tools[0].Name);
    }

    [Fact]
    public async Task InMemoryMcpServer_WithPlayFramework_ShouldIntegrate()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Add mock chat client (required by PlayFramework)
        var mockChatClient = PlayFrameworkTestBase.CreateMockChatClient("Forecast: Sunny with 25°C");
        services.AddSingleton(mockChatClient);

        // Add in-memory MCP server
        services.AddInMemoryMcpServer("WeatherServer", server =>
        {
            server.Clear()
                .AddTool(
                    name: "get_forecast",
                    description: "Get weather forecast",
                    executor: async _ => await Task.FromResult("Sunny with 25°C"));
        });

        // Add PlayFramework with scene using in-memory MCP
        services.AddPlayFramework(builder =>
        {
            builder.AddScene(scene => scene
                .WithName("WeatherScene")
                .WithDescription("Weather information scene")
                .WithMcpServer("WeatherServer"));
        });

        var provider = services.BuildServiceProvider();
        var sceneManagerFactory = provider.GetRequiredService<IFactory<ISceneManager>>();
        var sceneManager = sceneManagerFactory.Create(null);

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("What's the forecast?"))
        {
            responses.Add(response);
        }

        // Assert
        Assert.NotEmpty(responses);
    }

    [Fact]
    public async Task InMemoryMcpServer_MultipleInstances_ShouldBeIsolated()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Add two separate in-memory MCP servers
        services.AddInMemoryMcpServer("ServerA", server =>
        {
            server.Clear().AddTool("tool_a", "Tool from server A");
        });

        services.AddInMemoryMcpServer("ServerB", server =>
        {
            server.Clear().AddTool("tool_b", "Tool from server B");
        });

        var provider = services.BuildServiceProvider();
        var mcpManagerFactory = provider.GetRequiredService<IFactory<IMcpServerManager>>();

        // Act
        var managerA = mcpManagerFactory.Create("ServerA");
        var managerB = mcpManagerFactory.Create("ServerB");

        var toolsA = await managerA.GetToolsAsync();
        var toolsB = await managerB.GetToolsAsync();

        // Assert
        Assert.Single(toolsA);
        Assert.Equal("tool_a", toolsA[0].Name);

        Assert.Single(toolsB);
        Assert.Equal("tool_b", toolsB[0].Name);
    }
}
