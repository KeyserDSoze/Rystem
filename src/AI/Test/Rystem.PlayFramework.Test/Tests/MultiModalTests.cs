using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework.Test.Tests;

/// <summary>
/// Tests for multi-modal input/output support
/// </summary>
public class MultiModalTests : PlayFrameworkTestBase
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
                })
                .AddMainActor("You are a test assistant for multi-modal content.")
                .AddScene("MultiModalTest", "Test scene for multi-modal support", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<IMultiModalToolService>(serviceBuilder =>
                        {
                            serviceBuilder
                                .WithMethod(
                                    x => x.GetTextFileAsync(),
                                    "get_text_file",
                                    "Returns a sample text file as DataContent")
                                .WithMethod(
                                    x => x.GetImageAsync(),
                                    "get_image",
                                    "Returns a sample image as DataContent")
                                .WithMethod(
                                    x => x.GetMultipleContentAsync(),
                                    "get_multiple_content",
                                    "Returns multiple content items (image + text)");
                        });
                });
        });

        // Register multi-modal tool service
        services.AddSingleton<IMultiModalToolService, MultiModalToolService>();

        // Override IChatClient with mock for testing
        // Uses MockChatClient by default from PlayFrameworkTestBase
    }

    [Fact]
    public async Task MultiModalInput_FromText_CreatesTextContent()
    {
        // Arrange
        var input = MultiModalInput.FromText("Hello World");

        // Act
        var message = input.ToChatMessage(ChatRole.User);

        // Assert
        Assert.NotNull(message);
        Assert.Single(message.Contents);
        Assert.IsType<TextContent>(message.Contents[0]);
        Assert.Equal("Hello World", ((TextContent)message.Contents[0]).Text);
    }

    [Fact]
    public async Task MultiModalInput_FromTextFile_CreatesDataContent()
    {
        // Arrange
        var testFile = Path.Combine("TestData", "sample.txt");
        var fileBytes = await File.ReadAllBytesAsync(testFile);

        var input = MultiModalInput.FromFileBytes("Here is a text file", fileBytes, "text/plain");
        // Set name manually on DataContent
        ((DataContent)input.Contents[0]).Name = "sample.txt";

        // Act
        var message = input.ToChatMessage(ChatRole.User);

        // Assert
        Assert.NotNull(message);
        Assert.Equal(2, message.Contents.Count()); // TextContent + DataContent
        
        var textContent = message.Contents.OfType<TextContent>().FirstOrDefault();
        Assert.NotNull(textContent);
        Assert.Equal("Here is a text file", textContent.Text);

        var dataContent = message.Contents.OfType<DataContent>().FirstOrDefault();
        Assert.NotNull(dataContent);
        Assert.Equal("text/plain", dataContent.MediaType);
        Assert.Equal("sample.txt", dataContent.Name);
        Assert.NotEmpty(dataContent.Data.ToArray());
    }

    [Fact]
    public async Task MultiModalInput_FromJsonFile_CreatesDataContent()
    {
        // Arrange
        var testFile = Path.Combine("TestData", "sample-data.json");
        var fileBytes = await File.ReadAllBytesAsync(testFile);

        var input = MultiModalInput.FromFileBytes("Process this JSON", fileBytes, "application/json");
        // Set name manually on DataContent
        ((DataContent)input.Contents[0]).Name = "sample-data.json";

        // Act
        var message = input.ToChatMessage(ChatRole.User);

        // Assert
        var dataContent = message.Contents.OfType<DataContent>().FirstOrDefault();
        Assert.NotNull(dataContent);
        Assert.Equal("application/json", dataContent.MediaType);
        Assert.Equal("sample-data.json", dataContent.Name);
        
        // Verify content can be read back
        var contentBytes = dataContent.Data.ToArray();
        var contentText = System.Text.Encoding.UTF8.GetString(contentBytes);
        Assert.Contains("Multi-Modal Test", contentText);
    }

    [Fact]
    public async Task MultiModalInput_FromImageBytes_CreatesDataContent()
    {
        // Arrange - Create mock PNG bytes (PNG magic number + minimal data)
        var pngMagicNumber = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var mockImageBytes = pngMagicNumber.Concat(new byte[100]).ToArray();

        var input = MultiModalInput.FromImageBytes("Analyze this image", mockImageBytes, "image/png");

        // Act
        var message = input.ToChatMessage(ChatRole.User);

        // Assert
        var dataContent = message.Contents.OfType<DataContent>().FirstOrDefault();
        Assert.NotNull(dataContent);
        Assert.Equal("image/png", dataContent.MediaType);
        Assert.Equal(108, dataContent.Data.Length); // 8 magic + 100 data
    }

    [Fact]
    public async Task MultiModalInput_FromImageUrl_CreatesUriContent()
    {
        // Arrange
        var imageUrl = "https://example.com/image.jpg";
        var input = MultiModalInput.FromImageUrl("Analyze this image", imageUrl, "image/jpeg");

        // Act
        var message = input.ToChatMessage(ChatRole.User);

        // Assert
        var uriContent = message.Contents.OfType<UriContent>().FirstOrDefault();
        Assert.NotNull(uriContent);
        Assert.Equal("image/jpeg", uriContent.MediaType);
        Assert.Equal(imageUrl, uriContent.Uri.ToString());
    }

    [Fact]
    public async Task MultiModalInput_FromAudioBytes_CreatesDataContent()
    {
        // Arrange - Create mock MP3 bytes (MP3 frame sync)
        var mp3Header = new byte[] { 0xFF, 0xFB, 0x90, 0x00 };
        var mockAudioBytes = mp3Header.Concat(new byte[200]).ToArray();

        var input = MultiModalInput.FromAudioBytes("Transcribe this", mockAudioBytes, "audio/mpeg");

        // Act
        var message = input.ToChatMessage(ChatRole.User);

        // Assert
        var dataContent = message.Contents.OfType<DataContent>().FirstOrDefault();
        Assert.NotNull(dataContent);
        Assert.Equal("audio/mpeg", dataContent.MediaType);
        Assert.Equal(204, dataContent.Data.Length);
    }

    [Fact]
    public async Task AiSceneResponse_HasImage_DetectsImageContent()
    {
        // Arrange
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var imageContent = new DataContent(pngBytes, "image/png");

        var response = new AiSceneResponse
        {
            Status = AiResponseStatus.Completed,
            Message = "Here's an image",
            Contents = [imageContent]
        };

        // Act & Assert
        Assert.True(response.HasImage);
        Assert.False(response.HasAudio);
        
        var retrievedImage = response.GetImage();
        Assert.NotNull(retrievedImage);
        Assert.Equal("image/png", retrievedImage.MediaType);
    }

    [Fact]
    public async Task AiSceneResponse_HasAudio_DetectsAudioContent()
    {
        // Arrange
        var mp3Bytes = new byte[] { 0xFF, 0xFB, 0x90, 0x00 };
        var audioContent = new DataContent(mp3Bytes, "audio/mpeg");

        var response = new AiSceneResponse
        {
            Status = AiResponseStatus.Completed,
            Message = "Here's audio",
            Contents = [audioContent]
        };

        // Act & Assert
        Assert.False(response.HasImage);
        Assert.True(response.HasAudio);
        
        var retrievedAudio = response.GetAudio();
        Assert.NotNull(retrievedAudio);
        Assert.Equal("audio/mpeg", retrievedAudio.MediaType);
    }

    [Fact]
    public async Task AiSceneResponse_MultipleContents_RetrievesCorrectType()
    {
        // Arrange
        var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var audioBytes = new byte[] { 0xFF, 0xFB };
        
        var response = new AiSceneResponse
        {
            Status = AiResponseStatus.Completed,
            Message = "Multiple content types",
            Contents =
            [
                new DataContent(imageBytes, "image/png"),
                new DataContent(audioBytes, "audio/mpeg"),
                new TextContent("Additional text")
            ]
        };

        // Act & Assert
        Assert.True(response.HasImage);
        Assert.True(response.HasAudio);
        Assert.Equal(3, response.Contents.Count());
        
        var image = response.GetImage();
        var audio = response.GetAudio();
        
        Assert.Equal("image/png", image?.MediaType);
        Assert.Equal("audio/mpeg", audio?.MediaType);
    }

    [Fact]
    public async Task SceneManager_AcceptsMultiModalInput()
    {
        // Arrange
        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();
        var testFile = Path.Combine("TestData", "sample.txt");
        var fileBytes = await File.ReadAllBytesAsync(testFile);

        var input = MultiModalInput.FromFileBytes("Process this file", fileBytes, "text/plain");
        ((DataContent)input.Contents[0]).Name = "sample.txt";

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync(input))
        {
            responses.Add(response);
        }

        // Assert
        Assert.NotEmpty(responses);
        var finalResponse = responses.Last();
        Assert.Equal(AiResponseStatus.Completed, finalResponse.Status);
    }

    [Fact]
    public async Task Tool_ReturnsDataContent_AddedToConversation()
    {
        // This test verifies that tools can return DataContent directly
        
        // Arrange - Create custom service provider with mock ChatClient
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddPlayFramework(builder =>
        {
            builder
                .Configure(settings =>
                {
                    settings.Planning.Enabled = false;
                    settings.Summarization.Enabled = false;
                })
                .AddScene("MultiModalTest", "Test scene for multi-modal support", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<IMultiModalToolService>(serviceBuilder =>
                        {
                            serviceBuilder
                                .WithMethod(
                                    x => x.GetTextFileAsync(),
                                    "get_text_file",
                                    "Returns a sample text file as DataContent");
                        });
                });
        });

        services.AddSingleton<IMultiModalToolService, MultiModalToolService>();
        services.AddSingleton<IChatClient>(sp => new MockGetTextFileChatClient());

        var serviceProvider = services.BuildServiceProvider();
        var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();
        var input = MultiModalInput.FromText("Get me a text file");

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Direct
        };

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync(input, metadata: null, settings))
        {
            responses.Add(response);
        }

        // Assert
        Assert.NotEmpty(responses);
        
        // Check if tool was executed
        var toolResponses = responses.Where(r => 
            r.Status == AiResponseStatus.FunctionRequest || 
            r.Status == AiResponseStatus.FunctionCompleted).ToList();
        
        Assert.NotEmpty(toolResponses);
        
        // Verify tool completed successfully
        var completed = toolResponses.Any(r => r.Status == AiResponseStatus.FunctionCompleted);
        Assert.True(completed, "Tool should complete successfully");

        // Verify the service was called
        var toolService = serviceProvider.GetRequiredService<IMultiModalToolService>() as MultiModalToolService;
        Assert.NotNull(toolService);
        Assert.True(toolService!.GetTextFileWasCalled, "GetTextFileAsync should have been called");
    }

    [Fact]
    public async Task Tool_ReturnsImage_AddedToConversation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddPlayFramework(builder =>
        {
            builder
                .Configure(settings =>
                {
                    settings.Planning.Enabled = false;
                    settings.Summarization.Enabled = false;
                })
                .AddScene("MultiModalTest", "Test scene for multi-modal support", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<IMultiModalToolService>(serviceBuilder =>
                        {
                            serviceBuilder
                                .WithMethod(
                                    x => x.GetImageAsync(),
                                    "get_image",
                                    "Returns a sample image as DataContent");
                        });
                });
        });

        services.AddSingleton<IMultiModalToolService, MultiModalToolService>();
        services.AddSingleton<IChatClient>(sp => new MockGetImageChatClient());

        var serviceProvider = services.BuildServiceProvider();
        var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();
        var input = MultiModalInput.FromText("Get me an image");

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Direct
        };

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync(input, metadata: null, settings))
        {
            responses.Add(response);
        }

        // Assert
        Assert.NotEmpty(responses);
        var toolService = serviceProvider.GetRequiredService<IMultiModalToolService>() as MultiModalToolService;
        Assert.NotNull(toolService);
        Assert.True(toolService!.GetImageWasCalled, "GetImageAsync should have been called");
    }

    [Fact]
    public async Task Tool_ReturnsMultipleContent_AddedToConversation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddPlayFramework(builder =>
        {
            builder
                .Configure(settings =>
                {
                    settings.Planning.Enabled = false;
                    settings.Summarization.Enabled = false;
                })
                .AddScene("MultiModalTest", "Test scene for multi-modal support", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<IMultiModalToolService>(serviceBuilder =>
                        {
                            serviceBuilder
                                .WithMethod(
                                    x => x.GetMultipleContentAsync(),
                                    "get_multiple_content",
                                    "Returns multiple content items");
                        });
                });
        });

        services.AddSingleton<IMultiModalToolService, MultiModalToolService>();
        services.AddSingleton<IChatClient>(sp => new MockGetMultipleContentChatClient());

        var serviceProvider = services.BuildServiceProvider();
        var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();
        var input = MultiModalInput.FromText("Get me multiple content items");

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Direct
        };

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync(input, metadata: null, settings))
        {
            responses.Add(response);
        }

        // Assert
        Assert.NotEmpty(responses);
        var toolService = serviceProvider.GetRequiredService<IMultiModalToolService>() as MultiModalToolService;
        Assert.NotNull(toolService);
        Assert.True(toolService!.GetMultipleContentWasCalled, "GetMultipleContentAsync should have been called");
    }
}

