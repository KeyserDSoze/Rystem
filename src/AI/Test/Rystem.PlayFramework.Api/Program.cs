using Azure.AI.OpenAI;
using Azure;
using Rystem.PlayFramework;
using Rystem.PlayFramework.Api;
using Microsoft.Extensions.AI;
using System.ComponentModel;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configure CORS for TypeScript client
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// In-memory distributed cache (required for OnClient continuation tokens)
builder.Services.AddMemoryCache();

builder.Services.AddOpenApi();

// Configure Azure OpenAI (from user secrets or appsettings)
var azureOpenAIEndpoint = builder.Configuration["AzureOpenAI:Endpoint"];
var azureOpenAIKey = builder.Configuration["AzureOpenAI:Key"];
var azureOpenAIDeployment = builder.Configuration["AzureOpenAI:Deployment"] ?? "gpt-4o";

if (!string.IsNullOrEmpty(azureOpenAIEndpoint) && !string.IsNullOrEmpty(azureOpenAIKey))
{
    builder.Services.AddSingleton<IChatClient>(_ =>
        new AzureOpenAIChatClientAdapter(azureOpenAIEndpoint, azureOpenAIKey, azureOpenAIDeployment));
}
else
{
    // Fallback to mock implementation for demo purposes
    builder.Services.AddSingleton<IChatClient>(new MockChatClient());
}

