using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework.Test;

/// <summary>
/// Tests for caching functionality in PlayFramework.
/// </summary>
public sealed class CacheTests : PlayFrameworkTestBase
{
    protected override void ConfigurePlayFramework(IServiceCollection services)
    {
        services.AddPlayFramework(builder =>
        {
            builder
                .Configure(settings =>
                {
                    settings.Planning.Enabled = false;
                    settings.Summarization.Enabled = false;
                    settings.Cache.Enabled = true;
                    settings.Cache.DefaultExpirationSeconds = 300; // 5 minutes
                })
                .AddCache(cacheBuilder =>
                {
                    cacheBuilder.WithMemory();
                })
                .AddMainActor("You are a test assistant.")
                .AddScene(sceneBuilder =>
                {
                    sceneBuilder
                        .WithName("TestScene")
                        .WithDescription("A test scene");
                });
        });
    }

    [Fact]
    public void CacheService_ShouldBeRegistered()
    {
        // Arrange & Act
        var cacheService = ServiceProvider.GetService<ICacheService>();

        // Assert
        Assert.NotNull(cacheService);
    }

    [Fact]
    public async Task Cache_ShouldStoreAndRetrieveResponses()
    {
        // Arrange
        var cacheService = ServiceProvider.GetRequiredService<ICacheService>();
        var key = Guid.NewGuid().ToString();
        var responses = new List<AiSceneResponse>
        {
            new()
            {
                Status = AiResponseStatus.Running,
                Message = "Test message 1"
            },
            new()
            {
                Status = AiResponseStatus.Completed,
                Message = "Test message 2"
            }
        };

        // Act
        await cacheService.SetAsync(key, responses, CacheBehavior.Default);
        var retrieved = await cacheService.GetAsync(key);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(2, retrieved.Count);
        Assert.Equal("Test message 1", retrieved[0].Message);
        Assert.Equal("Test message 2", retrieved[1].Message);
    }

    [Fact]
    public async Task Cache_ShouldReturnNull_WhenKeyNotFound()
    {
        // Arrange
        var cacheService = ServiceProvider.GetRequiredService<ICacheService>();
        var key = Guid.NewGuid().ToString();

        // Act
        var retrieved = await cacheService.GetAsync(key);

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task Cache_ShouldRemove_ExistingKey()
    {
        // Arrange
        var cacheService = ServiceProvider.GetRequiredService<ICacheService>();
        var key = Guid.NewGuid().ToString();
        var responses = new List<AiSceneResponse>
        {
            new() { Status = AiResponseStatus.Running, Message = "Test" }
        };

        await cacheService.SetAsync(key, responses, CacheBehavior.Default);

        // Act
        await cacheService.RemoveAsync(key);
        var retrieved = await cacheService.GetAsync(key);

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task Cache_WithForeverBehavior_ShouldNotExpire()
    {
        // Arrange
        var cacheService = ServiceProvider.GetRequiredService<ICacheService>();
        var key = Guid.NewGuid().ToString();
        var responses = new List<AiSceneResponse>
        {
            new() { Status = AiResponseStatus.Running, Message = "Forever cached" }
        };

        // Act
        await cacheService.SetAsync(key, responses, CacheBehavior.Forever);
        
        // Simulate delay (in real scenario, this would be longer)
        await Task.Delay(100);
        
        var retrieved = await cacheService.GetAsync(key);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Single(retrieved);
        Assert.Equal("Forever cached", retrieved[0].Message);
    }
}
