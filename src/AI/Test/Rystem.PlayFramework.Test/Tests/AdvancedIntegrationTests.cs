using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace Rystem.PlayFramework.Test;

/// <summary>
/// Advanced integration tests demonstrating complex real-world scenarios with:
/// - Multi-step business workflows
/// - Data analysis pipelines
/// - Conditional logic based on results
/// - Error handling and recovery
/// - All 4 scenes orchestrated together
/// - Realistic business intelligence use cases
/// </summary>
public sealed class AdvancedIntegrationTests : PlayFrameworkTestBase
{
    public AdvancedIntegrationTests() : base(useRealAzureOpenAI: true)
    {
    }

    protected override void ConfigurePlayFramework(IServiceCollection services)
    {
        // Register all services
        services.AddScoped<ICalculatorService, CalculatorService>();
        services.AddScoped<IWeatherService, MockWeatherService>();
        services.AddScoped<IDataAnalysisService, MockDataAnalysisService>();
        services.AddScoped<IReportService, MockReportService>();
        services.AddScoped<ISalesService, MockSalesService>();
        services.AddScoped<IBusinessAnalyticsService, MockBusinessAnalyticsService>();

        // Configure PlayFramework with caching and summarization
        services.AddPlayFramework(builder =>
        {
            builder
                .WithPlanning(settings =>
                {
                    settings.MaxRecursionDepth = 3;
                })
                .AddMainActor(@"You are an expert business intelligence AI assistant.

Your capabilities span multiple domains:
- **Financial Analysis**: Calculate revenues, profits, growth rates, forecasts
- **Data Analytics**: Statistical analysis, trend detection, anomaly identification
- **Weather Intelligence**: Climate data correlation with business metrics
- **Report Generation**: Executive summaries, detailed reports, visualizations

When handling complex requests:
1. Break down into logical phases
2. Execute calculations with precision
3. Analyze data for insights
4. Correlate different data sources
5. Generate comprehensive reports
6. Provide actionable recommendations

Always explain your reasoning and show intermediate results.")

                // Calculator Scene (Enhanced)
                .AddScene("Calculator", "Advanced mathematical operations: arithmetic, percentages, compound calculations, financial formulas",
                sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<ICalculatorService>(serviceBuilder =>
                        {
                            serviceBuilder
                                .WithMethod(x => x.AddAsync(default, default), "add", "Add numbers: add(a, b)")
                                .WithMethod(x => x.SubtractAsync(default, default), "subtract", "Subtract: subtract(a, b)")
                                .WithMethod(x => x.MultiplyAsync(default, default), "multiply", "Multiply: multiply(a, b)")
                                .WithMethod(x => x.DivideAsync(default, default), "divide", "Divide: divide(a, b)");
                        })
                        .WithActors(actorBuilder =>
                        {
                            actorBuilder
                                .AddActor("Use high precision for financial calculations.")
                                .AddActor("Always validate inputs to prevent division by zero.");
                        });
                })

                // Weather Scene
                .AddScene("Weather", "Weather and climate data: temperatures, forecasts, seasonal patterns", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<IWeatherService>(serviceBuilder =>
                        {
                            serviceBuilder
                                .WithMethod(x => x.GetCurrentWeatherAsync(default!), "getCurrentWeather", "Current weather: getCurrentWeather(city)")
                                .WithMethod(x => x.GetTemperatureAsync(default!), "getTemperature", "Temperature in Celsius: getTemperature(city)")
                                .WithMethod(x => x.GetForecastAsync(default!, default), "getForecast", "N-day forecast: getForecast(city, days)");
                        })
                        .WithActors(actorBuilder =>
                        {
                            actorBuilder.AddActor("Provide weather insights and potential business impacts.");
                        });
                })

                // Data Analysis Scene (Enhanced)
                .AddScene("DataAnalysis", "Advanced statistical analysis: averages, extremes, trends, variance, correlations", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<IDataAnalysisService>(serviceBuilder =>
                        {
                            serviceBuilder
                                .WithMethod(x => x.CalculateAverageAsync(default!), "calculateAverage", "Average: calculateAverage([numbers])")
                                .WithMethod(x => x.FindMinimumAsync(default!), "findMinimum", "Minimum: findMinimum([numbers])")
                                .WithMethod(x => x.FindMaximumAsync(default!), "findMaximum", "Maximum: findMaximum([numbers])")
                                .WithMethod(x => x.CalculateSumAsync(default!), "calculateSum", "Sum: calculateSum([numbers])");
                        })
                        .WithActors(actorBuilder =>
                        {
                            actorBuilder
                                .AddActor("Provide statistical context and interpret results.")
                                .AddActor("Identify trends, patterns, and anomalies in data.");
                        });
                })

                // Report Generator Scene
                .AddScene("ReportGenerator", "Professional document generation: executive summaries, detailed reports, data tables, markdown formatting", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<IReportService>(serviceBuilder =>
                        {
                            serviceBuilder
                                .WithMethod(x => x.GenerateSummaryAsync(default!), "generateSummary", "Summary: generateSummary(text)")
                                .WithMethod(x => x.FormatAsTableAsync(default!, default!), "formatAsTable", "Table: formatAsTable(headers, rows)")
                                .WithMethod(x => x.CreateMarkdownReportAsync(default!, default!), "createMarkdownReport", "Report: createMarkdownReport(title, content)");
                        })
                        .WithActors(actorBuilder =>
                        {
                            actorBuilder.AddActor("Generate executive-quality reports with actionable insights.");
                        });
                })

