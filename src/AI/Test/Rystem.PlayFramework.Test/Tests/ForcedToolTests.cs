using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework.Test.Tests;

public sealed class ForcedToolTests
{
    [Fact]
    public async Task SceneExecution_WithForcedTools_FiltersToolsAndAdvancesRequiredTool()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var calculator = new ForcedToolCalculatorService();
        var chatClient = new ForcedToolChatClient();

        services.AddSingleton<IChatClient>(chatClient);
        services.AddSingleton<IForcedToolCalculatorService>(calculator);
        services.AddPlayFramework(builder =>
        {
            builder.AddScene("Calculator", "Calculator scene", scene =>
            {
                scene.WithService<IForcedToolCalculatorService>(service =>
                {
                    service
                        .WithMethod<double>(x => x.Add(default, default), "Add", "Adds two numbers")
                        .WithMethod<double>(x => x.Subtract(default, default), "Subtract", "Subtracts two numbers")
                        .WithMethod<double>(x => x.Multiply(default, default), "Multiply", "Multiplies two numbers");
                });
            });
        });

        var provider = services.BuildServiceProvider();
        var sceneManager = provider.GetRequiredService<IFactory<ISceneManager>>().Create(null)!;

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Scene,
            SceneName = "Calculator",
            ForcedTools =
            [
                new ForcedToolRequest
                {
                    SceneName = "Calculator",
                    ToolName = "Add",
                    SourceType = PlayFrameworkToolSourceType.Service,
                    SourceName = nameof(IForcedToolCalculatorService),
                    MemberName = nameof(IForcedToolCalculatorService.Add)
                },
                new ForcedToolRequest
                {
                    SceneName = "Calculator",
                    ToolName = "Subtract",
                    SourceType = PlayFrameworkToolSourceType.Service,
                    SourceName = nameof(IForcedToolCalculatorService),
                    MemberName = nameof(IForcedToolCalculatorService.Subtract)
                }
            ]
        };

        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Calculate something", settings: settings))
        {
            responses.Add(response);
        }

        Assert.Equal(["Add", "Subtract"], calculator.Calls);
        Assert.Contains(responses, x => x.Status == AiResponseStatus.Completed);

        Assert.Equal(3, chatClient.CapturedOptions.Count);
        Assert.Equal(["Add", "Subtract"], chatClient.CapturedOptions[0].ToolNames);
        Assert.True(chatClient.CapturedOptions[0].IsRequireAny);
        Assert.Null(chatClient.CapturedOptions[0].RequiredFunctionName);

        Assert.Equal(["Add", "Subtract"], chatClient.CapturedOptions[1].ToolNames);
        Assert.Equal("Subtract", chatClient.CapturedOptions[1].RequiredFunctionName);

        Assert.Equal(["Add", "Subtract"], chatClient.CapturedOptions[2].ToolNames);
        Assert.Null(chatClient.CapturedOptions[2].RequiredFunctionName);
        Assert.False(chatClient.CapturedOptions[2].IsRequireAny);
    }

    [Fact]
    public async Task SceneExecution_WithMissingForcedTool_ReturnsErrorBeforeCallingLlm()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IChatClient>(new ForcedToolChatClient());
        services.AddSingleton<IForcedToolCalculatorService, ForcedToolCalculatorService>();

        services.AddPlayFramework(builder =>
        {
            builder.AddScene("Calculator", "Calculator scene", scene =>
            {
                scene.WithService<IForcedToolCalculatorService>(service =>
                {
                    service.WithMethod<double>(x => x.Add(default, default), "Add", "Adds two numbers");
                });
            });
        });

        var provider = services.BuildServiceProvider();
        var sceneManager = provider.GetRequiredService<IFactory<ISceneManager>>().Create(null)!;

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Scene,
            SceneName = "Calculator",
            ForcedTools =
            [
                new ForcedToolRequest
                {
                    SceneName = "Calculator",
                    ToolName = "Subtract",
                    SourceType = PlayFrameworkToolSourceType.Service,
                    SourceName = nameof(IForcedToolCalculatorService),
                    MemberName = nameof(IForcedToolCalculatorService.Subtract)
                }
            ]
        };

        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Calculate something", settings: settings))
        {
            responses.Add(response);
        }

        var error = Assert.Single(responses.Where(x => x.Status == AiResponseStatus.Error));
        Assert.Contains("Forced tools not available", error.Message);
        Assert.Contains("Subtract", error.ErrorMessage);
    }
}

internal interface IForcedToolCalculatorService
{
    double Add(double left, double right);
    double Subtract(double left, double right);
    double Multiply(double left, double right);
}

internal sealed class ForcedToolCalculatorService : IForcedToolCalculatorService
{
    public List<string> Calls { get; } = [];

    public double Add(double left, double right)
    {
        Calls.Add(nameof(Add));
        return left + right;
    }

    public double Subtract(double left, double right)
    {
        Calls.Add(nameof(Subtract));
        return left - right;
    }

    public double Multiply(double left, double right)
    {
        Calls.Add(nameof(Multiply));
        return left * right;
    }
}

internal sealed record ForcedToolOptionsSnapshot(
    List<string> ToolNames,
    string? RequiredFunctionName,
    bool IsRequireAny);

internal sealed class ForcedToolChatClient : IChatClient
{
    private int _callCount;

    public List<ForcedToolOptionsSnapshot> CapturedOptions { get; } = [];

    public ChatClientMetadata Metadata => new("forced-tool-mock", null, "mock-model");

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _callCount++;

        var requiredToolMode = options?.ToolMode as RequiredChatToolMode;
        CapturedOptions.Add(new ForcedToolOptionsSnapshot(
            ToolNames: options?.Tools?.Select(x => x.Name).OrderBy(x => x).ToList() ?? [],
            RequiredFunctionName: requiredToolMode?.RequiredFunctionName,
            IsRequireAny: requiredToolMode != null && string.IsNullOrWhiteSpace(requiredToolMode.RequiredFunctionName)));

        var responseMessage = new ChatMessage(ChatRole.Assistant, "Processing");
        if (_callCount == 1)
        {
            responseMessage.Contents.Add(new FunctionCallContent(
                Guid.NewGuid().ToString(),
                "Add",
                new Dictionary<string, object?>
                {
                    ["left"] = 10,
                    ["right"] = 3
                }));
        }
        else if (_callCount == 2)
        {
            responseMessage.Contents.Add(new FunctionCallContent(
                Guid.NewGuid().ToString(),
                "Subtract",
                new Dictionary<string, object?>
                {
                    ["left"] = 10,
                    ["right"] = 4
                }));
        }
        else
        {
            responseMessage = new ChatMessage(ChatRole.Assistant, "Done");
        }

        return Task.FromResult(new ChatResponse([responseMessage])
        {
            ModelId = "mock-model"
        });
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
