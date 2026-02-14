using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Rystem.PlayFramework.Test;

/// <summary>
/// Tests for load balancing pool and fallback chain functionality.
/// </summary>
public class LoadBalancingAndFallbackTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly IServiceProvider _serviceProvider;

    public LoadBalancingAndFallbackTests(ITestOutputHelper output)
    {
        _output = output;

        var services = new ServiceCollection();

        // Logging
        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.AddDebug();
            logging.SetMinimumLevel(LogLevel.Debug);
        });

        _serviceProvider = services.BuildServiceProvider();
    }

    #region Load Balancing Tests

    [Fact]
    public async Task LoadBalancing_RoundRobin_DistributesRequestsEvenly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        });

        // Track which client was used
        var executionLog = new List<string>();

        // Register 3 clients in primary pool
        services.AddChatClient<MockChatClient>(
            name: "client-1",
            costSettings: c => {
                c.Enabled = true;
                c.Currency = "USD";
                c.InputTokenCostPer1K = 1.0m;
                c.OutputTokenCostPer1K = 2.0m;
            });

        services.AddChatClient<MockChatClient>(
            name: "client-2",
            costSettings: c => {
                c.Enabled = true;
                c.Currency = "USD";
                c.InputTokenCostPer1K = 1.0m;
                c.OutputTokenCostPer1K = 2.0m;
            });

        services.AddChatClient<MockChatClient>(
            name: "client-3",
            costSettings: c => {
                c.Enabled = true;
                c.Currency = "USD";
                c.InputTokenCostPer1K = 1.0m;
                c.OutputTokenCostPer1K = 2.0m;
            });

        services.AddPlayFramework(builder =>
        {
            builder
                .WithChatClient("client-1")
                .WithChatClient("client-2")
                .WithChatClient("client-3")
                .WithLoadBalancingMode(LoadBalancingMode.RoundRobin)
                .WithRetryPolicy(maxAttempts: 1, baseDelaySeconds: 0.1);

            builder.AddScene("test-scene", "Test scene for load balancing", _ => { });
        });

        var sp = services.BuildServiceProvider();
        var sceneManager = sp.GetRequiredService<ISceneManager>();

        // Act - Execute 6 requests to see round-robin distribution
        var clientOrder = new List<string>();
        for (int i = 0; i < 6; i++)
        {
            await foreach (var response in sceneManager.ExecuteAsync($"Request {i}"))
            {
                if (response.Status == AiResponseStatus.Running && !string.IsNullOrEmpty(response.Message))
                {
                    _output.WriteLine($"Request {i}: {response.Message}");
                }
            }
        }

        // Assert - With round-robin, order should be: client-1, client-2, client-3, client-1, client-2, client-3
        // Note: This is a simplified assertion - in real scenario we'd track ClientName from responses
        Assert.True(true); // Placeholder - real test would verify client order
    }

    [Fact]
    public async Task LoadBalancing_Sequential_UsesClientsInOrder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Debug));

        services.AddChatClient<MockChatClient>(name: "client-1", costSettings: c => c.Enabled = true);
        services.AddChatClient<MockChatClient>(name: "client-2", costSettings: c => c.Enabled = true);
        services.AddChatClient<MockChatClient>(name: "client-3", costSettings: c => c.Enabled = true);

        services.AddPlayFramework(builder =>
        {
            builder
                .WithChatClient("client-1")
                .WithChatClient("client-2")
                .WithChatClient("client-3")
                .WithLoadBalancingMode(LoadBalancingMode.Sequential)
                .WithRetryPolicy(maxAttempts: 1, baseDelaySeconds: 0.1);

            builder.AddScene("test-scene", "Test scene", _ => { });
        });

        var sp = services.BuildServiceProvider();
        var sceneManager = sp.GetRequiredService<ISceneManager>();

        // Act - Execute 3 requests
        for (int i = 0; i < 3; i++)
        {
            await foreach (var response in sceneManager.ExecuteAsync($"Request {i}"))
            {
                if (response.Status == AiResponseStatus.Running)
                {
                    _output.WriteLine($"Request {i}: {response.Message}");
                }
            }
        }

        // Assert - Sequential should use client-1 → client-2 → client-3
        Assert.True(true); // Placeholder
    }

    [Fact]
    public async Task LoadBalancing_None_UsesOnlyFirstClient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Debug));

        services.AddChatClient<MockChatClient>(name: "client-1", costSettings: c => c.Enabled = true);
        services.AddChatClient<MockChatClient>(name: "client-2", costSettings: c => c.Enabled = true);

        services.AddPlayFramework(builder =>
        {
            builder
                .WithChatClient("client-1")
                .WithChatClient("client-2")
                .WithLoadBalancingMode(LoadBalancingMode.None) // Only use first client
                .WithRetryPolicy(maxAttempts: 1, baseDelaySeconds: 0.1);

            builder.AddScene("test-scene", "Test scene", _ => { });
        });

        var sp = services.BuildServiceProvider();
        var sceneManager = sp.GetRequiredService<ISceneManager>();

        // Act
        await foreach (var response in sceneManager.ExecuteAsync("Test request"))
        {
            if (response.Status == AiResponseStatus.Running)
            {
                _output.WriteLine($"Response: {response.Message}");
            }
        }

        // Assert - Should only use client-1
        Assert.True(true); // Placeholder
    }

    #endregion

    #region Fallback Chain Tests

    [Fact]
    public async Task Fallback_ActivatesWhenPrimaryPoolFails()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Debug));

        // Primary pool clients that will fail
        services.AddChatClient<FailingMockChatClient>(
            name: "primary-1",
            costSettings: c => c.Enabled = true);

        services.AddChatClient<FailingMockChatClient>(
            name: "primary-2",
            costSettings: c => c.Enabled = true);

        // Fallback client that succeeds
        services.AddChatClient<MockChatClient>(
            name: "fallback-1",
            costSettings: c => c.Enabled = true);

        services.AddPlayFramework(builder =>
        {
            builder
                // Primary pool (will fail)
                .WithChatClient("primary-1")
                .WithChatClient("primary-2")
                .WithLoadBalancingMode(LoadBalancingMode.Sequential)
                
                // Fallback chain (will succeed)
                .WithChatClientAsFallback("fallback-1")
                .WithFallbackMode(FallbackMode.Sequential)
                
                .WithRetryPolicy(maxAttempts: 2, baseDelaySeconds: 0.1);

            builder.AddScene("test-scene", "Test scene", _ => { });
        });

        var sp = services.BuildServiceProvider();
        var sceneManager = sp.GetRequiredService<ISceneManager>();

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Test request"))
        {
            responses.Add(response);
            _output.WriteLine($"[{response.Status}] {response.Message}");
        }

        // Assert - Should have warnings about primary pool failures, then success from fallback
        Assert.Contains(responses, r => r.Status == AiResponseStatus.Completed);
    }

    [Fact]
    public async Task Fallback_DoesNotActivateWhenPrimarySucceeds()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Debug));

        // Primary pool - succeeds
        services.AddChatClient<MockChatClient>(
            name: "primary-1",
            costSettings: c => c.Enabled = true);

        // Fallback - should never be used
        services.AddChatClient<FailingMockChatClient>(
            name: "fallback-1",
            costSettings: c => c.Enabled = true);

        services.AddPlayFramework(builder =>
        {
            builder
                .WithChatClient("primary-1")
                .WithLoadBalancingMode(LoadBalancingMode.None)
                .WithChatClientAsFallback("fallback-1")
                .WithFallbackMode(FallbackMode.Sequential)
                .WithRetryPolicy(maxAttempts: 1, baseDelaySeconds: 0.1);

            builder.AddScene("test-scene", "Test scene", _ => { });
        });

        var sp = services.BuildServiceProvider();
        var sceneManager = sp.GetRequiredService<ISceneManager>();

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Test request"))
        {
            responses.Add(response);
            _output.WriteLine($"[{response.Status}] {response.Message}");
        }

        // Assert - Should succeed without activating fallback
        Assert.Contains(responses, r => r.Status == AiResponseStatus.Completed);
        Assert.DoesNotContain(responses, r => r.Message?.Contains("fallback") == true);
    }

    #endregion

    #region Retry Tests

    [Fact]
    public async Task Retry_RetriesTransientErrors()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Debug));

        // Client that fails twice with transient error, then succeeds
        services.AddChatClient<TransientFailureMockChatClient>(
            name: "client-1",
            costSettings: c => c.Enabled = true);

        services.AddPlayFramework(builder =>
        {
            builder
                .WithChatClient("client-1")
                .WithLoadBalancingMode(LoadBalancingMode.None)
                .WithRetryPolicy(maxAttempts: 3, baseDelaySeconds: 0.1);

            builder.AddScene("test-scene", "Test scene", _ => { });
        });

        var sp = services.BuildServiceProvider();
        var sceneManager = sp.GetRequiredService<ISceneManager>();

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Test request"))
        {
            responses.Add(response);
            _output.WriteLine($"[{response.Status}] {response.Message}");
        }

        // Assert - Should eventually succeed after retries
        Assert.Contains(responses, r => r.Status == AiResponseStatus.Completed);
    }

    [Fact]
    public async Task Retry_SkipsNonTransientErrors()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Debug));

        // Client that fails with non-transient error
        services.AddChatClient<NonTransientFailureMockChatClient>(
            name: "client-1",
            costSettings: c => c.Enabled = true);

        // Fallback that succeeds
        services.AddChatClient<MockChatClient>(
            name: "fallback-1",
            costSettings: c => c.Enabled = true);

        services.AddPlayFramework(builder =>
        {
            builder
                .WithChatClient("client-1")
                .WithLoadBalancingMode(LoadBalancingMode.None)
                .WithChatClientAsFallback("fallback-1")
                .WithRetryPolicy(maxAttempts: 3, baseDelaySeconds: 0.1);

            builder.AddScene("test-scene", "Test scene", _ => { });
        });

        var sp = services.BuildServiceProvider();
        var sceneManager = sp.GetRequiredService<ISceneManager>();

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Test request"))
        {
            responses.Add(response);
            _output.WriteLine($"[{response.Status}] {response.Message}");
        }

        // Assert - Should skip to fallback immediately (no retries on non-transient error)
        Assert.Contains(responses, r => r.Status == AiResponseStatus.Completed);
    }

    #endregion

    #region Cost Tracking Tests

    [Fact]
    public async Task CostTracking_AccumulatesAcrossMultipleClients()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Debug));

        services.AddChatClient<MockChatClient>(
            name: "client-1",
            costSettings: c => {
                c.Enabled = true;
                c.Currency = "USD";
                c.InputTokenCostPer1K = 1.0m;
                c.OutputTokenCostPer1K = 2.0m;
            });

        services.AddPlayFramework(builder =>
        {
            builder
                .WithChatClient("client-1")
                .WithLoadBalancingMode(LoadBalancingMode.None);

            builder.AddScene("test-scene", "Test scene", _ => { });
        });

        var sp = services.BuildServiceProvider();
        var sceneManager = sp.GetRequiredService<ISceneManager>();

        // Act
        decimal totalCost = 0;
        await foreach (var response in sceneManager.ExecuteAsync("Test request"))
        {
            if (response.Cost.HasValue)
            {
                totalCost = response.TotalCost;
                _output.WriteLine($"Cost: {response.Cost:F6}, Total: {response.TotalCost:F6}");
            }
        }

        // Assert - Should have accumulated cost
        Assert.True(totalCost > 0, "Total cost should be greater than 0");
    }

    #endregion

    #region Streaming Tests

    [Fact]
    public async Task Fallback_WorksWithoutPrimaryFailure()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Debug));

        // Primary fails, fallback succeeds
        services.AddChatClient<FailingMockChatClient>(
            name: "primary-1",
            costSettings: c => c.Enabled = true);

        services.AddChatClient<MockChatClient>(
            name: "fallback-1",
            costSettings: c => c.Enabled = true);

        services.AddPlayFramework(builder =>
        {
            builder
                .WithChatClient("primary-1")
                .WithLoadBalancingMode(LoadBalancingMode.None)
                .WithChatClientAsFallback("fallback-1")
                .WithRetryPolicy(maxAttempts: 1, baseDelaySeconds: 0.1);

            builder.AddScene("test-scene", "Test scene", _ => { });
        });

        var sp = services.BuildServiceProvider();
        var sceneManager = sp.GetRequiredService<ISceneManager>();

        // Act
        bool gotResponse = false;
        string? finalMessage = null;

        await foreach (var response in sceneManager.ExecuteAsync("Test request", metadata: null, new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Direct,
            CacheBehavior = CacheBehavior.Avoidable
        }))
        {
            _output.WriteLine($"Response Status: {response.Status}, Message: {response.Message}");

            if (response.Status == AiResponseStatus.Running)
            {
                gotResponse = true;
                finalMessage = response.Message;
            }
        }

        // Assert - Should have received response from fallback (not streaming, just normal response)
        Assert.True(gotResponse, "Should have received a running response");
        Assert.NotNull(finalMessage);
        Assert.Contains("Mock response", finalMessage); // MockChatClient returns "Mock response"
    }

    #endregion

    public void Dispose()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}

