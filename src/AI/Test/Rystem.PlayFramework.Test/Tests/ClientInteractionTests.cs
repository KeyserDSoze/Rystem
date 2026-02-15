using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;

namespace Rystem.PlayFramework.Test.Tests;

/// <summary>
/// Tests for client-side interaction with continuation token flow.
/// These tests simulate a TypeScript client executing OnClient() tools.
/// </summary>
public sealed class ClientInteractionTests : PlayFrameworkTestBase
{
    protected override void ConfigurePlayFramework(IServiceCollection services)
    {
        // Register IDistributedCache (REQUIRED for OnClient tools)
        services.AddDistributedMemoryCache();

        // Register custom mock ChatClient that simulates LLM calling client tools
        services.AddSingleton<IChatClient>(sp => new ClientInteractionMockChatClient());

        // Configure PlayFramework with client interaction scene
        services.AddPlayFramework(builder =>
        {
            builder
                .Configure(settings =>
                {
                    settings.Planning.Enabled = false;
                    settings.Summarization.Enabled = false;
                })
                .AddMainActor("You are a helpful assistant that can request client-side data.")
                .AddScene("Photography", "Capture photos from client device", sceneBuilder =>
                {
                    sceneBuilder
                        .WithCacheExpiration(TimeSpan.FromMinutes(5))
                        .OnClient(clientBuilder =>
                        {
                            clientBuilder
                                .AddTool(
                                    toolName: "capture_photo",
                                    description: "Captures a photo from the client's camera",
                                    timeoutSeconds: 30);
                        })
                        .WithActors(actorBuilder =>
                        {
                            actorBuilder.AddActor("When asked to take a photo, use the capture_photo tool.");
                        });
                })
                .AddScene("Location", "Get client location", sceneBuilder =>
                {
                    sceneBuilder
                        .WithCacheExpiration(TimeSpan.FromMinutes(3))
                        .OnClient(clientBuilder =>
                        {
                            clientBuilder
                                .AddTool(
                                    toolName: "get_location",
                                    description: "Gets the client's current GPS location",
                                    timeoutSeconds: 15);
                        });
                })
                .AddScene("MultiClient", "Multiple client tools", sceneBuilder =>
                {
                    sceneBuilder
                        .OnClient(clientBuilder =>
                        {
                            clientBuilder
                                .AddTool("capture_photo", "Capture photo", 30)
                                .AddTool("get_location", "Get location", 15);
                        });
                });
        });
    }

    /// <summary>
    /// Verifies that IDistributedCache is registered.
    /// </summary>
    [Fact]
    public void DistributedCache_ShouldBeRegistered()
    {
        var cache = ServiceProvider.GetService<IDistributedCache>();
        Assert.NotNull(cache);
    }

    /// <summary>
    /// Verifies that scene with OnClient() tools is created correctly.
    /// </summary>
    [Fact]
    public void SceneFactory_ShouldCreateSceneWithClientTools()
    {
        var sceneFactory = ServiceProvider.GetRequiredService<ISceneFactory>();
        var scene = sceneFactory.Create("Photography");

        Assert.NotNull(scene);
        Assert.Equal("Photography", scene.Name);

        // Verify ClientInteractionDefinitions are populated
        Assert.NotNull(scene.ClientInteractionDefinitions);
        Assert.Single(scene.ClientInteractionDefinitions);
        Assert.Equal("capture_photo", scene.ClientInteractionDefinitions[0].ToolName);
    }

    /// <summary>
    /// Tests basic client interaction flow: Request -> AwaitingClient -> Resume -> Completed.
    /// Uses SceneExecutionMode.Scene to bypass scene selection (directly executes Photography scene).
    /// </summary>
    [Fact]
    public async Task ClientInteraction_CapturePhoto_ShouldReturnAwaitingClientAndResumeSuccessfully()
    {
        // Arrange
        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Scene,
            SceneName = "Photography"
        };

        // Act - Phase 1: Execute scene; mock LLM returns FunctionCallContent for capture_photo
        AiSceneResponse? awaitingClientResponse = null;
        await foreach (var response in sceneManager.ExecuteAsync("Take a photo please", metadata: null, settings))
        {
            if (response.Status == AiResponseStatus.AwaitingClient)
            {
                awaitingClientResponse = response;
                break;
            }
        }

