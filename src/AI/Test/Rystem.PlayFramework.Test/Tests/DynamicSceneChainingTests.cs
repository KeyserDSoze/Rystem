using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework.Test.Tests;

/// <summary>
/// Tests for dynamic scene chaining - the third execution mode that allows
/// scenes to be chained dynamically based on LLM decisions during execution.
/// </summary>
public sealed class DynamicSceneChainingTests : PlayFrameworkTestBase
{
    /// <summary>
    /// Test basic dynamic chaining with two scenes.
    /// First scene: Get sales data
    /// LLM decides to chain to second scene: Generate report
    /// </summary>
    [Fact]
    public async Task DynamicChaining_TwoScenes_ExecutesBothSequentially()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Create mock chat client that selects scenes and tools
        var callCount = 0;
        services.AddSingleton<IChatClient>(sp => new DelegatingMockChatClient(() =>
        {
            callCount++;
            return callCount switch
            {
                1 => CreateSceneSelectionResponse("SalesAnalysis"),     // Select SalesAnalysis
                2 => CreateToolCallResponse("GetSalesData"),             // Call GetSalesData tool
                3 => CreateTextResponse("Sales for Q1: $1.5M, up 20%"), // Tool result
                4 => CreateContinueResponse(true),                       // YES - continue to next scene
                5 => CreateSceneSelectionResponse("ReportGenerator"),   // Select ReportGenerator
                6 => CreateToolCallResponse("GenerateReport"),           // Call GenerateReport tool
                7 => CreateTextResponse("## Q1 Sales Report\n\nTotal: $1.5M\nGrowth: +20%"), // Tool result
                8 => CreateContinueResponse(false),                      // NO - stop
                _ => CreateTextResponse("Final report complete")         // Final response
            };
        }));