                // Sales Analysis Scene (NEW)
                .AddScene("SalesAnalysis", "Sales data analysis: retrieve sales figures, calculate growth, analyze performance", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<ISalesService>(serviceBuilder =>
                        {
                            serviceBuilder
                                .WithMethod(x => x.GetMonthlySalesAsync(default, default), "getMonthlySales", "Monthly sales: getMonthlySales(year, month)")
                                .WithMethod(x => x.GetYearlySalesAsync(default), "getYearlySales", "Yearly sales: getYearlySales(year)")
                                .WithMethod(x => x.GetTopProductsAsync(default), "getTopProducts", "Top N products: getTopProducts(count)");
                        })
                        .WithActors(actorBuilder =>
                        {
                            actorBuilder.AddActor("Provide sales insights and growth trends.");
                        });
                })

                // Business Analytics Scene (NEW)
                .AddScene("BusinessAnalytics", "Advanced business metrics: growth rate calculation, trend analysis, forecasting, KPI tracking", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<IBusinessAnalyticsService>(serviceBuilder =>
                        {
                            serviceBuilder
                                .WithMethod(x => x.CalculateGrowthRateAsync(default, default), "calculateGrowthRate", "Growth %: calculateGrowthRate(oldValue, newValue)")
                                .WithMethod(x => x.CalculatePercentageAsync(default, default), "calculatePercentage", "Percentage: calculatePercentage(part, total)")
                                .WithMethod(x => x.DetectTrendAsync(default!), "detectTrend", "Trend: detectTrend([values])");
                        })
                        .WithActors(actorBuilder =>
                        {
                            actorBuilder.AddActor("Provide strategic business recommendations based on data.");
                        });
                });
        });
    }

    /// <summary>
    /// Test 1: Complex Business Analysis Workflow
    /// Scenario: Analyze quarterly sales performance with weather correlation and executive report
    /// </summary>
    [Fact(Skip = "Advanced integration test - Remove Skip to run with Azure OpenAI")]
    public async Task Advanced_QuarterlySalesAnalysis_WithWeatherCorrelation()
    {
        /*
         * SCENARIO: Quarterly Business Review
         * 
         * Steps:
         * 1. Retrieve Q1 sales data (Jan, Feb, Mar)
         * 2. Calculate total quarterly revenue
         * 3. Calculate month-over-month growth rates
         * 4. Get weather data for key cities in those months
         * 5. Analyze correlation between weather and sales
         * 6. Generate executive summary report
         * 
         * Expected:
         * - Multiple scene orchestration (Sales, Calculator, Weather, DataAnalysis, ReportGenerator)
         * - 15+ tool calls
         * - Conditional logic based on growth rates
         * - Professional markdown report as output
         */

        // Arrange
        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();

        var request = @"
Perform a comprehensive Q1 2024 sales analysis:

1. **Sales Data Collection**
   - Get monthly sales for January, February, and March 2024
   - Calculate total Q1 revenue

2. **Growth Analysis**
   - Calculate month-over-month growth rates
   - Identify best and worst performing months

3. **Statistical Analysis**
   - Calculate average monthly sales
   - Find min and max sales months
   - Calculate standard deviation if possible

4. **Weather Correlation** (Hypothesis: Weather impacts sales)
   - Get average temperatures for Milan, Rome, and Paris in Q1
   - Analyze if temperature correlates with sales performance

5. **Executive Report**
   - Create a comprehensive markdown report with:
     * Sales data table (Month, Revenue, Growth %)
     * Weather data table (City, Avg Temp)
     * Key findings and insights
     * Strategic recommendations

Provide detailed step-by-step analysis with all intermediate calculations.
";

        Console.WriteLine("=== ADVANCED TEST 1: Quarterly Sales Analysis ===\n");

        // Act
        var responses = new List<AiSceneResponse>();
        var scenesUsed = new HashSet<string>();
        var toolsExecuted = new List<string>();
        var startTime = DateTime.UtcNow;

        await foreach (var response in sceneManager.ExecuteAsync(request))
        {
            responses.Add(response);

            if (response.SceneName != null)
                scenesUsed.Add(response.SceneName);

            if (response.FunctionName != null)
                toolsExecuted.Add($"{response.SceneName}.{response.FunctionName}");

            Console.WriteLine($"[{response.Status,-25}] {response.Message}");
            if (response.SceneName != null)
                Console.WriteLine($"  └─ Scene: {response.SceneName}");
            if (response.FunctionName != null)
                Console.WriteLine($"  └─ Tool: {response.FunctionName}");
            Console.WriteLine();
        }

        var executionTime = (DateTime.UtcNow - startTime).TotalSeconds;

        // Assert
        Assert.NotEmpty(responses);
        Assert.Contains(responses, r => r.Status == AiResponseStatus.Completed);

        // Should use multiple scenes
        Assert.True(scenesUsed.Count >= 3, $"Expected at least 3 scenes, got {scenesUsed.Count}");
        Assert.Contains("SalesAnalysis", scenesUsed);
        Assert.Contains("Calculator", scenesUsed);

        // Should execute many tools
        Assert.True(toolsExecuted.Count >= 10, $"Expected at least 10 tool calls, got {toolsExecuted.Count}");

        // Should have planning
        Assert.Contains(responses, r => r.Status == AiResponseStatus.Planning);

        // Should generate final report
        var finalResponse = responses.LastOrDefault(r => r.Status == AiResponseStatus.Running);
        Assert.NotNull(finalResponse?.Message);
        Assert.Contains("report", finalResponse.Message.ToLower());

        Console.WriteLine($"\n📊 Execution Summary:");
        Console.WriteLine($"   Total time: {executionTime:F2}s");
        Console.WriteLine($"   Scenes used: {scenesUsed.Count} ({string.Join(", ", scenesUsed)})");
        Console.WriteLine($"   Tools executed: {toolsExecuted.Count}");
        Console.WriteLine($"   Total cost: ${responses.Last().TotalCost:F4}");
    }

    /// <summary>
    /// Test 2: Year-over-Year Comparison with Predictive Analysis
    /// </summary>
    [Fact(Skip = "Advanced integration test - Remove Skip to run with Azure OpenAI")]
    public async Task Advanced_YearOverYearAnalysis_WithForecasting()
    {
        /*
         * SCENARIO: Annual Performance Review
         * 
         * Steps:
         * 1. Get yearly sales for 2023 and 2024
         * 2. Calculate year-over-year growth rate
         * 3. Get monthly breakdown for both years
         * 4. Calculate average monthly growth
         * 5. Identify seasonal patterns
         * 6. Forecast Q2 2024 based on trends
         * 7. Generate comparative analysis report
         * 
         * Expected:
         * - Complex multi-step workflow
         * - Iterative calculations
         * - Trend detection
         * - Forecasting logic
         */

        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();

        var request = @"
Perform year-over-year analysis comparing 2023 vs 2024:

1. Get total sales for 2023 and 2024 (current)
2. Calculate YoY growth rate
3. If growth > 10%, analyze what's driving it
4. If growth < 0%, identify concerning trends
5. Get monthly data for both years and compare patterns
6. Based on trends, forecast Q2 2024 performance
7. Generate executive summary with recommendations
";

        Console.WriteLine("=== ADVANCED TEST 2: Year-over-Year Analysis ===\n");

        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync(request))
        {
            responses.Add(response);
            Console.WriteLine($"[{response.Status}] {response.Message}");
        }

        // Assert
        Assert.NotEmpty(responses);
        Assert.Contains(responses, r => r.Status == AiResponseStatus.Completed);

        // Should use BusinessAnalytics scene for growth rate
        var businessAnalyticsUsed = responses.Any(r => r.SceneName == "BusinessAnalytics");
        Assert.True(businessAnalyticsUsed, "Should use BusinessAnalytics scene");

        // Should calculate growth rate
        var growthCalculated = responses.Any(r =>
            r.FunctionName == "calculateGrowthRate" ||
            r.Message?.Contains("growth", StringComparison.OrdinalIgnoreCase) == true);
        Assert.True(growthCalculated, "Should calculate growth rate");
    }

    /// <summary>
    /// Test 3: Multi-City Sales Performance with Regional Analysis
    /// </summary>
    [Fact(Skip = "Advanced integration test - Remove Skip to run with Azure OpenAI")]
    public async Task Advanced_RegionalSalesAnalysis_WithWeatherImpact()
    {
        /*
         * SCENARIO: Regional Performance Analysis
         * 
         * Steps:
         * 1. Get sales data for Milan, Rome, Paris regions
         * 2. Get weather data for same cities
         * 3. Calculate sales per city
         * 4. Analyze correlation: temperature vs sales
         * 5. Rank cities by performance
         * 6. Generate regional comparison report
         * 
         * Expected:
         * - Parallel data collection (conceptually)
         * - Cross-scene data correlation
         * - Ranking and comparison logic
         * - Table generation
         */

        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();

        var request = @"
Analyze regional sales performance for Q1 2024:

**Cities to analyze:** Milan, Rome, Paris

For each city:
1. Get current temperature
2. Assume sales correlation: sales_estimate = temperature * 1000

Calculate:
- Which city has highest estimated sales?
- What's the average temperature across cities?
- What's the total estimated sales?
- What's the temperature range (max - min)?

Generate a comparison table with:
| City | Temperature | Est. Sales | Performance Rank |

Provide insights on how weather impacts sales.
";

        Console.WriteLine("=== ADVANCED TEST 3: Regional Analysis ===\n");

        var responses = new List<AiSceneResponse>();
        var toolsExecuted = new List<string>();

        await foreach (var response in sceneManager.ExecuteAsync(request))
        {
            responses.Add(response);
            if (response.FunctionName != null)
                toolsExecuted.Add(response.FunctionName);

            Console.WriteLine($"[{response.Status}] {response.Message}");
        }

        // Assert
        Assert.NotEmpty(responses);

        // Should call getTemperature multiple times
        var tempCalls = toolsExecuted.Count(t => t == "getTemperature");
        Assert.True(tempCalls >= 3, $"Expected at least 3 temperature calls, got {tempCalls}");

        // Should use DataAnalysis for calculations
        var dataAnalysisUsed = responses.Any(r => r.SceneName == "DataAnalysis");
        Assert.True(dataAnalysisUsed, "Should use DataAnalysis scene");

        // Should generate table
        var tableGenerated = responses.Any(r =>
            r.FunctionName == "formatAsTable" ||
            r.Message?.Contains("|", StringComparison.Ordinal) == true);
        Assert.True(tableGenerated, "Should generate comparison table");
    }

    /// <summary>
    /// Test 4: Complex Decision Tree with Conditional Logic
    /// </summary>
    [Fact(Skip = "Advanced integration test - Remove Skip to run with Azure OpenAI")]
    public async Task Advanced_ConditionalWorkflow_WithDecisionTree()
    {
        /*
         * SCENARIO: Automated Decision Making
         * 
         * Decision Tree:
         * IF sales growth > 20% THEN
         *    -> Analyze top products
         *    -> Calculate profit margins
         *    -> Recommend expansion
         * ELSE IF sales growth > 0% THEN
         *    -> Analyze steady growth
         *    -> Maintain current strategy
         * ELSE
         *    -> Identify problems
         *    -> Generate recovery plan
         * 
         * Expected:
         * - Conditional scene execution
         * - Different paths based on data
         * - Context-aware recommendations
         */

        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();

        var request = @"
Perform automated sales health check:

1. Get 2023 yearly sales
2. Get 2024 yearly sales (current YTD)
3. Calculate growth rate

**Decision Logic:**
- IF growth > 20%: 
  * Get top 3 products
  * Calculate percentage contribution of top products to total
  * Recommend: ""Accelerate growth strategy""
  
- ELSE IF growth > 0%:
  * Analyze steady growth pattern
  * Recommend: ""Maintain current trajectory""
  
- ELSE (negative growth):
  * Identify critical issues
  * Recommend: ""Implement recovery plan""

Generate action plan based on the scenario.
";

        Console.WriteLine("=== ADVANCED TEST 4: Conditional Workflow ===\n");

        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync(request))
        {
            responses.Add(response);
            Console.WriteLine($"[{response.Status}] {response.Message}");
        }

        // Assert
        Assert.NotEmpty(responses);
        Assert.Contains(responses, r => r.Status == AiResponseStatus.Completed);

        // Should calculate growth rate
        var growthCalculated = responses.Any(r =>
            r.FunctionName == "calculateGrowthRate" ||
            r.Message?.Contains("growth rate", StringComparison.OrdinalIgnoreCase) == true);
        Assert.True(growthCalculated);

        // Should provide recommendations
        var finalResponse = responses.LastOrDefault(r => r.Status == AiResponseStatus.Running);
        Assert.NotNull(finalResponse?.Message);
        Assert.True(
            finalResponse.Message.Contains("recommend", StringComparison.OrdinalIgnoreCase) ||
            finalResponse.Message.Contains("strategy", StringComparison.OrdinalIgnoreCase),
            "Should provide strategic recommendations");
    }

    /// <summary>
    /// Test 5: Maximum Complexity - All Scenes Orchestrated
    /// </summary>
    [Fact(Skip = "Advanced integration test - Remove Skip to run with Azure OpenAI")]
    public async Task Advanced_MaximumComplexity_AllScenesOrchestrated()
    {
        /*
         * ULTIMATE COMPLEXITY TEST
         * 
         * This test uses ALL 6 scenes in a single workflow:
         * 1. SalesAnalysis - Get sales data
         * 2. Calculator - Financial calculations
         * 3. BusinessAnalytics - KPI calculations
         * 4. DataAnalysis - Statistical analysis
         * 5. Weather - Climate data
         * 6. ReportGenerator - Executive summary
         * 
         * Expected:
         * - 6/6 scenes used
         * - 20+ tool calls
         * - Complex data flow
         * - Professional final report
         * - Execution time < 60 seconds
         */

        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();

        var request = @"
Create a COMPREHENSIVE Q1 2024 Business Intelligence Report:

**PART 1: Sales Performance**
- Get monthly sales for Jan, Feb, Mar 2024
- Calculate total Q1 revenue
- Calculate month-over-month growth for each month

**PART 2: Statistical Analysis**
- Calculate average monthly sales
- Find best and worst months
- Calculate sales variance

**PART 3: Year-over-Year Comparison**
- Get Q1 2023 sales (use yearly sales / 4 as estimate)
- Calculate YoY growth rate for Q1
- Determine if performance is improving

**PART 4: Product Analysis**
- Get top 3 products
- Calculate percentage of sales from top products

**PART 5: Weather Correlation**
- Get temperatures for Milan, Rome, Paris
- Calculate average regional temperature
- Hypothesize weather impact on sales

**PART 6: Executive Report**
Create professional markdown report with:
- Executive Summary (3-4 sentences)
- Sales Performance Table (Month | Revenue | Growth%)
- Key Metrics Table (Metric | Value)
- Weather Data Table (City | Temperature)
- Top Products List
- Strategic Recommendations (3 points)
- Conclusion

Make it look like a real McKinsey report!
";

        Console.WriteLine("=== ADVANCED TEST 5: Maximum Complexity ===\n");
        Console.WriteLine("This test orchestrates ALL 6 scenes in a single workflow.\n");

        var responses = new List<AiSceneResponse>();
        var scenesUsed = new HashSet<string>();
        var toolsByScene = new Dictionary<string, int>();
        var startTime = DateTime.UtcNow;

        await foreach (var response in sceneManager.ExecuteAsync(request))
        {
            responses.Add(response);

            if (response.SceneName != null)
            {
                scenesUsed.Add(response.SceneName);

                if (response.FunctionName != null)
                {
                    if (!toolsByScene.ContainsKey(response.SceneName))
                        toolsByScene[response.SceneName] = 0;
                    toolsByScene[response.SceneName]++;
                }
            }

            // Only log key events to avoid clutter
            if (response.Status == AiResponseStatus.Planning ||
                response.Status == AiResponseStatus.ExecutingScene ||
                response.Status == AiResponseStatus.FunctionCompleted ||
                response.Status == AiResponseStatus.Completed)
            {
                Console.WriteLine($"[{response.Status}] {response.Message?.Substring(0, Math.Min(100, response.Message?.Length ?? 0))}");
            }
        }

        var executionTime = (DateTime.UtcNow - startTime).TotalSeconds;

        // Assert
        Assert.NotEmpty(responses);
        Assert.Contains(responses, r => r.Status == AiResponseStatus.Completed);

        Console.WriteLine($"\n{'=',-80}");
        Console.WriteLine("📊 EXECUTION METRICS");
        Console.WriteLine($"{'=',-80}");
        Console.WriteLine($"Total Execution Time: {executionTime:F2}s");
        Console.WriteLine($"Total Responses: {responses.Count}");
        Console.WriteLine($"Scenes Used: {scenesUsed.Count}/6");
        Console.WriteLine($"Total Tool Calls: {toolsByScene.Values.Sum()}");
        Console.WriteLine($"Total Cost: ${responses.Last().TotalCost:F4}");
        Console.WriteLine($"\nTool Calls by Scene:");
        foreach (var kvp in toolsByScene.OrderByDescending(x => x.Value))
        {
            Console.WriteLine($"  {kvp.Key,-20}: {kvp.Value} tools");
        }
        Console.WriteLine($"\nScenes Used: {string.Join(", ", scenesUsed.OrderBy(x => x))}");
        Console.WriteLine($"{'=',-80}\n");

        // Assertions for maximum complexity
        Assert.True(scenesUsed.Count >= 4, $"Expected at least 4 scenes, got {scenesUsed.Count}");
        Assert.True(toolsByScene.Values.Sum() >= 15, $"Expected at least 15 tool calls, got {toolsByScene.Values.Sum()}");
        Assert.True(executionTime < 120, $"Expected execution < 120s, got {executionTime:F2}s");

        // Should have comprehensive report
        var finalResponse = responses.LastOrDefault(r => r.Status == AiResponseStatus.Running);
        Assert.NotNull(finalResponse?.Message);
        Assert.Contains("##", finalResponse.Message); // Markdown headers
        Assert.True(finalResponse.Message.Length > 500, "Report should be comprehensive");
    }
}