        // Assert Phase 1: Should have received AwaitingClient
        Assert.NotNull(awaitingClientResponse);
        Assert.Equal(AiResponseStatus.AwaitingClient, awaitingClientResponse.Status);
        Assert.NotNull(awaitingClientResponse.ContinuationToken);
        Assert.NotNull(awaitingClientResponse.ClientInteractionRequest);
        Assert.Equal("capture_photo", awaitingClientResponse.ClientInteractionRequest.ToolName);
        Assert.NotNull(awaitingClientResponse.ConversationKey);

        // Simulate client execution: Create fake photo data
        var fakePhotoBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // Minimal JPEG header

        var clientResult = new ClientInteractionResult
        {
            InteractionId = awaitingClientResponse.ClientInteractionRequest.InteractionId,
            Contents = new List<AIContent>
            {
                new DataContent(fakePhotoBytes, "image/jpeg")
            },
            ExecutedAt = DateTime.UtcNow
        };

        // Act - Phase 2: Resume with continuation token and client result
        var resumeSettings = new SceneRequestSettings
        {
            ContinuationToken = awaitingClientResponse.ContinuationToken,
            ClientInteractionResults = new List<ClientInteractionResult> { clientResult }
        };

        var finalResponses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("", metadata: null, resumeSettings))
        {
            finalResponses.Add(response);
        }

        // Assert Phase 2: Should complete successfully
        var completedResponse = finalResponses.FirstOrDefault(r => r.Status == AiResponseStatus.Completed);
        Assert.NotNull(completedResponse);
    }

    /// <summary>
    /// Tests client interaction with error response from client.
    /// Error is caught during continuation validation (before reaching scene execution).
    /// </summary>
    [Fact]
    public async Task ClientInteraction_ErrorFromClient_ShouldHandleGracefully()
    {
        // Arrange
        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Scene,
            SceneName = "Photography"
        };

        // Act - Phase 1: Execute scene to get AwaitingClient
        AiSceneResponse? awaitingClientResponse = null;
        await foreach (var response in sceneManager.ExecuteAsync("Take a photo please", metadata: null, settings))
        {
            if (response.Status == AiResponseStatus.AwaitingClient)
            {
                awaitingClientResponse = response;
                break;
            }
        }

        Assert.NotNull(awaitingClientResponse);
        Assert.NotNull(awaitingClientResponse.ContinuationToken);
        Assert.NotNull(awaitingClientResponse.ClientInteractionRequest);

        // Simulate client error
        var clientResult = new ClientInteractionResult
        {
            InteractionId = awaitingClientResponse.ClientInteractionRequest.InteractionId,
            Error = "User denied camera permission",
            ExecutedAt = DateTime.UtcNow
        };

        // Act - Phase 2: Resume with error
        var resumeSettings = new SceneRequestSettings
        {
            ContinuationToken = awaitingClientResponse.ContinuationToken,
            ClientInteractionResults = new List<ClientInteractionResult> { clientResult }
        };

        var finalResponses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("", metadata: null, resumeSettings))
        {
            finalResponses.Add(response);
        }

        // Assert: Should return error (validation catches the client error)
        Assert.NotEmpty(finalResponses);
        var errorResponse = finalResponses.FirstOrDefault(r => r.Status == AiResponseStatus.Error);
        Assert.NotNull(errorResponse);
    }

    /// <summary>
    /// Tests client interaction with geolocation data (TextContent).
    /// </summary>
    [Fact]
    public async Task ClientInteraction_GetLocation_ShouldHandleTextContent()
    {
        // Arrange
        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Scene,
            SceneName = "Location"
        };

        // Act - Phase 1: Execute scene to get AwaitingClient
        AiSceneResponse? awaitingClientResponse = null;
        await foreach (var response in sceneManager.ExecuteAsync("What's my location?", metadata: null, settings))
        {
            if (response.Status == AiResponseStatus.AwaitingClient)
            {
                awaitingClientResponse = response;
                break;
            }
        }

        Assert.NotNull(awaitingClientResponse);
        Assert.NotNull(awaitingClientResponse.ContinuationToken);
        Assert.NotNull(awaitingClientResponse.ClientInteractionRequest);
        Assert.Equal("get_location", awaitingClientResponse.ClientInteractionRequest.ToolName);

        // Simulate client returning location as TextContent (JSON)
        var locationJson = "{\"latitude\": 45.4642, \"longitude\": 9.1900, \"accuracy\": 10}";
        var clientResult = new ClientInteractionResult
        {
            InteractionId = awaitingClientResponse.ClientInteractionRequest.InteractionId,
            Contents = new List<AIContent>
            {
                new TextContent(locationJson)
            },
            ExecutedAt = DateTime.UtcNow
        };

        // Act - Phase 2: Resume with location
        var resumeSettings = new SceneRequestSettings
        {
            ContinuationToken = awaitingClientResponse.ContinuationToken,
            ClientInteractionResults = new List<ClientInteractionResult> { clientResult }
        };

        var finalResponses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("", metadata: null, resumeSettings))
        {
            finalResponses.Add(response);
        }

        // Assert: Should complete successfully
        Assert.NotEmpty(finalResponses);
        var completedResponse = finalResponses.FirstOrDefault(r => r.Status == AiResponseStatus.Completed);
        Assert.NotNull(completedResponse);
    }

    /// <summary>
    /// Verifies that continuation token is deleted after use (single-use).
    /// </summary>
    [Fact]
    public async Task ClientInteraction_ContinuationToken_ShouldBeSingleUse()
    {
        // Arrange
        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();
        var cache = ServiceProvider.GetRequiredService<IDistributedCache>();

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Scene,
            SceneName = "Photography"
        };

        // Act - Phase 1: Get continuation token
        AiSceneResponse? awaitingClientResponse = null;
        await foreach (var response in sceneManager.ExecuteAsync("Take a photo", metadata: null, settings))
        {
            if (response.Status == AiResponseStatus.AwaitingClient)
            {
                awaitingClientResponse = response;
                break;
            }
        }

        Assert.NotNull(awaitingClientResponse);
        Assert.NotNull(awaitingClientResponse.ContinuationToken);
        var continuationToken = awaitingClientResponse.ContinuationToken!;

        // Verify token exists in cache (note: stored with "continuation:{factoryName}:" prefix)
        var cacheKey = $"continuation:default:{continuationToken}";
        var cachedData = await cache.GetAsync(cacheKey);
        Assert.NotNull(cachedData);

        // Phase 2: Resume (this should delete the token)
        var clientResult = new ClientInteractionResult
        {
            InteractionId = awaitingClientResponse.ClientInteractionRequest!.InteractionId,
            Contents = new List<AIContent> { new DataContent(new byte[] { 0xFF, 0xD8 }, "image/jpeg") },
            ExecutedAt = DateTime.UtcNow
        };

        var resumeSettings = new SceneRequestSettings
        {
            ContinuationToken = continuationToken,
            ClientInteractionResults = new List<ClientInteractionResult> { clientResult }
        };

        await foreach (var _ in sceneManager.ExecuteAsync("", metadata: null, resumeSettings))
        {
            // Just consume responses
        }

        // Assert: Token should be deleted from cache
        var cachedDataAfter = await cache.GetAsync(cacheKey);
        Assert.Null(cachedDataAfter);
    }

    /// <summary>
    /// Tests that invalid continuation token returns error.
    /// </summary>
    [Fact]
    public async Task ClientInteraction_InvalidContinuationToken_ShouldReturnError()
    {
        // Arrange
        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();

        var settings = new SceneRequestSettings
        {
            ContinuationToken = Guid.NewGuid().ToString(), // Invalid token
            ClientInteractionResults = new List<ClientInteractionResult>
            {
                new ClientInteractionResult
                {
                    InteractionId = Guid.NewGuid().ToString(),
                    Contents = new List<AIContent> { new TextContent("dummy") },
                    ExecutedAt = DateTime.UtcNow
                }
            }
        };

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("", metadata: null, settings))
        {
            responses.Add(response);
        }

        // Assert: Should return error about missing/expired continuation token
        Assert.NotEmpty(responses);
        var errorResponse = responses.FirstOrDefault(r => r.Status == AiResponseStatus.Error);
        Assert.NotNull(errorResponse);
        Assert.Contains("continuation", errorResponse.ErrorMessage ?? errorResponse.Message ?? "", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests resume with empty Contents and no Error (invalid result).
    /// ValidateResult should return false, causing an Error response.
    /// </summary>
    [Fact]
    public async Task ClientInteraction_EmptyContentsNoError_ShouldReturnError()
    {
        // Arrange
        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Scene,
            SceneName = "Photography"
        };

        // Phase 1: Get AwaitingClient
        AiSceneResponse? awaitingClientResponse = null;
        await foreach (var response in sceneManager.ExecuteAsync("Take a photo", metadata: null, settings))
        {
            if (response.Status == AiResponseStatus.AwaitingClient)
            {
                awaitingClientResponse = response;
                break;
            }
        }

        Assert.NotNull(awaitingClientResponse);
        Assert.NotNull(awaitingClientResponse.ContinuationToken);

        // Phase 2: Resume with empty contents and no error
        var clientResult = new ClientInteractionResult
        {
            InteractionId = awaitingClientResponse.ClientInteractionRequest!.InteractionId,
            Contents = new List<AIContent>(), // Empty — invalid
            ExecutedAt = DateTime.UtcNow
        };

        var resumeSettings = new SceneRequestSettings
        {
            ContinuationToken = awaitingClientResponse.ContinuationToken,
            ClientInteractionResults = new List<ClientInteractionResult> { clientResult }
        };

        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("", metadata: null, resumeSettings))
        {
            responses.Add(response);
        }

        // Assert: ValidateResult returns false for empty contents → Error
        Assert.NotEmpty(responses);
        var errorResponse = responses.FirstOrDefault(r => r.Status == AiResponseStatus.Error);
        Assert.NotNull(errorResponse);
    }

    /// <summary>
    /// Tests SceneExecutionMode.Scene with null SceneName.
    /// Should return an error, not throw.
    /// </summary>
    [Fact]
    public async Task SceneMode_NullSceneName_ShouldReturnError()
    {
        // Arrange
        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Scene,
            SceneName = null // Missing!
        };

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Hello", metadata: null, settings))
        {
            responses.Add(response);
        }

        // Assert
        Assert.NotEmpty(responses);
        var errorResponse = responses.FirstOrDefault(r => r.Status == AiResponseStatus.Error);
        Assert.NotNull(errorResponse);
    }
}

