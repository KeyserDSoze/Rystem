using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rystem.PlayFramework.Test.Infrastructure;

namespace Rystem.PlayFramework.Test.Tests;

/// <summary>
/// Tests for rate limiting functionality with metadata-based grouping.
/// </summary>
public sealed class RateLimitingTests
{
    [Fact]
    public async Task RateLimit_PerUser_ShouldEnforceLimit()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddChatClient<MockChatClient>(name: null);

        services.AddPlayFramework(builder =>
        {
            builder.AddScene("TestScene", "Test scene", _ => { });

            builder.WithRateLimit(limit => limit
                .GroupBy("userId")
                .TokenBucket(capacity: 3, refillRate: 1)
                .RejectOnExceeded());
        });

        var serviceProvider = services.BuildServiceProvider();
        var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();

        var metadata = new Dictionary<string, object>
        {
            ["userId"] = "user123"
        };

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Direct
        };

        // Act & Assert - First 3 requests should succeed
        for (int i = 0; i < 3; i++)
        {
            var responses = new List<AiSceneResponse>();
            await foreach (var response in sceneManager.ExecuteAsync($"Request {i + 1}", metadata, settings))
            {
                responses.Add(response);
            }

            var hasError = responses.Any(r => r.Status == AiResponseStatus.Error && 
                                             r.ErrorMessage?.Contains("Rate limit exceeded") == true);
            Assert.False(hasError, $"Request {i + 1} should not be rate limited");
        }

        // 4th request should be rejected
        var rejectedResponses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Request 4", metadata, settings))
        {
            rejectedResponses.Add(response);
        }

        var isRateLimited = rejectedResponses.Any(r => 
            r.Status == AiResponseStatus.Error && 
            r.ErrorMessage?.Contains("Rate limit exceeded") == true);
        Assert.True(isRateLimited, "4th request should be rate limited");
    }

    [Fact]
    public async Task RateLimit_CompositeKey_ShouldIsolateByUserAndTenant()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddChatClient<MockChatClient>(name: null);

        services.AddPlayFramework(builder =>
        {
            builder.AddScene("TestScene", "Test scene", _ => { });

            builder.WithRateLimit(limit => limit
                .GroupBy("userId", "tenantId")
                .TokenBucket(capacity: 2, refillRate: 1)
                .RejectOnExceeded());
        });

        var serviceProvider = services.BuildServiceProvider();
        var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Direct
        };

        // Act & Assert - User1 in TenantA: 2 requests OK, 3rd rejected
        var user1TenantA = new Dictionary<string, object>
        {
            ["userId"] = "user1",
            ["tenantId"] = "tenantA"
        };

        for (int i = 0; i < 2; i++)
        {
            var responses = new List<AiSceneResponse>();
            await foreach (var response in sceneManager.ExecuteAsync($"Request {i + 1}", user1TenantA, settings))
            {
                responses.Add(response);
            }
            Assert.False(responses.Any(r => r.Status == AiResponseStatus.Error));
        }

        var rejectedUser1A = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Request 3", user1TenantA, settings))
        {
            rejectedUser1A.Add(response);
        }
        Assert.True(rejectedUser1A.Any(r => r.Status == AiResponseStatus.Error));

        // Same user in different tenant should have independent limit
        var user1TenantB = new Dictionary<string, object>
        {
            ["userId"] = "user1",
            ["tenantId"] = "tenantB"
        };

        var user1BResponses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Request 1", user1TenantB, settings))
        {
            user1BResponses.Add(response);
        }
        Assert.False(user1BResponses.Any(r => r.Status == AiResponseStatus.Error),
            "Same user in different tenant should have independent limit");
    }

    [Fact]
    public async Task RateLimit_WaitBehavior_ShouldWaitForTokenRefill()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddChatClient<MockChatClient>(name: null);

        services.AddPlayFramework(builder =>
        {
            builder.AddScene("TestScene", "Test scene", _ => { });

            builder.WithRateLimit(limit => limit
                .GroupBy("userId")
                .TokenBucket(capacity: 1, refillRate: 10)
                .WaitOnExceeded(TimeSpan.FromSeconds(5)));
        });

        var serviceProvider = services.BuildServiceProvider();
        var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();

        var metadata = new Dictionary<string, object> { ["userId"] = "user123" };
        var settings = new SceneRequestSettings { ExecutionMode = SceneExecutionMode.Direct };

        // Act - First request: immediate
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var response1 = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Request 1", metadata, settings))
        {
            response1.Add(response);
        }
        
        var firstDuration = stopwatch.Elapsed;
        Assert.False(response1.Any(r => r.Status == AiResponseStatus.Error));
        Assert.True(firstDuration < TimeSpan.FromMilliseconds(100), "First request should be immediate");

        // Second request: should wait for refill (~0.1s)
        stopwatch.Restart();
        
        var response2 = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Request 2", metadata, settings))
        {
            response2.Add(response);
        }
        
        var secondDuration = stopwatch.Elapsed;
        Assert.False(response2.Any(r => r.Status == AiResponseStatus.Error));
        Assert.True(secondDuration >= TimeSpan.FromMilliseconds(80), 
            $"Second request should wait for refill (waited {secondDuration.TotalMilliseconds}ms)");
    }

    [Fact]
    public async Task RateLimit_Disabled_ShouldNotLimit()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddChatClient<MockChatClient>(name: null);

        // No WithRateLimit() call = rate limiting disabled
        services.AddPlayFramework(builder =>
        {
            builder.AddScene("TestScene", "Test scene", _ => { });
        });

        var serviceProvider = services.BuildServiceProvider();
        var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();

        var metadata = new Dictionary<string, object> { ["userId"] = "user123" };
        var settings = new SceneRequestSettings { ExecutionMode = SceneExecutionMode.Direct };

        // Act - Should allow many requests without rate limiting
        for (int i = 0; i < 10; i++)
        {
            var responses = new List<AiSceneResponse>();
            await foreach (var response in sceneManager.ExecuteAsync($"Request {i + 1}", metadata, settings))
            {
                responses.Add(response);
            }

            Assert.False(responses.Any(r => r.Status == AiResponseStatus.Error && 
                                           r.ErrorMessage?.Contains("Rate limit") == true),
                $"Request {i + 1} should not be rate limited when feature is disabled");
        }
    }
}
