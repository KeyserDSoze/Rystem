using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework.Test;

/// <summary>
/// Complex tests demonstrating multi-scene orchestration, planning, and dynamic scene selection.
/// </summary>
public sealed class MultiSceneTests : PlayFrameworkTestBase
{
    protected override void ConfigurePlayFramework(IServiceCollection services)
    {
        // Register services
        services.AddScoped<ICalculatorService, CalculatorService>();
        services.AddScoped<IWeatherService, MockWeatherService>();
        services.AddScoped<IDataAnalysisService, MockDataAnalysisService>();
        services.AddScoped<IReportService, MockReportService>();

        // Configure PlayFramework with multiple scenes
        services.AddPlayFramework(builder =>
        {
            builder
                .WithPlanning(settings =>
                {
                    settings.MaxRecursionDepth = 5;
                })
                .AddMainActor(@"You are an intelligent assistant that can:
- Perform mathematical calculations
- Get weather information
- Analyze data
- Generate reports
When a task requires multiple operations, plan the execution carefully.")

                // Scene 1: Calculator
                .AddScene("Calculator", "Use this scene to perform mathematical calculations like addition, subtraction, multiplication, and division", sceneBuilder =>
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
                            actorBuilder.AddActor("Always use calculator tools for precise calculations.");
                        });
                })

                // Scene 2: Weather
                .AddScene("Weather", "Use this scene to get current weather information and temperature for cities", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<IWeatherService>(serviceBuilder =>
                        {
                            serviceBuilder
                                .WithMethod(x => x.GetCurrentWeatherAsync(default!), "getCurrentWeather", "Get current weather conditions for a city")
                                .WithMethod(x => x.GetTemperatureAsync(default!), "getTemperature", "Get temperature in Celsius for a city")
                                .WithMethod(x => x.GetForecastAsync(default!, default), "getForecast", "Get weather forecast for a city for N days");
                        })
                        .WithActors(actorBuilder =>
                        {
                            actorBuilder.AddActor("Provide weather information in a clear, user-friendly format.");
                        });
                })