#region Multi-Modal Tool Service

/// <summary>
/// Service interface for multi-modal tools
/// </summary>
public interface IMultiModalToolService
{
    /// <summary>
    /// Returns a text file as DataContent
    /// </summary>
    Task<DataContent> GetTextFileAsync();

    /// <summary>
    /// Returns an image as DataContent
    /// </summary>
    Task<DataContent> GetImageAsync();

    /// <summary>
    /// Returns multiple content items
    /// </summary>
    Task<List<AIContent>> GetMultipleContentAsync();
}

/// <summary>
/// Implementation of multi-modal tool service
/// </summary>
public class MultiModalToolService : IMultiModalToolService
{
    public bool GetTextFileWasCalled { get; private set; }
    public bool GetImageWasCalled { get; private set; }
    public bool GetMultipleContentWasCalled { get; private set; }

    public async Task<DataContent> GetTextFileAsync()
    {
        GetTextFileWasCalled = true;

        var testFile = Path.Combine("TestData", "sample.txt");
        var fileBytes = await File.ReadAllBytesAsync(testFile);

        return new DataContent(fileBytes, "text/plain")
        {
            Name = "sample.txt"
        };
    }

    public Task<DataContent> GetImageAsync()
    {
        GetImageWasCalled = true;

        // Create mock PNG bytes (PNG magic number + minimal data)
        var pngMagicNumber = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var mockImageBytes = pngMagicNumber.Concat(new byte[100]).ToArray();

        return Task.FromResult(new DataContent(mockImageBytes, "image/png")
        {
            Name = "generated-image.png"
        });
    }