/// <summary>
/// Mock ChatClient that simulates LLM calling client-side tools.
/// Searches all conversation messages for keywords to decide which tool to call.
/// On first call: returns FunctionCallContent matching the detected tool.
/// On subsequent calls (with FunctionResultContent in history): returns text completion.
/// </summary>
internal sealed class ClientInteractionMockChatClient : IChatClient
{
    private int _callCount;

    public ChatClientMetadata Metadata => new("client-interaction-mock", null, "mock-1.0");

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _callCount++;
        var messagesList = messages.ToList();

        // Check if we have FunctionResultContent in the conversation (resume after client interaction)
        var hasFunctionResult = messagesList.Any(m =>
            m.Contents.Any(c => c is FunctionResultContent));

        if (hasFunctionResult)
        {
            return Task.FromResult(new ChatResponse(new ChatMessage
            {
                Role = ChatRole.Assistant,
                Contents = new List<AIContent>
                {
                    new TextContent("Successfully processed client data.")
                }
            }));
        }

        // Search ALL messages for keywords (not just last message, since system messages come after user message)
        var allText = string.Join(" ", messagesList.Select(m => m.Text ?? ""));

        // Detect photo/camera request
        if (allText.Contains("photo", StringComparison.OrdinalIgnoreCase) ||
            allText.Contains("camera", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new ChatResponse(new ChatMessage
            {
                Role = ChatRole.Assistant,
                Contents = new List<AIContent>
                {
                    new FunctionCallContent("capture_photo_call", "capture_photo", arguments: null)
                }
            }));
        }

        // Detect location request
        if (allText.Contains("location", StringComparison.OrdinalIgnoreCase) ||
            allText.Contains("where", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new ChatResponse(new ChatMessage
            {
                Role = ChatRole.Assistant,
                Contents = new List<AIContent>
                {
                    new FunctionCallContent("get_location_call", "get_location", arguments: null)
                }
            }));
        }

        // Default response (no tool call)
        return Task.FromResult(new ChatResponse(new ChatMessage
        {
            Role = ChatRole.Assistant,
            Contents = new List<AIContent>
            {
                new TextContent("Mock response without client interaction.")
            }
        }));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Streaming not required for client interaction tests");
    }

    public void Dispose() { }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
}