// ==================== NEW MOCK SERVICES ====================

/// <summary>
/// Sales service for advanced business scenarios
/// </summary>
public interface ISalesService
{
    Task<double> GetMonthlySalesAsync(int year, int month);
    Task<double> GetYearlySalesAsync(int year);
    Task<string[]> GetTopProductsAsync(int count);
}

public sealed class MockSalesService : ISalesService
{
    private readonly Dictionary<(int year, int month), double> _monthlySales = new()
    {
        // 2023 data
        [(2023, 1)] = 45000,
        [(2023, 2)] = 48000,
        [(2023, 3)] = 52000,
        [(2023, 4)] = 55000,
        [(2023, 5)] = 58000,
        [(2023, 6)] = 61000,
        [(2023, 7)] = 59000,
        [(2023, 8)] = 57000,
        [(2023, 9)] = 62000,
        [(2023, 10)] = 68000,
        [(2023, 11)] = 72000,
        [(2023, 12)] = 85000,

        // 2024 data (Q1 + projections)
        [(2024, 1)] = 52000,  // +15.6% YoY
        [(2024, 2)] = 56000,  // +16.7% YoY
        [(2024, 3)] = 61000,  // +17.3% YoY
        [(2024, 4)] = 64000,  // Projected
        [(2024, 5)] = 67000,  // Projected
        [(2024, 6)] = 70000,  // Projected
    };