    public Task<List<AIContent>> GetMultipleContentAsync()
    {
        GetMultipleContentWasCalled = true;

        var contents = new List<AIContent>
        {
            new TextContent("Here are multiple content items:"),
            new DataContent(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, "image/png")
            {
                Name = "multi-content-image.png"
            },
            new UriContent(new Uri("https://example.com/document.pdf"), "application/pdf")
        };

        return Task.FromResult(contents);
    }
}

#endregion

#region Mock Chat Clients

/// <summary>
/// Mock ChatClient for get_text_file tool testing
/// </summary>
internal class MockGetTextFileChatClient : IChatClient
{
    private int _callCount = 0;
    public ChatClientMetadata Metadata => new("mock-multimodal-text", null, "mock-1.0");

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);
        _callCount++;

        var responseMessage = new ChatMessage(ChatRole.Assistant, "Processing");

        if (options?.Tools?.Count > 0)
        {
            // First call: scene selection
            if (_callCount == 1)
            {
                var sceneSelectionCall = new FunctionCallContent(
                    callId: Guid.NewGuid().ToString(),
                    name: "MultiModalTest",
                    arguments: new Dictionary<string, object?>());

                responseMessage.Contents.Add(sceneSelectionCall);
            }
            // Second call: tool execution
            else
            {
                var functionCall = new FunctionCallContent(
                    callId: Guid.NewGuid().ToString(),
                    name: "get_text_file",
                    arguments: new Dictionary<string, object?>());

                responseMessage.Contents.Add(functionCall);
            }
        }

        return new ChatResponse([responseMessage]) { ModelId = "mock-model" };
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public void Dispose() { }
    public object? GetService(Type serviceType, object? serviceKey = null) => null;
}