        services.AddPlayFramework(builder =>
        {
            builder
                .AddScene("SalesAnalysis", "Analyzes sales data", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<SalesService>(serviceBuilder =>
                        {
                            serviceBuilder.WithMethod(x => x.GetSalesData(), "GetSalesData", "Get sales data");
                        });
                })
                .AddScene("ReportGenerator", "Generates formatted reports", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<ReportService>(serviceBuilder =>
                        {
                            serviceBuilder.WithMethod(x => x.GenerateReport(), "GenerateReport", "Generate report");
                        });
                });
        });

        services.AddSingleton<SalesService>();
        services.AddSingleton<ReportService>();

        var provider = services.BuildServiceProvider();
        var sceneManager = provider.GetRequiredService<ISceneManager>();

        // Act
        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.DynamicChaining,
            MaxDynamicScenes = 5
        };

        var results = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync(
            "Analyze Q1 sales and generate a report",
            settings))
        {
            results.Add(response);
        }

        // Assert
        Assert.NotEmpty(results);

        // Verify both scenes were executed
        var sceneExecutions = results.Where(r => r.Status == AiResponseStatus.ExecutingScene).ToList();
        Assert.Equal(2, sceneExecutions.Count);
        Assert.Contains(sceneExecutions, r => r.SceneName == "SalesAnalysis");
        Assert.Contains(sceneExecutions, r => r.SceneName == "ReportGenerator");

        // Verify final response was generated
        Assert.Contains(results, r => r.Status == AiResponseStatus.Completed);
    }

    /// <summary>
    /// Test that dynamic chaining stops after max scenes reached.
    /// </summary>
    [Fact]
    public async Task DynamicChaining_MaxScenesLimit_StopsAfterLimit()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        var callCount = 0;
        services.AddSingleton<IChatClient>(sp => new DelegatingMockChatClient(() =>
        {
            callCount++;
            var sceneIndex = (callCount - 1) / 4 + 1; // Each scene: select + tool call + result + continue
            var stepInScene = (callCount - 1) % 4;

            if (sceneIndex <= 3)
            {
                return stepInScene switch
                {
                    0 => CreateSceneSelectionResponse($"Scene{sceneIndex}"),
                    1 => CreateToolCallResponse($"Tool{sceneIndex}"),
                    2 => CreateTextResponse($"Result {sceneIndex}"),
                    3 => CreateContinueResponse(true), // Always YES for first 3 scenes
                    _ => CreateTextResponse("Unexpected")
                };
            }

            return CreateTextResponse("Final answer after 3 scenes");
        }));

        services.AddPlayFramework(builder =>
        {
            builder
                .AddScene("Scene1", "Scene1", s => s.WithService<TestService>(b => b.WithMethod(x => x.Tool1(), "Tool1", "")))
                .AddScene("Scene2", "Scene2", s => s.WithService<TestService>(b => b.WithMethod(x => x.Tool2(), "Tool2", "")))
                .AddScene("Scene3", "Scene3", s => s.WithService<TestService>(b => b.WithMethod(x => x.Tool3(), "Tool3", "")))
                .AddScene("Scene4", "Scene4", s => s.WithService<TestService>(b => b.WithMethod(x => x.Tool4(), "Tool4", ""))); // Won't be reached
        });

        services.AddSingleton<TestService>();

        var provider = services.BuildServiceProvider();
        var sceneManager = provider.GetRequiredService<ISceneManager>();

        // Act
        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.DynamicChaining,
            MaxDynamicScenes = 3 // Limit to 3 scenes
        };

        var results = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Execute multiple scenes", settings))
        {
            results.Add(response);
        }

        // Assert
        var sceneExecutions = results.Where(r => r.Status == AiResponseStatus.ExecutingScene).ToList();
        Assert.Equal(3, sceneExecutions.Count); // Exactly 3 scenes
        Assert.DoesNotContain(sceneExecutions, r => r.SceneName == "Scene4"); // Scene4 never executed
    }

    /// <summary>
    /// Test that already executed scenes are excluded from next selection.
    /// </summary>
    [Fact]
    public async Task DynamicChaining_ExcludesExecutedScenes_OnlyOffersRemaining()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        var callCount = 0;
        services.AddSingleton<IChatClient>(sp => new DelegatingMockChatClient(() =>
        {
            callCount++;
            return callCount switch
            {
                1 => CreateSceneSelectionResponse("DataFetcher"),
                2 => CreateToolCallResponse("FetchData"),
                3 => CreateTextResponse("Data: [1,2,3]"),
                4 => CreateContinueResponse(true),
                5 => CreateSceneSelectionResponse("Analyzer"),
                6 => CreateToolCallResponse("AnalyzeData"),
                7 => CreateTextResponse("Analysis: Average is 2"),
                8 => CreateContinueResponse(false),
                _ => CreateTextResponse("Complete analysis with average")
            };
        }));

        services.AddPlayFramework(builder =>
        {
            builder
                .AddScene("DataFetcher", "DataFetcher", s => s.WithService<DataService>(b => b.WithMethod(x => x.FetchData(), "FetchData", "")))
                .AddScene("Analyzer", "Analyzer", s => s.WithService<DataService>(b => b.WithMethod(x => x.AnalyzeData(), "AnalyzeData", "")))
                .AddScene("Reporter", "Reporter", s => s.WithService<DataService>(b => b.WithMethod(x => x.GenerateReport(), "GenerateReport", "")));
        });

        services.AddSingleton<DataService>();

        var provider = services.BuildServiceProvider();
        var sceneManager = provider.GetRequiredService<ISceneManager>();

        // Act
        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.DynamicChaining,
            MaxDynamicScenes = 5
        };

        var results = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Fetch and analyze data", settings))
        {
            results.Add(response);
        }

        // Assert
        var sceneExecutions = results.Where(r => r.Status == AiResponseStatus.ExecutingScene).ToList();
        Assert.Equal(2, sceneExecutions.Count);

        // Verify DataFetcher was only executed once
        var dataFetcherExecutions = sceneExecutions.Count(r => r.SceneName == "DataFetcher");
        Assert.Equal(1, dataFetcherExecutions);
    }

    /// <summary>
    /// Test that LLM can decide not to continue after first scene.
    /// </summary>
    [Fact]
    public async Task DynamicChaining_LlmDecidesStop_AfterFirstScene()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        var callCount = 0;
        services.AddSingleton<IChatClient>(sp => new DelegatingMockChatClient(() =>
        {
            callCount++;
            return callCount switch
            {
                1 => CreateSceneSelectionResponse("Add"), // Select "Add" scene (registered below)
                2 => CreateToolCallResponse("Add"), // Call Add tool
                3 => CreateTextResponse("Result: 5"),
                4 => CreateContinueResponse(false), // NO - stop immediately
                _ => CreateTextResponse("The sum is 5")
            };
        }));

        services.AddPlayFramework(builder =>
        {
            builder
                .AddScene("Add", "Add", s => s.WithService<CalculatorService>(b => b.WithMethod(x => x.Add(default, default), "Add", "")))
                .AddScene("Multiply", "Multiply", s => s.WithService<CalculatorService>(b => b.WithMethod(x => x.Multiply(default, default), "Multiply", "")));
        });

        services.AddSingleton<CalculatorService>();

        var provider = services.BuildServiceProvider();
        var sceneManager = provider.GetRequiredService<ISceneManager>();

        // Act
        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.DynamicChaining
        };

        var results = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("What is 2 + 3?", settings))
        {
            results.Add(response);
        }

        // Assert
        var sceneExecutions = results.Where(r => r.Status == AiResponseStatus.ExecutingScene).ToList();
        Assert.Single(sceneExecutions); // Only one scene executed
        Assert.Equal("Add", sceneExecutions[0].SceneName); // Should be "Add" scene
    }

    // Helper methods to create responses
    private static ChatResponse CreateSceneSelectionResponse(string sceneName)
    {
        var functionCall = new FunctionCallContent(
            callId: Guid.NewGuid().ToString(),
            name: sceneName,
            arguments: new Dictionary<string, object?>());

        var message = new ChatMessage(ChatRole.Assistant, [functionCall]);

        return new ChatResponse([message])
        {
            ModelId = "mock-model",
            Usage = new UsageDetails { InputTokenCount = 100, OutputTokenCount = 50 }
        };
    }

    private static ChatResponse CreateToolCallResponse(string toolName)
    {
        var functionCall = new FunctionCallContent(
            callId: Guid.NewGuid().ToString(),
            name: toolName,
            arguments: new Dictionary<string, object?>());

        var message = new ChatMessage(ChatRole.Assistant, [functionCall]);

        return new ChatResponse([message])
        {
            ModelId = "mock-model",
            Usage = new UsageDetails { InputTokenCount = 100, OutputTokenCount = 50 }
        };
    }

    private static ChatResponse CreateTextResponse(string text)
    {
        var message = new ChatMessage(ChatRole.Assistant, text);

        return new ChatResponse([message])
        {
            ModelId = "mock-model",
            Usage = new UsageDetails { InputTokenCount = 50, OutputTokenCount = 100 }
        };
    }

    private static ChatResponse CreateContinueResponse(bool shouldContinue)
    {
        var text = shouldContinue ? "YES, I need to execute another scene" : "NO, the task is complete";
        return CreateTextResponse(text);
    }

    // Helper services for tests
    internal class SalesService
    {
        public string GetSalesData() => "Q1 Sales: $1.5M";
    }

    internal class ReportService
    {
        public string GenerateReport() => "## Sales Report\n\nTotal: $1.5M";
    }

    internal class TestService
    {
        public string Tool1() => "Tool1 executed";
        public string Tool2() => "Tool2 executed";
        public string Tool3() => "Tool3 executed";
        public string Tool4() => "Tool4 executed";
    }

    internal class DataService
    {
        public string FetchData() => "[1,2,3]";
        public string AnalyzeData() => "Average: 2";
        public string GenerateReport() => "Report generated";
    }

    internal class CalculatorService
    {
        public int Add(int a, int b) => a + b;
        public int Multiply(int a, int b) => a * b;
    }
}

/// <summary>
/// Mock chat client that delegates response generation to a function.
/// </summary>
internal class DelegatingMockChatClient : IChatClient
{
    private readonly Func<ChatResponse> _responseFactory;

    public DelegatingMockChatClient(Func<ChatResponse> responseFactory)
    {
        _responseFactory = responseFactory;
    }

    public ChatClientMetadata Metadata => new("mock-delegating-client", null, "mock-1.0");

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_responseFactory());
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public void Dispose() { }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
}