    public Task<double> GetMonthlySalesAsync(int year, int month)
    {
        return Task.FromResult(_monthlySales.GetValueOrDefault((year, month), 50000));
    }

    public Task<double> GetYearlySalesAsync(int year)
    {
        var total = _monthlySales
            .Where(kvp => kvp.Key.year == year)
            .Sum(kvp => kvp.Value);

        return Task.FromResult(total > 0 ? total : 600000); // Fallback
    }

    public Task<string[]> GetTopProductsAsync(int count)
    {
        var products = new[]
        {
            "Premium Widget Pro",
            "Enterprise Dashboard Suite",
            "Cloud Analytics Platform",
            "AI-Powered Insights Tool",
            "Mobile App Bundle"
        };

        return Task.FromResult(products.Take(count).ToArray());
    }
}

/// <summary>
/// Business analytics service for KPI calculations
/// </summary>
public interface IBusinessAnalyticsService
{
    Task<double> CalculateGrowthRateAsync(double oldValue, double newValue);
    Task<double> CalculatePercentageAsync(double part, double total);
    Task<string> DetectTrendAsync(double[] values);
}

public sealed class MockBusinessAnalyticsService : IBusinessAnalyticsService
{
    public Task<double> CalculateGrowthRateAsync(double oldValue, double newValue)
    {
        if (oldValue == 0)
            throw new ArgumentException("Old value cannot be zero", nameof(oldValue));

        var growthRate = ((newValue - oldValue) / oldValue) * 100;
        return Task.FromResult(Math.Round(growthRate, 2));
    }

    public Task<double> CalculatePercentageAsync(double part, double total)
    {
        if (total == 0)
            throw new ArgumentException("Total cannot be zero", nameof(total));

        var percentage = (part / total) * 100;
        return Task.FromResult(Math.Round(percentage, 2));
    }

    public Task<string> DetectTrendAsync(double[] values)
    {
        if (values.Length < 2)
            return Task.FromResult("Insufficient data");

        var increases = 0;
        var decreases = 0;

        for (int i = 1; i < values.Length; i++)
        {
            if (values[i] > values[i - 1]) increases++;
            else if (values[i] < values[i - 1]) decreases++;
        }

        if (increases > decreases * 2)
            return Task.FromResult("Strong upward trend");
        else if (increases > decreases)
            return Task.FromResult("Moderate upward trend");
        else if (decreases > increases * 2)
            return Task.FromResult("Strong downward trend");
        else if (decreases > increases)
            return Task.FromResult("Moderate downward trend");
        else
            return Task.FromResult("Stable/Flat trend");
    }
}