                // Scene 3: Data Analysis
                .AddScene("DataAnalysis", "Use this scene to analyze data, calculate statistics like average, minimum, maximum, and sum", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<IDataAnalysisService>(serviceBuilder =>
                        {
                            serviceBuilder
                                .WithMethod(x => x.CalculateAverageAsync(default!), "calculateAverage", "Calculate average of a list of numbers")
                                .WithMethod(x => x.FindMinimumAsync(default!), "findMinimum", "Find minimum value in a list")
                                .WithMethod(x => x.FindMaximumAsync(default!), "findMaximum", "Find maximum value in a list")
                                .WithMethod(x => x.CalculateSumAsync(default!), "calculateSum", "Calculate sum of a list of numbers");
                        })
                        .WithActors(actorBuilder =>
                        {
                            actorBuilder.AddActor("Analyze data carefully and provide statistical insights.");
                        });
                })

                // Scene 4: Report Generator
                .AddScene("ReportGenerator", "Use this scene to generate formatted reports, summaries, and documentation", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<IReportService>(serviceBuilder =>
                        {
                            serviceBuilder
                                .WithMethod(x => x.GenerateSummaryAsync(default!), "generateSummary", "Generate a summary from text")
                                .WithMethod(x => x.FormatAsTableAsync(default!, default!), "formatAsTable", "Format data as a table with headers and rows")
                                .WithMethod(x => x.CreateMarkdownReportAsync(default!, default!), "createMarkdownReport", "Create a markdown report with title and content");
                        })
                        .WithActors(actorBuilder =>
                        {
                            actorBuilder.AddActor("Generate well-formatted, professional reports.");
                        });
                });
        });
    }

    [Fact]
    public async Task SceneManager_ShouldRegisterAllScenes()
    {
        // Arrange
        var sceneFactory = ServiceProvider.GetRequiredService<ISceneFactory>();

        // Act
        var sceneNames = sceneFactory.GetSceneNames().ToList();

        // Assert
        Assert.Equal(4, sceneNames.Count);
        Assert.Contains("Calculator", sceneNames);
        Assert.Contains("Weather", sceneNames);
        Assert.Contains("DataAnalysis", sceneNames);
        Assert.Contains("ReportGenerator", sceneNames);
    }

    [Fact]
    public async Task SceneFactory_ShouldCreateAllScenesWithTools()
    {
        // Arrange
        var sceneFactory = ServiceProvider.GetRequiredService<ISceneFactory>();

        // Act & Assert
        var calculatorScene = sceneFactory.Create("Calculator");
        Assert.NotNull(calculatorScene);
        Assert.Equal(4, calculatorScene.GetTools().Count());

        var weatherScene = sceneFactory.Create("Weather");
        Assert.NotNull(weatherScene);
        Assert.Equal(3, weatherScene.GetTools().Count());

        var dataAnalysisScene = sceneFactory.Create("DataAnalysis");
        Assert.NotNull(dataAnalysisScene);
        Assert.Equal(4, dataAnalysisScene.GetTools().Count());

        var reportScene = sceneFactory.Create("ReportGenerator");
        Assert.NotNull(reportScene);
        Assert.Equal(3, reportScene.GetTools().Count());
    }

    [Fact]
    public async Task WeatherService_ShouldReturnMockData()
    {
        // Arrange
        var weatherService = ServiceProvider.GetRequiredService<IWeatherService>();

        // Act
        var weather = await weatherService.GetCurrentWeatherAsync("Milan");
        var temperature = await weatherService.GetTemperatureAsync("Rome");
        var forecast = await weatherService.GetForecastAsync("Venice", 3);

        // Assert
        Assert.Contains("Milan", weather);
        Assert.NotEqual(0, temperature);
        Assert.Contains("forecast", forecast.ToLower());
    }

    [Fact]
    public async Task DataAnalysisService_ShouldCalculateStatistics()
    {
        // Arrange
        var dataService = ServiceProvider.GetRequiredService<IDataAnalysisService>();
        var numbers = new[] { 10.0, 20.0, 30.0, 40.0, 50.0 };

        // Act
        var average = await dataService.CalculateAverageAsync(numbers);
        var min = await dataService.FindMinimumAsync(numbers);
        var max = await dataService.FindMaximumAsync(numbers);
        var sum = await dataService.CalculateSumAsync(numbers);

        // Assert
        Assert.Equal(30.0, average);
        Assert.Equal(10.0, min);
        Assert.Equal(50.0, max);
        Assert.Equal(150.0, sum);
    }

    // Integration tests that require LLM
    [Fact(Skip = "Requires LLM - Enable for integration testing")]
    public async Task MultiScene_WithPlanning_ShouldCalculateAndAnalyze()
    {
        // This test demonstrates:
        // 1. LLM creates a plan with multiple scenes
        // 2. Calculator scene calculates values
        // 3. DataAnalysis scene analyzes the results

        // Arrange
        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync(
            "Calculate these values: 10+5, 20+8, 30+12. Then find the average of the three results."))
        {
            responses.Add(response);
        }

        // Assert
        Assert.NotEmpty(responses);
        Assert.Contains(responses, r => r.Status == AiResponseStatus.Planning); // Planning was used
        Assert.Contains(responses, r => r.SceneName == "Calculator"); // Calculator scene executed
        Assert.Contains(responses, r => r.SceneName == "DataAnalysis"); // DataAnalysis scene executed
        Assert.Contains(responses, r => r.Status == AiResponseStatus.Completed);

        // The average should be: (15 + 28 + 42) / 3 = 28.33
        var finalResponse = responses.LastOrDefault(r => r.Status == AiResponseStatus.Running);
        Assert.NotNull(finalResponse?.Message);
        Assert.Contains("28", finalResponse.Message); // Should mention the average
    }

    [Fact(Skip = "Requires LLM - Enable for integration testing")]
    public async Task MultiScene_WithDynamicSelection_ShouldChooseWeatherScene()
    {
        // This test demonstrates:
        // 1. LLM dynamically selects the Weather scene based on user input
        // 2. Weather scene executes appropriate tool

        // Arrange
        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync(
            "What's the temperature in Paris?"))
        {
            responses.Add(response);
        }

        // Assert
        Assert.NotEmpty(responses);
        Assert.Contains(responses, r => r.SceneName == "Weather");
        Assert.Contains(responses, r => r.FunctionName == "getTemperature");
        Assert.Contains(responses, r => r.Status == AiResponseStatus.Completed);
    }

    [Fact(Skip = "Requires LLM - Enable for integration testing")]
    public async Task MultiScene_ComplexWorkflow_WeatherThenCalculate()
    {
        // This test demonstrates a complex workflow:
        // 1. Get temperatures for multiple cities (Weather scene)
        // 2. Calculate average temperature (DataAnalysis scene)
        // 3. Generate a report (ReportGenerator scene)

        // Arrange
        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync(
            "Get the temperatures for Rome, Milan, and Naples. Calculate the average temperature and create a summary report."))
        {
            responses.Add(response);
        }

        // Assert
        Assert.NotEmpty(responses);

        // Should execute multiple scenes
        var executedScenes = responses.Where(r => r.SceneName != null).Select(r => r.SceneName).Distinct().ToList();
        Assert.Contains("Weather", executedScenes);
        Assert.True(executedScenes.Count >= 2, "Should execute at least 2 different scenes");

        // Should complete successfully
        Assert.Contains(responses, r => r.Status == AiResponseStatus.Completed);
    }

    [Fact(Skip = "Requires LLM - Enable for integration testing")]
    public async Task MultiScene_WithToolCalling_ShouldExecuteMultipleTools()
    {
        // This test demonstrates multiple tool calls within a single scene

        // Arrange
        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync(
            "Calculate: (15 + 25) * 2 - 10"))
        {
            responses.Add(response);
        }

        // Assert
        Assert.NotEmpty(responses);

        // Should call multiple calculator tools
        var toolCalls = responses.Where(r => r.FunctionName != null).Select(r => r.FunctionName).ToList();
        Assert.True(toolCalls.Count >= 3, $"Should execute at least 3 tool calls, got {toolCalls.Count}");

        // Final result should be: (15 + 25) * 2 - 10 = 40 * 2 - 10 = 80 - 10 = 70
        var finalResponse = responses.LastOrDefault(r => r.Status == AiResponseStatus.Running);
        Assert.NotNull(finalResponse?.Message);
        Assert.Contains("70", finalResponse.Message);
    }

    [Fact(Skip = "Requires LLM - Enable for integration testing")]
    public async Task MultiScene_PlanningEnabled_ShouldCreateExecutionPlan()
    {
        // This test verifies that planning is actually used

        // Arrange
        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync(
            "Get weather for London, then calculate if the temperature is above 20 degrees."))
        {
            responses.Add(response);
        }

        // Assert
        Assert.NotEmpty(responses);
        Assert.Contains(responses, r => r.Status == AiResponseStatus.Planning);
        Assert.Contains(responses, r => r.Status == AiResponseStatus.Completed);
    }

    [Fact(Skip = "Requires LLM - Enable for integration testing")]
    public async Task MultiScene_ResponseStream_ShouldProvideDetailedStatus()
    {
        // This test verifies the response stream provides detailed execution status

        // Arrange
        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();

        // Act
        var statuses = new List<AiResponseStatus>();
        await foreach (var response in sceneManager.ExecuteAsync("Calculate 100 / 4"))
        {
            statuses.Add(response.Status);
        }

        // Assert
        Assert.Contains(AiResponseStatus.Initializing, statuses);
        Assert.Contains(AiResponseStatus.ExecutingScene, statuses);
        Assert.Contains(AiResponseStatus.FunctionRequest, statuses);
        Assert.Contains(AiResponseStatus.FunctionCompleted, statuses);
        Assert.Contains(AiResponseStatus.Completed, statuses);
    }
}

