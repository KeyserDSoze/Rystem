using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework.Test;

/// <summary>
/// Interactive demonstration of multi-scene orchestration.
/// Run these tests manually with Azure OpenAI configured to see detailed execution.
/// </summary>
public sealed class InteractiveMultiSceneDemo : PlayFrameworkTestBase
{
    public InteractiveMultiSceneDemo() : base(useRealAzureOpenAI: true)
    {
    }

    protected override void ConfigurePlayFramework(IServiceCollection services)
    {
        // Register services
        services.AddScoped<ICalculatorService, CalculatorService>();
        services.AddScoped<IWeatherService, MockWeatherService>();
        services.AddScoped<IDataAnalysisService, MockDataAnalysisService>();
        services.AddScoped<IReportService, MockReportService>();

        // Configure PlayFramework with verbose logging
        services.AddPlayFramework(builder =>
        {
            builder
                .WithPlanning()
                .WithSummarization(settings =>
                {
                    settings.CharacterThreshold = 20_000;
                })
                .Configure(settings =>
                {
                    settings.Planning.Enabled = true;
                    settings.Summarization.Enabled = false;
                })
                .AddMainActor(@"You are an expert AI assistant with access to multiple specialized tools.

Available capabilities:
- Calculator: Perform mathematical operations (add, subtract, multiply, divide)
- Weather: Get weather information and temperatures for cities worldwide
- DataAnalysis: Calculate statistics (average, min, max, sum) on data sets
- ReportGenerator: Create formatted reports, summaries, and tables

When handling complex requests:
1. Break down the task into steps
2. Use appropriate scenes for each step
3. Pass data between scenes efficiently
4. Provide clear, comprehensive responses

Always explain your reasoning and show intermediate steps.")

                // Calculator Scene
                .AddScene("Calculator", "Mathematical calculations: addition, subtraction, multiplication, division", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<ICalculatorService>(serviceBuilder =>
                        {
                            serviceBuilder
                                .WithMethod(x => x.AddAsync(default, default), "add", "Add two numbers: add(a, b)")
                                .WithMethod(x => x.SubtractAsync(default, default), "subtract", "Subtract: subtract(a, b) = a - b")
                                .WithMethod(x => x.MultiplyAsync(default, default), "multiply", "Multiply: multiply(a, b)")
                                .WithMethod(x => x.DivideAsync(default, default), "divide", "Divide: divide(a, b) = a / b");
                        })
                        .WithActors(actorBuilder =>
                        {
                            actorBuilder.AddActor("Use calculator tools for all numeric operations. Show step-by-step calculations.");
                        });
                })

                // Weather Scene
                .AddScene("Weather", "Weather information: current conditions, temperature, forecasts for any city", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<IWeatherService>(serviceBuilder =>
                        {
                            serviceBuilder
                                .WithMethod(x => x.GetCurrentWeatherAsync(default!), "getCurrentWeather", "Get current weather: getCurrentWeather(city)")
                                .WithMethod(x => x.GetTemperatureAsync(default!), "getTemperature", "Get temperature in Celsius: getTemperature(city)")
                                .WithMethod(x => x.GetForecastAsync(default!, default), "getForecast", "Get N-day forecast: getForecast(city, days)");
                        })
                        .WithActors(actorBuilder =>
                        {
                            actorBuilder.AddActor("Provide weather information clearly. Include temperature units (°C).");
                        });
                })

                // Data Analysis Scene
                .AddScene("DataAnalysis", "Statistical analysis: calculate average, minimum, maximum, sum of number arrays", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<IDataAnalysisService>(serviceBuilder =>
                        {
                            serviceBuilder
                                .WithMethod(x => x.CalculateAverageAsync(default!), "calculateAverage", "Average of numbers: calculateAverage([n1, n2, ...])")
                                .WithMethod(x => x.FindMinimumAsync(default!), "findMinimum", "Minimum value: findMinimum([numbers])")
                                .WithMethod(x => x.FindMaximumAsync(default!), "findMaximum", "Maximum value: findMaximum([numbers])")
                                .WithMethod(x => x.CalculateSumAsync(default!), "calculateSum", "Sum of values: calculateSum([numbers])");
                        })
                        .WithActors(actorBuilder =>
                        {
                            actorBuilder.AddActor("Provide statistical insights. Explain the significance of calculated values.");
                        });
                })

                // Report Generator Scene
                .AddScene("ReportGenerator", "Generate formatted reports, summaries, markdown tables, and documentation", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<IReportService>(serviceBuilder =>
                        {
                            serviceBuilder
                                .WithMethod(x => x.GenerateSummaryAsync(default!), "generateSummary", "Summarize text: generateSummary(text)")
                                .WithMethod(x => x.FormatAsTableAsync(default!, default!), "formatAsTable", "Create markdown table: formatAsTable(headers, rows)")
                                .WithMethod(x => x.CreateMarkdownReportAsync(default!, default!), "createMarkdownReport", "Full report: createMarkdownReport(title, content)");
                        })
                        .WithActors(actorBuilder =>
                        {
                            actorBuilder.AddActor("Generate professional, well-formatted reports. Use markdown formatting.");
                        });
                });
        });
    }

    /// <summary>
    /// Demo: Simple calculation with detailed step-by-step output
    /// </summary>
    [Fact(Skip = "Interactive demo - Remove Skip to run with Azure OpenAI")]
    public async Task Demo1_SimpleCalculation_StepByStep()
    {
        // Arrange
        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();

        Console.WriteLine("\n=== DEMO 1: Simple Calculation ===");
        Console.WriteLine("Request: Calculate (25 + 15) * 2\n");

        // Act
        await foreach (var response in sceneManager.ExecuteAsync("Calculate (25 + 15) * 2"))
        {
            Console.WriteLine($"[{response.Status}] {response.Message}");

            if (response.SceneName != null)
                Console.WriteLine($"  └─ Scene: {response.SceneName}");

            if (response.FunctionName != null)
                Console.WriteLine($"  └─ Tool: {response.FunctionName}");

            if (response.Cost.HasValue)
                Console.WriteLine($"  └─ Cost: ${response.Cost:F4} | Total: ${response.TotalCost:F4}");

            Console.WriteLine();
        }
    }

    /// <summary>
    /// Demo: Multi-scene workflow with weather and calculations
    /// </summary>
    [Fact(Skip = "Interactive demo - Remove Skip to run with Azure OpenAI")]
    public async Task Demo2_WeatherAnalysis_MultiScene()
    {
        // Arrange
        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();

        Console.WriteLine("\n=== DEMO 2: Weather Analysis (Multi-Scene) ===");
        Console.WriteLine("Request: Get temperatures for Rome, Milan, Venice. Calculate average and identify warmest city.\n");

        // Act
        var scenesUsed = new HashSet<string>();
        var toolsCalled = new List<string>();

        await foreach (var response in sceneManager.ExecuteAsync(
            "Get the current temperatures for Rome, Milan, and Venice. " +
            "Then calculate the average temperature and tell me which city is the warmest."))
        {
            Console.WriteLine($"[{response.Status,-25}] {response.Message}");

            if (response.SceneName != null)
            {
                scenesUsed.Add(response.SceneName);
                Console.WriteLine($"  └─ Scene: {response.SceneName}");
            }

            if (response.FunctionName != null)
            {
                toolsCalled.Add(response.FunctionName);
                Console.WriteLine($"  └─ Tool: {response.FunctionName}");
            }

            Console.WriteLine();
        }

        Console.WriteLine($"\n📊 Summary:");
        Console.WriteLine($"   Scenes used: {string.Join(", ", scenesUsed)}");
        Console.WriteLine($"   Tools called: {toolsCalled.Count} ({string.Join(", ", toolsCalled)})");
    }

    /// <summary>
    /// Demo: Complex workflow requiring planning
    /// </summary>
    [Fact(Skip = "Interactive demo - Remove Skip to run with Azure OpenAI")]
    public async Task Demo3_ComplexWorkflow_WithPlanning()
    {
        // Arrange
        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();

        Console.WriteLine("\n=== DEMO 3: Complex Workflow with Planning ===");
        Console.WriteLine("Request: Multi-step analysis with report generation\n");

        var request = @"
I need a comprehensive analysis:
1. Calculate these values: 100/4, 50*2, 75+25
2. Find the average, minimum, and maximum of those three results
3. Get the temperature for Paris
4. Create a markdown report that includes:
   - A table with the calculated values
   - Statistical analysis
   - Weather information for Paris
   - A summary conclusion
";

        Console.WriteLine($"Request:\n{request}\n");

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync(request))
        {
            responses.Add(response);

            Console.WriteLine($"[{response.Status,-25}] {response.Message}");

            if (response.SceneName != null)
                Console.WriteLine($"  └─ Scene: {response.SceneName}");

            if (response.FunctionName != null)
                Console.WriteLine($"  └─ Tool: {response.FunctionName}");

            if (response.InputTokens.HasValue || response.OutputTokens.HasValue)
                Console.WriteLine($"  └─ Tokens: {response.InputTokens ?? 0} in, {response.OutputTokens ?? 0} out");

            Console.WriteLine();
        }

        // Summary
        Console.WriteLine($"\n📊 Execution Summary:");
        Console.WriteLine($"   Total responses: {responses.Count}");
        Console.WriteLine($"   Scenes used: {responses.Where(r => r.SceneName != null).Select(r => r.SceneName).Distinct().Count()}");
        Console.WriteLine($"   Tools called: {responses.Count(r => r.FunctionName != null)}");
        Console.WriteLine($"   Total cost: ${responses.Last().TotalCost:F4}");

        var planningResponse = responses.FirstOrDefault(r => r.Status == AiResponseStatus.Planning);
        if (planningResponse != null)
            Console.WriteLine($"   Planning: ✅ Used");
    }

    /// <summary>
    /// Demo: Error handling and recovery
    /// </summary>
    [Fact(Skip = "Interactive demo - Remove Skip to run with Azure OpenAI")]
    public async Task Demo4_ErrorHandling_DivisionByZero()
    {
        // Arrange
        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();

        Console.WriteLine("\n=== DEMO 4: Error Handling ===");
        Console.WriteLine("Request: Calculate 100 / 0 (will trigger error)\n");

        // Act
        await foreach (var response in sceneManager.ExecuteAsync("Calculate 100 divided by 0"))
        {
            Console.WriteLine($"[{response.Status,-25}] {response.Message}");

            if (response.ErrorMessage != null)
                Console.WriteLine($"  ⚠️ Error: {response.ErrorMessage}");

            if (response.FunctionName != null)
                Console.WriteLine($"  └─ Tool: {response.FunctionName}");

            Console.WriteLine();
        }
    }

    /// <summary>
    /// Demo: Dynamic scene selection
    /// </summary>
    [Fact(Skip = "Interactive demo - Remove Skip to run with Azure OpenAI")]
    public async Task Demo5_DynamicSceneSelection()
    {
        // Arrange
        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();

        Console.WriteLine("\n=== DEMO 5: Dynamic Scene Selection ===");

        var requests = new[]
        {
            "What's 15 + 27?",
            "What's the temperature in London?",
            "Find the average of 10, 20, 30, 40, 50",
            "Create a summary of this text: The PlayFramework is an advanced AI orchestration system..."
        };

        foreach (var request in requests)
        {
            Console.WriteLine($"\nRequest: {request}");
            Console.WriteLine(new string('-', 80));

            string? selectedScene = null;

            await foreach (var response in sceneManager.ExecuteAsync(request))
            {
                if (response.SceneName != null && selectedScene == null)
                {
                    selectedScene = response.SceneName;
                    Console.WriteLine($"✅ Selected Scene: {selectedScene}");
                }

                if (response.Status == AiResponseStatus.Running)
                    Console.WriteLine($"📝 Response: {response.Message}");
            }

            Console.WriteLine();
        }
    }

    /// <summary>
    /// Demo: Response streaming visualization
    /// </summary>
    [Fact(Skip = "Interactive demo - Remove Skip to run with Azure OpenAI")]
    public async Task Demo6_ResponseStreamingVisualization()
    {
        // Arrange
        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();

        Console.WriteLine("\n=== DEMO 6: Response Streaming Visualization ===");
        Console.WriteLine("Request: Calculate (10 + 20) * 3\n");

        // Act
        var startTime = DateTime.UtcNow;
        var stepNumber = 0;

        await foreach (var response in sceneManager.ExecuteAsync("Calculate (10 + 20) * 3"))
        {
            stepNumber++;
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

            Console.WriteLine($"Step {stepNumber,2} [{elapsed,7:F0}ms] {GetStatusIcon(response.Status)} {response.Status}");

            if (!string.IsNullOrEmpty(response.Message))
                Console.WriteLine($"         {response.Message}");

            if (response.SceneName != null)
                Console.WriteLine($"         🎬 Scene: {response.SceneName}");

            if (response.FunctionName != null)
                Console.WriteLine($"         🔧 Tool: {response.FunctionName}");

            if (response.Cost.HasValue)
                Console.WriteLine($"         💰 Cost: ${response.Cost:F4}");

            Console.WriteLine();
        }

        Console.WriteLine($"⏱️  Total execution time: {(DateTime.UtcNow - startTime).TotalMilliseconds:F0}ms");
    }

    private static string GetStatusIcon(AiResponseStatus status)
    {
        return status switch
        {
            AiResponseStatus.Initializing => "🔄",
            AiResponseStatus.Planning => "📋",
            AiResponseStatus.ExecutingScene => "🎬",
            AiResponseStatus.FunctionRequest => "🔧",
            AiResponseStatus.FunctionCompleted => "✅",
            AiResponseStatus.Running => "▶️",
            AiResponseStatus.Completed => "🎉",
            AiResponseStatus.Error => "❌",
            _ => "•"
        };
    }
}
