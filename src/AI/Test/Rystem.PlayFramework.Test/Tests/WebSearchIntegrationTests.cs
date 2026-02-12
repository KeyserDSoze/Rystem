using Microsoft.Extensions.DependencyInjection;
using Rystem.PlayFramework;
using Xunit;

namespace Rystem.PlayFramework.Test;

/// <summary>
/// Integration tests for Web Search functionality.
/// Tests cover service registration, search execution, cost tracking, filtering, and result limits.
/// </summary>
public sealed class WebSearchIntegrationTests : PlayFrameworkTestBase
{
    protected override void ConfigurePlayFramework(IServiceCollection services)
    {
        // Register mock web search service with cost tracking
        services.AddWebSearchService<MockWebSearchService>(cost =>
        {
            cost.CostPerSearch = 0.005m;      // $0.005 per search (Bing pricing)
            cost.CostPerResult = 0.0001m;     // $0.0001 per result (optional)
            cost.MonthlyQuota = 10000;        // 10K searches/month
        });

        // Configure PlayFramework with web search enabled
        services.AddPlayFramework(config =>
        {
            config.WithWebSearch(settings =>
            {
                settings.Enabled = true;
                settings.MaxResults = 10;
                settings.SafeSearch = true;
                settings.Market = "en-US";
                settings.Freshness = WebSearchFreshness.Any;
            });
        });
    }

    [Fact]
    public void WebSearchService_ShouldBeRegistered()
    {
        // Arrange & Act
        var webSearchService = ServiceProvider.GetService<IWebSearchService>();
        var factory = ServiceProvider.GetService<IFactory<IWebSearchService>>();

        // Assert
        Assert.NotNull(webSearchService);
        Assert.NotNull(factory);

        // Verify factory can create service with empty key
        var factoryService = factory.Create(string.Empty);
        Assert.NotNull(factoryService);
        Assert.IsType<MockWebSearchService>(factoryService);
    }

    [Fact]
    public async Task WebSearchService_SearchAsync_ReturnsResults()
    {
        // Arrange
        var webSearchService = ServiceProvider.GetRequiredService<IWebSearchService>();
        var request = new WebSearchRequest
        {
            Query = "OpenAI GPT-5 announcement",
            Settings = new WebSearchSettings
            {
                MaxResults = 5,
                SafeSearch = true,
                Market = "en-US"
            }
        };

        // Act
        var result = await webSearchService.SearchAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Documents);
        Assert.True(result.Documents.Count <= 5, "Should respect MaxResults setting");
        Assert.NotNull(result.ExecutedQuery);
        Assert.Equal("OpenAI GPT-5 announcement", result.ExecutedQuery);
        Assert.True(result.DurationMs >= 0, "Duration should be non-negative");

        // Verify document structure
        var firstDoc = result.Documents.First();
        Assert.NotNull(firstDoc.Title);
        Assert.NotNull(firstDoc.Url);
        Assert.NotNull(firstDoc.Snippet);
        Assert.True(firstDoc.RelevanceScore > 0, "Should have relevance score");
        Assert.StartsWith("http", firstDoc.Url, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WebSearchService_LowQualityQuery_FiltersResults()
    {
        // Arrange
        var webSearchService = ServiceProvider.GetRequiredService<IWebSearchService>();
        var request = new WebSearchRequest
        {
            Query = "xyzqwertyzxc gibberish nonsense",
            Settings = new WebSearchSettings
            {
                MaxResults = 10,
                MinimumScore = 0.7 // High threshold to filter gibberish
            }
        };

        // Act
        var result = await webSearchService.SearchAsync(request);

        // Assert
        Assert.NotNull(result);

        // Gibberish query should return very few or no results with high MinimumScore
        Assert.True(result.Documents.Count < 3, 
            $"Low-quality query should return few results, got {result.Documents.Count}");

        // All returned documents should meet minimum score threshold
        foreach (var doc in result.Documents)
        {
            Assert.True(doc.RelevanceScore >= 0.7, 
                $"Document '{doc.Title}' has score {doc.RelevanceScore:F2}, expected >= 0.70");
        }
    }

    [Fact]
    public async Task WebSearchService_MaxResults_LimitsResults()
    {
        // Arrange
        var webSearchService = ServiceProvider.GetRequiredService<IWebSearchService>();
        var request = new WebSearchRequest
        {
            Query = ".NET tutorial", // Broad query that matches many documents
            Settings = new WebSearchSettings
            {
                MaxResults = 3 // Strict limit
            }
        };

        // Act
        var result = await webSearchService.SearchAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Documents.Count <= 3, "Should not exceed MaxResults");
        Assert.True(result.TotalCount >= result.Documents.Count, 
            "TotalCount should be >= returned documents");

        // Results should be ordered by relevance (highest first)
        for (int i = 0; i < result.Documents.Count - 1; i++)
        {
            Assert.True(
                result.Documents[i].RelevanceScore >= result.Documents[i + 1].RelevanceScore,
                "Results should be ordered by descending relevance");
        }
    }

    [Fact]
    public async Task WebSearchService_FreshnessFilter_ReturnsRecentContent()
    {
        // Arrange
        var webSearchService = ServiceProvider.GetRequiredService<IWebSearchService>();
        var request = new WebSearchRequest
        {
            Query = "GPT-5 announcement", // Recent news
            Settings = new WebSearchSettings
            {
                MaxResults = 10,
                Freshness = WebSearchFreshness.Week // Last 7 days only
            }
        };

        // Act
        var result = await webSearchService.SearchAsync(request);

        // Assert
        Assert.NotNull(result);

        // All results with PublishedDate should be within the last week
        var oneWeekAgo = DateTime.UtcNow.AddDays(-7);
        foreach (var doc in result.Documents.Where(d => d.PublishedDate.HasValue))
        {
            Assert.True(doc.PublishedDate!.Value >= oneWeekAgo,
                $"Document '{doc.Title}' published on {doc.PublishedDate.Value:yyyy-MM-dd} is older than 1 week");
        }
    }
}
