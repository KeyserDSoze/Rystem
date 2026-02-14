using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Rystem.PlayFramework.Test;

/// <summary>
/// Integration tests with real Azure OpenAI.
/// These tests require valid Azure OpenAI credentials in user secrets.
/// </summary>
public sealed class AzureOpenAIIntegrationTests : PlayFrameworkTestBase
{
    private readonly ITestOutputHelper _output;

    public AzureOpenAIIntegrationTests(ITestOutputHelper output) : base(useRealAzureOpenAI: true)
    {
        _output = output;
    }

    protected override void ConfigurePlayFramework(IServiceCollection services)
    {
        // Register calculator service
        services.AddScoped<ICalculatorService, CalculatorService>();

        services.AddPlayFramework(builder =>
        {
            builder
                .Configure(settings =>
                {
                    settings.Planning.Enabled = false; // Start simple
                    settings.Summarization.Enabled = false;
                })
                .AddMainActor("You are a helpful math assistant. When asked to perform calculations, use the available calculator tools.")
                .AddCache(cache => cache.WithMemory())
                .AddScene("Calculator", "Use this scene to perform mathematical calculations. Available operations: add, subtract, multiply, divide.", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<ICalculatorService>(serviceBuilder =>
                        {
                            serviceBuilder
                                .WithMethod(x => x.AddAsync(default, default), "add", "Add two numbers together. Parameters: a (first number), b (second number)")
                                .WithMethod(x => x.SubtractAsync(default, default), "subtract", "Subtract second number from first. Parameters: a (first number), b (second number)")
                                .WithMethod(x => x.MultiplyAsync(default, default), "multiply", "Multiply two numbers. Parameters: a (first number), b (second number)")
                                .WithMethod(x => x.DivideAsync(default, default), "divide", "Divide first number by second. Parameters: a (numerator), b (denominator)");
                        })
                        .WithActors(actorBuilder =>
                        {
                            actorBuilder
                                .AddActor("Always use the calculator tools to perform calculations. Do not calculate manually.")
                                .AddActor("Return the result in a clear format like: 'The result is: [number]'");
                        });
                });
        });
    }

    [Fact(Skip = "Requires Azure OpenAI - Remove Skip attribute to run")]
    public async Task AzureOpenAI_ShouldConnect()
    {
        // Arrange
        var chatClient = ServiceProvider.GetRequiredService<IChatClient>();

        // Act
        var response = await chatClient.GetResponseAsync(new[]
        {
            new ChatMessage(ChatRole.User, "Say 'Hello from Azure OpenAI!' and nothing else.")
        });

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Messages);
        Assert.NotEmpty(response.Messages);

        var messageText = response.Messages.FirstOrDefault()?.Text;
        Assert.NotNull(messageText);

        _output.WriteLine($"Response: {messageText}");
        _output.WriteLine($"Model: {response.ModelId}");
        _output.WriteLine($"Tokens - Input: {response.Usage?.InputTokenCount}, Output: {response.Usage?.OutputTokenCount}");
    }

    [Fact(Skip = "Requires Azure OpenAI - Remove Skip attribute to run")]
    public async Task PlayFramework_WithAzureOpenAI_ShouldExecuteCalculation()
    {
        // Arrange
        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Calculate 15 + 27"))
        {
            responses.Add(response);
            _output.WriteLine($"[{response.Status}] {response.SceneName}: {response.Message}");

            if (response.FunctionName != null)
            {
                _output.WriteLine($"  Function: {response.FunctionName}");
                _output.WriteLine($"  Arguments: {response.FunctionArguments}");
            }

            if (response.Cost.HasValue)
            {
                _output.WriteLine($"  Cost: ${response.Cost:F6} (Total: ${response.TotalCost:F6})");
            }
        }

        // Assert
        Assert.NotEmpty(responses);
        Assert.Contains(responses, r => r.Status == AiResponseStatus.Completed);

        // Check if calculator tool was called
        var toolCalls = responses.Where(r => r.Status == AiResponseStatus.FunctionCompleted).ToList();
        _output.WriteLine($"\nTotal tool calls: {toolCalls.Count}");

        // Check for result containing "42"
        var finalMessages = responses.Where(r => r.Status == AiResponseStatus.Running && !string.IsNullOrEmpty(r.Message)).ToList();
        _output.WriteLine($"\nFinal messages:");
        foreach (var msg in finalMessages)
        {
            _output.WriteLine($"  {msg.Message}");
        }
    }

    [Fact(Skip = "Requires Azure OpenAI - Remove Skip attribute to run")]
    public async Task PlayFramework_WithAzureOpenAI_ShouldHandleMultipleOperations()
    {
        // Arrange
        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Calculate (10 + 5) * 3"))
        {
            responses.Add(response);
            _output.WriteLine($"[{response.Status}] {response.Message ?? response.FunctionName}");
        }

        // Assert
        Assert.NotEmpty(responses);

        var toolCalls = responses.Where(r => r.Status == AiResponseStatus.FunctionCompleted).ToList();
        _output.WriteLine($"\nTotal operations: {toolCalls.Count}");

        // Should have at least 2 operations: addition and multiplication
        Assert.True(toolCalls.Count >= 2, $"Expected at least 2 tool calls, got {toolCalls.Count}");
    }

    [Fact(Skip = "Requires Azure OpenAI - Remove Skip attribute to run")]
    public async Task PlayFramework_ShouldTrackCostsAccurately()
    {
        // Arrange
        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("What is 100 divided by 5?"))
        {
            responses.Add(response);
        }

        // Assert
        var finalResponse = responses.LastOrDefault(r => r.Status == AiResponseStatus.Completed);
        Assert.NotNull(finalResponse);

        _output.WriteLine($"Total Cost: ${finalResponse.TotalCost:F6}");
        _output.WriteLine($"Total Input Tokens: {responses.Sum(r => r.InputTokens ?? 0)}");
        _output.WriteLine($"Total Output Tokens: {responses.Sum(r => r.OutputTokens ?? 0)}");

        Assert.True(finalResponse.TotalCost > 0, "Total cost should be greater than zero");
    }
}