// Mock Services

/// <summary>
/// Weather service for testing multi-scene scenarios.
/// </summary>
public interface IWeatherService
{
    Task<string> GetCurrentWeatherAsync(string city);
    Task<double> GetTemperatureAsync(string city);
    Task<string> GetForecastAsync(string city, int days);
}

public sealed class MockWeatherService : IWeatherService
{
    private readonly Dictionary<string, double> _temperatures = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Milan"] = 18.5,
        ["Rome"] = 22.3,
        ["Naples"] = 24.1,
        ["Venice"] = 17.8,
        ["Florence"] = 20.6,
        ["Paris"] = 15.2,
        ["London"] = 12.8,
        ["Berlin"] = 14.3,
        ["Madrid"] = 25.7,
        ["Barcelona"] = 23.4
    };

    public Task<string> GetCurrentWeatherAsync(string city)
    {
        var temp = _temperatures.GetValueOrDefault(city, 20.0);
        var condition = temp > 22 ? "Sunny" : temp > 15 ? "Partly Cloudy" : "Cloudy";
        return Task.FromResult($"Weather in {city}: {condition}, {temp}°C");
    }

    public Task<double> GetTemperatureAsync(string city)
    {
        return Task.FromResult(_temperatures.GetValueOrDefault(city, 20.0));
    }

    public Task<string> GetForecastAsync(string city, int days)
    {
        var baseTemp = _temperatures.GetValueOrDefault(city, 20.0);
        var forecast = new List<string>();
        for (int i = 1; i <= days; i++)
        {
            var dayTemp = baseTemp + Random.Shared.Next(-3, 4);
            forecast.Add($"Day {i}: {dayTemp}°C");
        }
        return Task.FromResult($"Forecast for {city} ({days} days):\n{string.Join("\n", forecast)}");
    }
}

