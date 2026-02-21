using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework.Test;

/// <summary>
/// Tests for PlayFramework with a simple calculator scene.
/// </summary>
public sealed class SimpleCalculatorTests : PlayFrameworkTestBase
{
    protected override void ConfigurePlayFramework(IServiceCollection services)
    {
        // Register calculator service
        services.AddScoped<ICalculatorService, CalculatorService>();

        // Configure PlayFramework
        services.AddPlayFramework(builder =>
        {
            builder
                .AddMainActor("You are a helpful math assistant. When asked to perform calculations, use the available tools.")
                .AddScene("Calculator", "Use this scene to perform mathematical calculations", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<ICalculatorService>(serviceBuilder =>
                        {
                            serviceBuilder
                                .WithMethod(x => x.AddAsync(default, default), "add", "Add two numbers together")
                                .WithMethod(x => x.SubtractAsync(default, default), "subtract", "Subtract second number from first")
                                .WithMethod(x => x.MultiplyAsync(default, default), "multiply", "Multiply two numbers")
                                .WithMethod(x => x.DivideAsync(default, default), "divide", "Divide first number by second");
                        })
                        .WithActors(actorBuilder =>
                        {
                            actorBuilder
                                .AddActor("Always use the calculator tools to perform calculations.")
                                .AddActor("Return results in a clear, formatted way.");
                        });
                });
        });
    }

    [Fact]
    public async Task SceneManager_ShouldBeRegistered()
    {
        // Arrange & Act
        var sceneManager = ServiceProvider.GetService<ISceneManager>();

        // Assert
        Assert.NotNull(sceneManager);
    }

    [Fact]
    public async Task SceneFactory_ShouldCreateCalculatorScene()
    {
        // Arrange
        var sceneFactory = ServiceProvider.GetRequiredService<ISceneFactory>();

        // Act
        var scene = sceneFactory.TryGetScene("Calculator");

        // Assert
        Assert.NotNull(scene);
        Assert.Equal("Calculator", scene.Name);
        Assert.NotEmpty(scene.Tools);
        Assert.Equal(4, scene.Tools.Count); // add, subtract, multiply, divide
    }

    [Fact]
    public async Task Calculator_ShouldAddNumbers()
    {
        // Arrange
        var calculator = ServiceProvider.GetRequiredService<ICalculatorService>();

        // Act
        var result = await calculator.AddAsync(5, 3);

        // Assert
        Assert.Equal(8, result);
    }

    // This test requires actual LLM integration
    [Fact(Skip = "Requires LLM - Enable manually for integration testing")]
    public async Task PlayFramework_ShouldExecuteCalculatorScene()
    {
        // Arrange
        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Calculate 10 + 5"))
        {
            responses.Add(response);
        }

        // Assert
        Assert.NotEmpty(responses);
        Assert.Contains(responses, r => r.Status == AiResponseStatus.Completed);

        // Check if we got a response mentioning "15"
        var finalResponse = responses.LastOrDefault(r => r.Status == AiResponseStatus.Running);
        Assert.NotNull(finalResponse);
        Assert.NotNull(finalResponse.Message);
        Assert.Contains("15", finalResponse.Message);
    }
}

/// <summary>
/// Simple calculator service for testing.
/// </summary>
public interface ICalculatorService
{
    Task<double> AddAsync(double a, double b);
    Task<double> SubtractAsync(double a, double b);
    Task<double> MultiplyAsync(double a, double b);
    Task<double> DivideAsync(double a, double b);
}

public sealed class CalculatorService : ICalculatorService
{
    public Task<double> AddAsync(double a, double b) => Task.FromResult(a + b);
    public Task<double> SubtractAsync(double a, double b) => Task.FromResult(a - b);
    public Task<double> MultiplyAsync(double a, double b) => Task.FromResult(a * b);
    public Task<double> DivideAsync(double a, double b)
    {
        if (b == 0)
            throw new ArgumentException("Cannot divide by zero", nameof(b));
        return Task.FromResult(a / b);
    }
}