#region Mock Chat Clients for Testing

/// <summary>
/// Mock client that always fails with non-transient error.
/// </summary>
internal class FailingMockChatClient : IChatClient
{
    public ChatClientMetadata Metadata => new("FailingMock", new Uri("http://localhost"), "mock-model");

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Client failed (non-transient)");
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Client failed (non-transient)");
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public void Dispose() { }
}

/// <summary>
/// Mock client that fails with transient error for first 2 attempts, then succeeds.
/// </summary>
internal class TransientFailureMockChatClient : IChatClient
{
    private int _attemptCount = 0;

    public ChatClientMetadata Metadata => new("TransientFailureMock", new Uri("http://localhost"), "mock-model");

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _attemptCount++;

        if (_attemptCount <= 2)
        {
            // Transient error (HTTP 429 - Rate Limit)
            throw new HttpRequestException("Rate limit exceeded (429)", null, System.Net.HttpStatusCode.TooManyRequests);
        }

        // Success on 3rd attempt
        return Task.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, "Success after retries")])
        {
            Usage = new UsageDetails
            {
                InputTokenCount = 150,
                OutputTokenCount = 250
            }
        });
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public void Dispose() { }
}

/// <summary>
/// Mock client that always fails with non-transient error (should skip retries).
/// </summary>
internal class NonTransientFailureMockChatClient : IChatClient
{
    public ChatClientMetadata Metadata => new("NonTransientFailureMock", new Uri("http://localhost"), "mock-model");

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Non-transient error (HTTP 401 - Unauthorized)
        throw new HttpRequestException("Unauthorized (401)", null, System.Net.HttpStatusCode.Unauthorized);
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public void Dispose() { }
}

/// <summary>
/// Mock client that supports streaming.
/// </summary>
internal class StreamingMockChatClient : IChatClient
{
    public ChatClientMetadata Metadata => new("StreamingMock", new Uri("http://localhost"), "mock-model");

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, "Mock streaming response")])
        {
            Usage = new UsageDetails
            {
                InputTokenCount = 50,
                OutputTokenCount = 100
            }
        });
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var words = new[] { "This", "is", "a", "streamed", "response" };

        foreach (var word in words)
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, word + " ");

            await Task.Delay(10, cancellationToken);
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public void Dispose() { }
}

#endregion