/// <summary>
/// Data analysis service for testing multi-scene scenarios.
/// </summary>
public interface IDataAnalysisService
{
    Task<double> CalculateAverageAsync(double[] numbers);
    Task<double> FindMinimumAsync(double[] numbers);
    Task<double> FindMaximumAsync(double[] numbers);
    Task<double> CalculateSumAsync(double[] numbers);
}

public sealed class MockDataAnalysisService : IDataAnalysisService
{
    public Task<double> CalculateAverageAsync(double[] numbers)
    {
        if (numbers.Length == 0) throw new ArgumentException("Array cannot be empty");
        return Task.FromResult(numbers.Average());
    }

    public Task<double> FindMinimumAsync(double[] numbers)
    {
        if (numbers.Length == 0) throw new ArgumentException("Array cannot be empty");
        return Task.FromResult(numbers.Min());
    }

    public Task<double> FindMaximumAsync(double[] numbers)
    {
        if (numbers.Length == 0) throw new ArgumentException("Array cannot be empty");
        return Task.FromResult(numbers.Max());
    }

    public Task<double> CalculateSumAsync(double[] numbers)
    {
        return Task.FromResult(numbers.Sum());
    }
}

/// <summary>
/// Report generation service for testing multi-scene scenarios.
/// </summary>
public interface IReportService
{
    Task<string> GenerateSummaryAsync(string text);
    Task<string> FormatAsTableAsync(string[] headers, string[][] rows);
    Task<string> CreateMarkdownReportAsync(string title, string content);
}

public sealed class MockReportService : IReportService
{
    public Task<string> GenerateSummaryAsync(string text)
    {
        var words = text.Split(' ');
        var summary = words.Length > 20
            ? string.Join(" ", words.Take(20)) + "..."
            : text;
        return Task.FromResult($"Summary: {summary}");
    }

    public Task<string> FormatAsTableAsync(string[] headers, string[][] rows)
    {
        var table = new System.Text.StringBuilder();
        table.AppendLine("| " + string.Join(" | ", headers) + " |");
        table.AppendLine("|" + string.Join("|", headers.Select(_ => "---")) + "|");
        foreach (var row in rows)
        {
            table.AppendLine("| " + string.Join(" | ", row) + " |");
        }
        return Task.FromResult(table.ToString());
    }

    public Task<string> CreateMarkdownReportAsync(string title, string content)
    {
        var report = $"# {title}\n\n{content}\n\n---\n*Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC*";
        return Task.FromResult(report);
    }
}