/// <summary>
/// Mock ChatClient for get_image tool testing
/// </summary>
internal class MockGetImageChatClient : IChatClient
{
    private int _callCount = 0;
    public ChatClientMetadata Metadata => new("mock-multimodal-image", null, "mock-1.0");

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);
        _callCount++;

        var responseMessage = new ChatMessage(ChatRole.Assistant, "Processing");

        if (options?.Tools?.Count > 0)
        {
            if (_callCount == 1)
            {
                var sceneSelectionCall = new FunctionCallContent(
                    callId: Guid.NewGuid().ToString(),
                    name: "MultiModalTest",
                    arguments: new Dictionary<string, object?>());

                responseMessage.Contents.Add(sceneSelectionCall);
            }
            else
            {
                var functionCall = new FunctionCallContent(
                    callId: Guid.NewGuid().ToString(),
                    name: "get_image",
                    arguments: new Dictionary<string, object?>());

                responseMessage.Contents.Add(functionCall);
            }
        }

        return new ChatResponse([responseMessage]) { ModelId = "mock-model" };
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public void Dispose() { }
    public object? GetService(Type serviceType, object? serviceKey = null) => null;
}

/// <summary>
/// Mock ChatClient for get_multiple_content tool testing
/// </summary>
internal class MockGetMultipleContentChatClient : IChatClient
{
    private int _callCount = 0;
    public ChatClientMetadata Metadata => new("mock-multimodal-multiple", null, "mock-1.0");

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);
        _callCount++;

        var responseMessage = new ChatMessage(ChatRole.Assistant, "Processing");

        if (options?.Tools?.Count > 0)
        {
            if (_callCount == 1)
            {
                var sceneSelectionCall = new FunctionCallContent(
                    callId: Guid.NewGuid().ToString(),
                    name: "MultiModalTest",
                    arguments: new Dictionary<string, object?>());

                responseMessage.Contents.Add(sceneSelectionCall);
            }
            else
            {
                var functionCall = new FunctionCallContent(
                    callId: Guid.NewGuid().ToString(),
                    name: "get_multiple_content",
                    arguments: new Dictionary<string, object?>());

                responseMessage.Contents.Add(functionCall);
            }
        }

        return new ChatResponse([responseMessage]) { ModelId = "mock-model" };
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public void Dispose() { }
    public object? GetService(Type serviceType, object? serviceKey = null) => null;
}

#endregion

