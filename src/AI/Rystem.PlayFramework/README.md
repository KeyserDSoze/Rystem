# Rystem.PlayFramework

[![NuGet](https://img.shields.io/nuget/v/Rystem.PlayFramework)](https://www.nuget.org/packages/Rystem.PlayFramework)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**Rystem.PlayFramework** is a .NET 10 orchestration framework for building multi-agent AI applications. It provides a fluent API to compose **Scenes** (task contexts with tools and actors), **Actors** (system prompts), and execution **Modes** (direct, planning, dynamic chaining), with built-in support for caching, cost tracking, rate limiting, RAG, web search, MCP servers, conversation memory, client-side tool execution, OpenTelemetry, and SSE streaming.

## Installation

```bash
dotnet add package Rystem.PlayFramework
```

> Requires **.NET 10** and an `IChatClient` implementation (e.g. Azure OpenAI, Ollama, or any `Microsoft.Extensions.AI`-compatible provider).

## Quick Start

```csharp
using Rystem.PlayFramework;
using Rystem.PlayFramework.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSingleton<IChatClient>(/* your chat-client */);

builder.Services.AddPlayFramework(framework =>
{
    framework
        .AddMainActor("You are a helpful AI assistant.")
        .AddScene("Chat", "General conversation", scene =>
        {
            scene.WithActors(actors =>
            {
                actors.AddActor("Provide clear and concise answers.");
            });
        });
});

var app = builder.Build();

app.MapPlayFramework(settings =>
{
    settings.BasePath = "/api/ai";
});

app.Run();
```

This exposes two SSE endpoints per factory:

| Endpoint | Description |
|----------|-------------|
| `POST /api/ai/{factoryName}` | Step-by-step streaming (one SSE event per pipeline step) |
| `POST /api/ai/{factoryName}/streaming` | Token-level streaming (one SSE event per text chunk) |

---

## Core Concepts

### Scenes

A **Scene** is a named execution context that groups actors, tools, and optional configuration. The framework routes user messages to the appropriate scene based on the execution mode.

```csharp
framework.AddScene("CustomerSupport", "Handle customer inquiries", scene =>
{
    scene
        .WithActors(actors =>
        {
            actors.AddActor("You are a customer support specialist.");
            actors.AddActor(context => $"Customer tier: {context.Metadata["tier"]}");
        })
        .WithService<IOrderService>(tools =>
        {
            tools.WithMethod(x => x.GetOrderAsync(default!, default), "GetOrder", "Retrieve an order by ID");
        });
});
```

### Actors

**Actors** are system prompts injected into scene execution. They can be static, dynamic (context-aware), async, or custom classes implementing `IActor`.

```csharp
// Static
actors.AddActor("Be concise.", cacheForSubsequentCalls: true);

// Dynamic
actors.AddActor(context => $"User region: {context.Metadata["region"]}");

// Async
actors.AddActor(async (context, ct) => await LoadPromptFromDb(ct));

// Custom class
actors.AddActor<ComplianceActor>();
```

### Execution Modes

| Mode | Description |
|------|-------------|
| `Direct` | Single scene execution, fastest path |
| `Planning` | AI creates an upfront execution plan across multiple scenes |
| `DynamicChaining` | AI selects the next scene dynamically based on previous results |
| `Scene` | Execute a specific scene by name (set via `SceneName` in request settings) |

```csharp
framework.WithExecutionMode(SceneExecutionMode.Planning);
```

---

## Named / Factory Instances

Register multiple PlayFramework instances for multi-tenant, A/B testing, or tiered scenarios:

```csharp
services.AddPlayFramework("basic", builder => { /* ... */ });
services.AddPlayFramework("premium", builder => { /* ... */ });
services.AddPlayFramework(UserTier.Enterprise, builder => { /* ... */ });
```

Resolve via `IPlayFramework`:

```csharp
public interface IPlayFramework
{
    ISceneManager GetDefault();
    ISceneManager Get(AnyOf<string?, Enum> name);
    ISceneManager? Create(AnyOf<string?, Enum>? name = null);
    bool Exists(AnyOf<string?, Enum>? name = null);
}
```

---

## Service Tools

Expose any service method as an AI-callable tool:

```csharp
scene.WithService<IWeatherService>(tools =>
{
    tools.WithMethod(x => x.GetForecastAsync(default!, default), "GetWeather", "Get weather forecast");
});
```

The framework extracts parameter metadata from the expression tree and registers it as an AI function tool.

---

## Client-Side Tool Execution

Delegate tool execution to the client (browser, mobile app) when the server cannot access device capabilities (GPS, camera, user confirmation, etc.).

### Server Registration

```csharp
scene.OnClient(client =>
{
    client.AddTool("getCurrentLocation",
        "Gets user GPS coordinates from the browser",
        timeoutSeconds: 15);

    client.AddTool<UserConfirmationArgs>("getUserConfirmation",
        "Ask for explicit user confirmation",
        timeoutSeconds: 60);
});
```

### Flow

1. AI decides to call a client tool → server responds with `AwaitingClient` status, a `ContinuationToken`, and a `ClientInteractionRequest`.
2. Client executes the tool locally (e.g. `navigator.geolocation`).
3. Client sends back the `ContinuationToken` + `ClientInteractionResult` to resume execution.

> Requires `IDistributedCache` (e.g. `AddDistributedMemoryCache()` or Redis).

---

## MCP Integration (Model Context Protocol)

Attach remote or in-memory MCP servers to scenes:

```csharp
// Remote MCP server
services.AddMcpServer("https://mcp.example.com", "my-mcp", settings =>
{
    settings.AuthorizationHeader = "Bearer token";
    settings.TimeoutSeconds = 30;
});

// In-memory MCP server (for testing)
services.AddInMemoryMcpServer("test-mcp", server =>
{
    server.AddTool("calculate", "Calculator", inputSchema, async args => "42");
    server.AddResource("config", "config://app", "App config", content: "...");
    server.AddPrompt("greeting", "Greeting prompt");
});

// Attach to scene with optional filtering
scene.WithMcpServer("my-mcp", filter =>
{
    filter.Tools = ["tool1", "tool2"];
    filter.ToolsRegex = "^calc_.*";
});
```

---

## RAG (Retrieval-Augmented Generation)

Add vector search context to scenes:

```csharp
// Global RAG
framework.WithRag(settings =>
{
    settings.TopK = 10;
    settings.SearchAlgorithm = VectorSearchAlgorithm.CosineSimilarity;
    settings.MinimumScore = 0.7;
}, "azure");

// Scene-level override
scene.WithRag(settings => settings.TopK = 5, "azure");
scene.WithoutRag("azure"); // disable for a specific scene
```

Implement `IRagService` to provide the search backend.

---

## Web Search

Enrich AI responses with live web results:

```csharp
framework.WithWebSearch(settings =>
{
    settings.MaxResults = 5;
    settings.SafeSearch = true;
    settings.Freshness = WebSearchFreshness.Week;
    settings.Market = "en-US";
}, "bing");
```

Implement `IWebSearchService` to provide the search backend.

---

## Conversation Memory

Persist and summarize conversation history across requests:

```csharp
framework.WithMemory(memory =>
{
    memory.WithDefaultMemoryStorage("userId", "sessionId");
    memory.WithMaxSummaryLength(2000);
    memory.WithIncludePreviousMemory(true);

    // Or custom storage (e.g. Redis, SQL):
    // memory.WithCustomStorage<RedisMemoryStorage>();
});
```

Memory is keyed by `ConversationKey` (sent in request settings) and automatically manages summarization of long conversations.

---

## Caching

```csharp
framework.AddCache(cache =>
{
    cache.WithMemory();           // In-memory
    cache.WithDistributed();      // IDistributedCache (Redis, SQL, etc.)
    cache.WithCustomCache<T>();   // Custom ICacheService
    cache.Configure(s => s.DefaultExpirationSeconds = 300);
});
```

Per-request cache behavior via `SceneRequestSettings.CacheBehavior`:

| Value | Description |
|-------|-------------|
| `Default` | Normal caching |
| `Avoidable` | Skip cache for this request |
| `Forever` | Cache indefinitely |

---

## Cost Tracking & Budget Limits

```csharp
framework
    .WithCostTracking("USD", inputCostPer1K: 0.01m, outputCostPer1K: 0.03m)
    .WithModelCosts("gpt-4o", inputCostPer1K: 0.03m, outputCostPer1K: 0.06m);
```

Set per-request budget limits:

```csharp
var settings = new SceneRequestSettings { MaxBudget = 0.50m };
```

When `TotalCost` exceeds `MaxBudget`, execution stops and returns `AiResponseStatus.BudgetExceeded`.

Every `AiSceneResponse` includes `InputTokens`, `OutputTokens`, `Cost`, and `TotalCost`.

---

## Rate Limiting

```csharp
framework.WithRateLimit(limit => limit
    .GroupBy("userId")
    .TokenBucket(capacity: 100, refillRate: 10)
    .WaitOnExceeded(TimeSpan.FromSeconds(30)));
```

| Strategy | Description |
|----------|-------------|
| `TokenBucket` | Steady-rate with burst capacity |
| `FixedWindow` | Fixed time windows |
| `SlidingWindow` | Rolling time windows |
| `Concurrent` | Max concurrent requests |

| Behavior | Description |
|----------|-------------|
| `Wait` | Queue until capacity is available |
| `Reject` | Immediately reject (429) |
| `Fallback` | Route to fallback client |

---

## Load Balancing & Fallback

```csharp
framework.Configure(settings =>
{
    settings.ChatClientNames = ["gpt-4o-1", "gpt-4o-2", "gpt-4o-3"];
    settings.LoadBalancingMode = LoadBalancingMode.RoundRobin;

    settings.FallbackChatClientNames = ["claude-sonnet", "llama-local"];
    settings.FallbackMode = FallbackMode.Sequential;

    settings.MaxRetryAttempts = 3;
    settings.RetryBaseDelaySeconds = 1.0;
});
```

---

## OpenTelemetry

Built-in distributed tracing and metrics:

```csharp
framework.Configure(settings =>
{
    settings.Telemetry.EnableTracing = true;
    settings.Telemetry.EnableMetrics = true;
    settings.Telemetry.TraceScenes = true;
    settings.Telemetry.TraceTools = true;
    settings.Telemetry.TraceLlmCalls = true;
    settings.Telemetry.TracePlanning = true;
    settings.Telemetry.TraceMcpOperations = true;
    settings.Telemetry.SamplingRate = 1.0;

    // PII-sensitive (disabled by default)
    settings.Telemetry.IncludeLlmPrompts = false;
    settings.Telemetry.IncludeLlmResponses = false;
});
```

---

## Multi-Modal Input

```csharp
var input = MultiModalInput.FromText("Describe this image");
var input = MultiModalInput.FromImageUrl("What's in this?", "https://example.com/img.png");
var input = MultiModalInput.FromImageBytes("Analyze", bytes, "image/png");
var input = MultiModalInput.FromAudioUrl("Transcribe", "https://example.com/audio.mp3");
var input = MultiModalInput.FromAudioBytes("Transcribe", audioBytes, "audio/mp3");
var input = MultiModalInput.FromFileUrl("Summarize", "https://example.com/doc.pdf");
var input = MultiModalInput.FromFileBytes("Parse", docBytes, "application/pdf");
```

The `AiSceneResponse` provides convenience accessors for multi-modal output: `HasImage`, `HasAudio`, `GetImage()`, `GetAllImages()`, etc.

---

## Per-Request Settings

Override any global setting at request time via `SceneRequestSettings`:

| Property | Description |
|----------|-------------|
| `ExecutionMode` | Override execution mode |
| `MaxRecursionDepth` | Max planning depth (default: 5) |
| `EnableSummarization` | Toggle summarization |
| `EnableDirector` | Toggle director |
| `EnableStreaming` | Token-level streaming |
| `ModelId` | Override model |
| `Temperature` | Override temperature |
| `MaxTokens` | Override max tokens |
| `CacheBehavior` | Cache behavior |
| `MaxBudget` | Budget limit |
| `MaxDynamicScenes` | Max scenes in dynamic chaining |
| `ConversationKey` | Conversation ID for memory/cache |
| `ContinuationToken` | Resume from client interaction |
| `SceneName` | Direct scene execution |
| `ClientInteractionResults` | Client tool results |

---

## Response Model

Every `AiSceneResponse` in the `IAsyncEnumerable<AiSceneResponse>` stream includes:

| Property | Description |
|----------|-------------|
| `Status` | Current pipeline status (see below) |
| `SceneName` | Scene being executed |
| `Message` | Response text |
| `StreamingChunk` | Token-level chunk (when streaming) |
| `FunctionName` / `FunctionArguments` | Tool call info |
| `InputTokens` / `OutputTokens` / `TotalTokens` | Token usage |
| `Cost` / `TotalCost` | Cost tracking |
| `ConversationKey` | Conversation ID |
| `ContinuationToken` | For client interaction resumption |
| `ClientInteractionRequest` | Client tool request details |
| `Contents` | Multi-modal output content |
| `ErrorMessage` | Error details |
| `Metadata` | Additional data |

### Response Statuses

| Status | Description |
|--------|-------------|
| `Initializing` | Pipeline starting |
| `LoadingCache` | Checking cache |
| `ExecutingMainActors` | Running global actors |
| `Planning` | Creating execution plan |
| `ExecutingScene` | Running a scene |
| `FunctionRequest` | AI requested a tool call |
| `FunctionCompleted` | Tool call completed |
| `ToolSkipped` | Tool call skipped |
| `Streaming` | Token streaming in progress |
| `Running` | General execution |
| `Summarizing` | Summarizing conversation |
| `DirectorDecision` | Director evaluating results |
| `GeneratingFinalResponse` | Generating final response |
| `SavingCache` | Persisting to cache |
| `AwaitingClient` | Waiting for client tool execution |
| `Completed` | Execution finished |
| `BudgetExceeded` | Budget limit reached |
| `Error` | Error occurred |

---

## HTTP API

### Request Body

```json
{
  "message": "What's the weather like?",
  "contents": [
    { "type": "image", "data": "base64...", "mediaType": "image/png" }
  ],
  "metadata": {
    "userId": "u123",
    "tenantId": "t1"
  },
  "settings": {
    "executionMode": "Planning",
    "conversationKey": "conv-abc",
    "enableStreaming": true,
    "maxBudget": 0.5
  },
  "continuationToken": "...",
  "clientInteractionResults": [
    {
      "interactionId": "interaction-id",
      "contents": [{ "type": "text", "data": "42.3601,-71.0589" }]
    }
  ]
}
```

### API Settings

```csharp
app.MapPlayFramework(settings =>
{
    settings.BasePath = "/api/ai";
    settings.RequireAuthentication = true;
    settings.EnableAutoMetadata = true;
    settings.EnableCompression = true;
    settings.MaxRequestBodySize = 10_485_760;
    settings.AuthorizationPolicies = ["ReadAccess"];
    settings.FactoryPolicies = new() { ["premium"] = ["PremiumUser"] };
});
```

---

## Extensibility Points

| Interface | Purpose | Default |
|-----------|---------|---------|
| `IActor` | Custom actor logic | N/A (user implements) |
| `IPlanner` | Custom planning strategy | `DeterministicPlanner` |
| `ISummarizer` | Custom summarization | `DefaultSummarizer` |
| `IDirector` | Multi-scene orchestration | `MainDirector` |
| `ICacheService` | Custom cache backend | `CacheService` |
| `IJsonService` | Custom JSON serialization | `DefaultJsonService` |
| `IMemory` | Custom memory summarization | `Memory` |
| `IMemoryStorage` | Custom memory persistence | `InMemoryMemoryStorage` |
| `IRagService` | RAG search backend | User must implement |
| `IWebSearchService` | Web search backend | User must implement |
| `IRateLimiter` | Custom rate limiter | `TokenBucketRateLimiter` |
| `IRateLimitStorage` | Rate limit state store | `InMemoryRateLimitStorage` |

Register custom implementations:

```csharp
framework.AddCustomPlanner<MyPlanner>();
framework.AddCustomSummarizer<MySummarizer>();
framework.AddCustomDirector<MyDirector>();
framework.AddCustomJsonService<MyJsonService>();
```

---

## Programmatic Usage

Use `ISceneManager` directly without HTTP:

```csharp
public class ChatService(IPlayFramework playFramework)
{
    public async IAsyncEnumerable<AiSceneResponse> ChatAsync(string message)
    {
        var manager = playFramework.GetDefault();

        await foreach (var response in manager.ExecuteAsync(message,
            settings: new SceneRequestSettings
            {
                ConversationKey = "user-123",
                EnableStreaming = true,
                MaxBudget = 1.0m
            }))
        {
            yield return response;
        }
    }
}
```

---

## Full Example

```csharp
using Rystem.PlayFramework;
using Rystem.PlayFramework.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSingleton<IChatClient>(/* your provider */);

builder.Services.AddPlayFramework(framework =>
{
    framework
        .Configure(settings =>
        {
            settings.Planning.Enabled = true;
            settings.Summarization.Enabled = true;
            settings.DefaultExecutionMode = SceneExecutionMode.Direct;
        })
        .WithCostTracking("USD", inputCostPer1K: 0.01m, outputCostPer1K: 0.03m)
        .WithMemory(memory =>
        {
            memory.WithDefaultMemoryStorage("userId");
            memory.WithMaxSummaryLength(2000);
        })
        .WithRateLimit(limit => limit
            .GroupBy("userId")
            .TokenBucket(capacity: 50, refillRate: 5)
            .WaitOnExceeded(TimeSpan.FromSeconds(10)))
        .AddMainActor("You are a helpful AI assistant.")
        .AddScene("Chat", "General conversation", scene =>
        {
            scene.WithActors(actors =>
            {
                actors.AddActor("Be clear and concise.");
            });
        })
        .AddScene("Research", "Deep research with web search", scene =>
        {
            scene
                .WithActors(actors =>
                {
                    actors.AddActor("Search the web and cite sources.");
                })
                .WithWebSearch(settings =>
                {
                    settings.MaxResults = 5;
                    settings.Freshness = WebSearchFreshness.Week;
                }, "bing");
        })
        .AddScene("Assistant", "Interactive assistant with client tools", scene =>
        {
            scene
                .WithActors(actors =>
                {
                    actors.AddActor("You can interact with the user's device.");
                })
                .OnClient(client =>
                {
                    client.AddTool("getCurrentLocation",
                        "Gets user GPS coordinates",
                        timeoutSeconds: 15);
                    client.AddTool<ConfirmationArgs>("getUserConfirmation",
                        "Ask user for confirmation",
                        timeoutSeconds: 60);
                });
        });
});

var app = builder.Build();

app.UseCors();
app.MapPlayFramework(settings =>
{
    settings.BasePath = "/api/ai";
    settings.RequireAuthentication = false;
    settings.EnableCompression = true;
});

app.Run();
```

---

## License

MIT - see [LICENSE](https://github.com/KeyserDSoze/Rystem/blob/master/LICENSE.txt).

## Links

- [NuGet Package](https://www.nuget.org/packages/Rystem.PlayFramework)
- [GitHub Repository](https://github.com/KeyserDSoze/Rystem/tree/master/src/AI/Rystem.PlayFramework)
- [Full Documentation](https://rystem.net)