// Configure PlayFramework with Chat scene
builder.Services.AddPlayFramework("default", frameworkBuilder =>
{
    frameworkBuilder
        .AddCache(cacheBuilder =>
        {
            cacheBuilder
                .WithMemory()
                .WithExpiration(TimeSpan.FromMinutes(30));
        })
        .WithPlanning(planningSettings =>
        {
            planningSettings.MaxRecursionDepth = 5;
        })
        .WithRetry(maxAttempts: 3, baseDelaySeconds: 1.0)
        .WithTelemetry(telemetryBuilder =>
        {
            telemetryBuilder.EnableMetrics = true;
            telemetryBuilder.TraceSummarization = true;
            telemetryBuilder.TraceDirector = true;
            telemetryBuilder.TraceLlmCalls = true;
        })
        .AddMainActor("You are a helpful AI assistant. You help users with their questions and tasks in a friendly and professional manner.")
        .AddScene("General Requests", "Use this scene for every request. General conversation and question answering. ", sceneBuilder =>
        {
            sceneBuilder
                .WithActors(actorBuilder =>
                {
                    actorBuilder
                        .AddActor("Provide clear, concise, and accurate answers.")
                        .AddActor("Be friendly and engaging in conversation.")
                        .AddActor("If you don't know something, admit it honestly.");
                })
                .OnClient(clientBuilder =>
                {
                    clientBuilder
                        .AddTool("getCurrentLocation",
                            "Gets the user's current geographic location (latitude, longitude) from the browser. Use when the user asks about nearby places, weather, or anything location-dependent.",
                            timeoutSeconds: 15)
                        .AddTool<UserConfirmationArgs>("getUserConfirmation",
                            "Asks the user for explicit confirmation before performing a sensitive or irreversible action. Use when the user requests something that needs a yes/no decision.",
                            timeoutSeconds: 60);
                });
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Servers = [new ScalarServer("http://localhost:5158", "Local Development")];
        options.Title = "PlayFramework API";
        options.Theme = ScalarTheme.BluePlanet;
    });
}

app.UseCors();
app.UseHttpsRedirection();

// Map PlayFramework HTTP endpoints
// POST /api/ai/{factoryName} - Step-by-step streaming (each PlayFramework step)
// POST /api/ai/{factoryName}/streaming - Token-level streaming (each text chunk)
app.MapPlayFramework("default", settings =>
{
    settings.BasePath = "/api/ai";
    settings.RequireAuthentication = false; // Set to true for production
    settings.EnableCompression = true;
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck");

app.Run();

// Mock IChatClient implementation for demo without Azure OpenAI
public class MockChatClient : IChatClient
{
    public ChatClientMetadata Metadata => new("MockChatClient", null, "mock-model");

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Delay(500, cancellationToken);

        var words = new[]
        {
            "Hello! ", "I'm ", "a ", "mock ", "AI ", "assistant. ",
            "I ", "can ", "help ", "you ", "test ", "the ", "PlayFramework ",
            "API. ", "Configure ", "Azure ", "OpenAI ", "credentials ", "for ",
            "real ", "AI ", "responses!"
        };

        foreach (var word in words)
        {
            await Task.Delay(50, cancellationToken);
            yield return new ChatResponseUpdate(ChatRole.Assistant, word);
        }
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(500, cancellationToken);

        return new ChatResponse(
        [
            new ChatMessage(ChatRole.Assistant,
                "Hello! I'm a mock AI assistant. I can help you test the PlayFramework API. Configure Azure OpenAI credentials for real AI responses!")
        ])
        {
            ModelId = "mock-model"
        };
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}

// Adapter for Azure OpenAI that implements IChatClient from Microsoft.Extensions.AI
public sealed class AzureOpenAIChatClientAdapter : IChatClient
{
    private readonly Azure.AI.OpenAI.AzureOpenAIClient _azureClient;
    private readonly OpenAI.Chat.ChatClient _chatClient;
    private readonly string _deploymentName;

    public AzureOpenAIChatClientAdapter(string endpoint, string apiKey, string deploymentName)
    {
        _deploymentName = deploymentName;
        _azureClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        _chatClient = _azureClient.GetChatClient(deploymentName);
    }

    public ChatClientMetadata Metadata => new(
        providerName: "AzureOpenAI",
        providerUri: new Uri("https://azure.microsoft.com/products/ai-services/openai-service"),
        _deploymentName);

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Convert AI messages to OpenAI messages
        var openAiMessages = new List<OpenAI.Chat.ChatMessage>();
        foreach (var msg in messages)
        {
            var text = msg.Text ?? string.Empty;
            openAiMessages.Add(msg.Role.Value switch
            {
                "system" => OpenAI.Chat.ChatMessage.CreateSystemMessage(text),
                "user" => OpenAI.Chat.ChatMessage.CreateUserMessage(text),
                "assistant" => OpenAI.Chat.ChatMessage.CreateAssistantMessage(text),
                _ => OpenAI.Chat.ChatMessage.CreateUserMessage(text)
            });
        }

        var response = await _chatClient.CompleteChatAsync(openAiMessages, cancellationToken: cancellationToken);

        var responseMessages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.Assistant, response.Value.Content[0].Text)
        };

        return new ChatResponse(responseMessages)
        {
            ModelId = _deploymentName
        };
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var openAiMessages = new List<OpenAI.Chat.ChatMessage>();
        foreach (var msg in messages)
        {
            var text = msg.Text ?? string.Empty;
            openAiMessages.Add(msg.Role.Value switch
            {
                "system" => OpenAI.Chat.ChatMessage.CreateSystemMessage(text),
                "user" => OpenAI.Chat.ChatMessage.CreateUserMessage(text),
                "assistant" => OpenAI.Chat.ChatMessage.CreateAssistantMessage(text),
                _ => OpenAI.Chat.ChatMessage.CreateUserMessage(text)
            });
        }

        await foreach (var streamUpdate in _chatClient.CompleteChatStreamingAsync(openAiMessages, cancellationToken: cancellationToken))
        {
            foreach (var contentPart in streamUpdate.ContentUpdate)
            {
                yield return new ChatResponseUpdate(ChatRole.Assistant, contentPart.Text);
            }
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}

/// <summary>
/// Arguments model for the getUserConfirmation client tool.
/// JSON Schema is auto-generated and sent to the LLM.
/// </summary>
public sealed class UserConfirmationArgs
{
    /// <summary>
    /// The question to ask the user for confirmation.
    /// </summary>
    [Description("The question to present to the user, e.g. 'Do you want to proceed with deleting your account?'")]
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// Label for the confirm button (default: "Yes").
    /// </summary>
    [Description("Text for the confirm/accept button")]
    public string ConfirmLabel { get; set; } = "Yes";

    /// <summary>
    /// Label for the cancel button (default: "No").
    /// </summary>
    [Description("Text for the cancel/reject button")]
    public string CancelLabel { get; set; } = "No";
}
