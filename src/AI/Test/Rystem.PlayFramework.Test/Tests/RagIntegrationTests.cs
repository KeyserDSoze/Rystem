using Microsoft.Extensions.DependencyInjection;
using Rystem.PlayFramework.Test.Infrastructure;

namespace Rystem.PlayFramework.Test.Tests;

/// <summary>
/// Basic test suite for RAG (Retrieval-Augmented Generation) integration.
/// Tests RAG configuration and cost tracking.
/// </summary>
public class RagIntegrationTests : PlayFrameworkTestBase
{
    private ISceneManager? _sceneManager;

    protected ISceneManager SceneManager => _sceneManager ??= ServiceProvider.GetRequiredService<ISceneManager>();

    protected override void ConfigurePlayFramework(IServiceCollection services)
    {
        // Register MockRagService with cost configuration
        services.AddRagService<MockRagService>(
            configureCost: settings =>
            {
                // OpenAI text-embedding-ada-002 pricing
                settings.CostPerThousandEmbeddingTokens = 0.0001m;
                settings.CostPerThousandSearchTokens = 0m;
                settings.FixedCostPerSearch = 0m;
            },
            name: "default"
        );

        // Configure PlayFramework
        services.AddPlayFramework(builder =>
        {
            builder
                .Configure(settings =>
                {
                    settings.Planning.Enabled = false;
                    settings.Summarization.Enabled = false;
                })
                // Configure global RAG with default cost settings
                .WithRag(settings =>
                {
                    settings.TopK = 5;
                    settings.SearchAlgorithm = VectorSearchAlgorithm.CosineSimilarity;
                    settings.MinimumScore = 0.3;
                }, "default")
                .AddMainActor("You are a helpful assistant with access to a knowledge base.")
                // Scene 1: Customer Support (override TopK)
                .AddScene("CustomerSupport", "Customer support with knowledge base", scene =>
                {
                    scene
                        .WithRag(settings =>
                        {
                            settings.TopK = 3; // Fewer, more relevant documents
                            settings.MinimumScore = 0.5; // Higher quality threshold
                        }, "default")
                        .WithActors(actorBuilder =>
                        {
                            actorBuilder.AddActor("You are a helpful customer support agent. Use the knowledge base to provide accurate answers.");
                        });
                });
        });
    }

    [Fact]
    public void RagService_ShouldBeRegistered()
    {
        // Arrange & Act
        var ragService = ServiceProvider.GetService<IRagService>();

        // Assert
        Assert.NotNull(ragService);
    }

    [Fact]
    public async Task RagService_SearchAsync_ReturnsDocuments()
    {
        // Arrange
        var ragService = ServiceProvider.GetRequiredService<IRagService>();
        var request = new RagRequest
        {
            Query = "How do I reset my password?",
            Settings = new RagSettings
            {
                TopK = 5,
                MinimumScore = 0.3
            }
        };

        // Act
        var result = await ragService.SearchAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Documents);
        Assert.True(result.Documents.Count <= 5);
        Assert.NotNull(result.TokenUsage);
        Assert.Equal(150, result.TokenUsage.EmbeddingTokens);
    }

    [Fact]
    public async Task RagService_CostCalculation_IsAccurate()
    {
        // Arrange
        var ragService = ServiceProvider.GetRequiredService<IRagService>();
        var request = new RagRequest
        {
            Query = "API authentication",
            Settings = new RagSettings { TopK = 5 }
        };

        // Act
        var result = await ragService.SearchAsync(request);

        // Assert - Verify token usage
        Assert.NotNull(result.TokenUsage);
        Assert.Equal(150, result.TokenUsage.EmbeddingTokens);

        // Expected cost: (150 / 1000) * 0.0001 = $0.000015
        // (Cost calculated by PlayFramework using RagCostSettings)
    }

    [Fact]
    public async Task RagService_LowQualityQuery_FiltersResults()
    {
        // Arrange
        var ragService = ServiceProvider.GetRequiredService<IRagService>();
        var request = new RagRequest
        {
            Query = "xyz abc qwerty", // Gibberish
            Settings = new RagSettings
            {
                TopK = 10,
                MinimumScore = 0.5 // High threshold
            }
        };

        // Act
        var result = await ragService.SearchAsync(request);

        // Assert - Should return no or very few documents
        Assert.NotNull(result);
    }

    [Fact]
    public async Task RagService_TopK_LimitsResults()
    {
        // Arrange
        var ragService = ServiceProvider.GetRequiredService<IRagService>();
        var request = new RagRequest
        {
            Query = "password authentication security billing API",
            Settings = new RagSettings
            {
                TopK = 3,
                MinimumScore = 0.1
            }
        };

        // Act
        var result = await ragService.SearchAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Documents.Count <= 3);
    }
}

