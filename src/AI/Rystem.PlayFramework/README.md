# Rystem.PlayFramework

`Rystem.PlayFramework` is the orchestration core of the AI area.

It is a named, scene-based execution framework built around `Microsoft.Extensions.AI` chat clients. You register one or more PlayFramework instances, attach scenes, actors, tools, cache, persistence, and optional voice, memory, telemetry, or rate-limit behavior, then execute them either programmatically or through HTTP SSE endpoints.

## Installation

```bash
dotnet add package Rystem.PlayFramework
```

For real model access you also need one or more chat-client registrations. In practice that usually means an adapter package such as:

```bash
dotnet add package Rystem.PlayFramework.Adapters
```

If you want HTTP endpoints, the HTTP mapping extensions live in this package under the `Rystem.PlayFramework.Api` namespace. There is no separate `Rystem.PlayFramework.Api` library package in this repo.

## Architecture

The core entry points are:

- `AddPlayFramework(...)`
- `MapPlayFramework(...)`
- `ISceneManager`
- `IPlayFramework`

The lifecycle is:

1. register a named PlayFramework instance
2. attach one or more named `IChatClient` registrations
3. add scenes, actors, and tools
4. optionally enable cache, repository persistence, memory, telemetry, rate limiting, and voice
5. execute through `ISceneManager.ExecuteAsync(...)` or the HTTP API

Each PlayFramework instance is factory-based. The name can be a `string` or `Enum`, and resolution happens through `IFactory<ISceneManager>` or the `IPlayFramework` wrapper.

## Example: minimal HTTP backend

This follows the real patterns used in `src/AI/Test/Rystem.PlayFramework.Api/Program.cs` and the factory tests.

```csharp
using RepositoryFramework;
using Rystem.PlayFramework;
using Rystem.PlayFramework.Adapters;
using Rystem.PlayFramework.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();

builder.Services.AddAdapterForAzureOpenAI("default", settings =>
{
    settings.Endpoint = new Uri(builder.Configuration["AzureOpenAI:Endpoint"]!);
    settings.ApiKey = builder.Configuration["AzureOpenAI:Key"]!;
    settings.Deployment = builder.Configuration["AzureOpenAI:Deployment"] ?? "gpt-4o";
});

builder.Services.AddSingleton<ICalculatorService, CalculatorService>();

builder.Services.AddPlayFramework("default", framework =>
{
    framework
        .WithChatClient("default")
        .UseDefaultGuardrails()
        .AddCache(cache =>
        {
            cache.WithMemory()
                 .WithExpiration(TimeSpan.FromMinutes(30));
        })
        .AddMainActor("You are a helpful assistant.")
        .AddScene("Calculator", "Arithmetic operations", scene =>
        {
            scene
                .WithDescriptionFromTools()
                .WithService<ICalculatorService>(tools =>
                {
                    tools
                        .WithMethod<double>(x => x.Add(default, default), "Add", "Add two numbers")
                        .WithMethod<double>(x => x.Multiply(default, default), "Multiply", "Multiply two numbers");
                });
        });
});

var app = builder.Build();

app.MapPlayFramework("default", settings =>
{
    settings.BasePath = "/api/ai";
});

app.Run();

public interface ICalculatorService
{
    double Add(double left, double right);
    double Multiply(double left, double right);
}

public sealed class CalculatorService : ICalculatorService
{
    public double Add(double left, double right) => left + right;
    public double Multiply(double left, double right) => left * right;
}
```

With that setup, the main endpoints are:

```text
POST /api/ai/default
POST /api/ai/default/streaming
```

## Example: programmatic execution

The smallest runtime API is `ISceneManager`.

```csharp
public sealed class AssistantService
{
    private readonly ISceneManager _sceneManager;

    public AssistantService(IFactory<ISceneManager> factory)
        => _sceneManager = factory.Create("default");

    public async Task RunAsync()
    {
        var metadata = new Dictionary<string, object>
        {
            ["userId"] = "user-42",
            ["tenantId"] = "tenant-a"
        };

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Scene,
            SceneName = "Calculator",
            ConversationKey = "conversation-42"
        };

        await foreach (var step in _sceneManager.ExecuteAsync("What is 12 * 7?", metadata, settings))
        {
            Console.WriteLine($"[{step.Status}] {step.Message}");
        }
    }
}
```

If you want a lighter wrapper over the factory pattern, inject `IPlayFramework` and call:

- `Create(...)`
- `CreateOrDefault(...)`
- `Exists(...)`

Example:

```csharp
public sealed class MultiBotService
{
    private readonly IPlayFramework _playFramework;

    public MultiBotService(IPlayFramework playFramework)
        => _playFramework = playFramework;

    public async Task RunAsync()
    {
        var manager = _playFramework.Create("default");

        await foreach (var step in manager.ExecuteAsync("Hello"))
        {
            Console.WriteLine(step.Message);
        }
    }
}
```

## Example: multi-modal input helpers

`ISceneManager.ExecuteAsync(...)` also accepts `MultiModalInput`, and the package includes convenience constructors for common cases.

```csharp
var input = MultiModalInput.FromImageUrl(
    text: "Describe the important details in this image.",
    imageUrl: "https://example.com/photo.png",
    mimeType: "image/png");

await foreach (var step in sceneManager.ExecuteAsync(input))
{
    Console.WriteLine($"[{step.Status}] {step.Message}");
}
```

Available helpers include:

- `MultiModalInput.FromText(...)`
- `MultiModalInput.FromImageUrl(...)`
- `MultiModalInput.FromImageBytes(...)`
- `MultiModalInput.FromAudioUrl(...)`
- `MultiModalInput.FromAudioBytes(...)`
- `MultiModalInput.FromFileUrl(...)`
- `MultiModalInput.FromFileBytes(...)`

## Example: load balancing and fallback

PlayFramework can use multiple named chat clients for the same factory. The primary pool handles normal traffic, and the fallback chain is only used when the primary pool fails.

This matches the patterns covered by `LoadBalancingAndFallbackTests.cs`.

```csharp
using Rystem.PlayFramework;
using Rystem.PlayFramework.Adapters;

builder.Services.AddAdapterForAzureOpenAI("primary-1", settings =>
{
    settings.Endpoint = new Uri(builder.Configuration["AzureOpenAI:Endpoint"]!);
    settings.ApiKey = builder.Configuration["AzureOpenAI:Key"]!;
    settings.Deployment = "gpt-4o";
});

builder.Services.AddAdapterForAzureOpenAI("primary-2", settings =>
{
    settings.Endpoint = new Uri(builder.Configuration["AzureOpenAI:Endpoint"]!);
    settings.ApiKey = builder.Configuration["AzureOpenAI:Key"]!;
    settings.Deployment = "gpt-4o-mini";
});

builder.Services.AddAdapterForAzureOpenAI("fallback-1", settings =>
{
    settings.Endpoint = new Uri(builder.Configuration["AzureOpenAI:Endpoint"]!);
    settings.ApiKey = builder.Configuration["AzureOpenAI:Key"]!;
    settings.Deployment = "gpt-4o-mini";
});

builder.Services.AddPlayFramework("router", framework =>
{
    framework
        .WithChatClient("primary-1")
        .WithChatClient("primary-2")
        .WithLoadBalancingMode(LoadBalancingMode.RoundRobin)
        .WithChatClientAsFallback("fallback-1")
        .WithFallbackMode(FallbackMode.Sequential)
        .WithRetryPolicy(maxAttempts: 2, baseDelaySeconds: 0.5)
        .AddScene("General Requests", "General conversation", _ => { });
});
```

Important behavior:

- `WithChatClient(...)` adds clients to the primary pool
- `WithChatClientAsFallback(...)` adds clients to the fallback chain
- `WithLoadBalancingMode(...)` controls the primary pool order
- `WithFallbackMode(...)` controls fallback ordering
- `WithRetry(...)` and `WithRetryPolicy(...)` both configure transient retry behavior

## Example: scenes, actors, and service tools

Scenes are the main unit of orchestration.

`SceneBuilder` exposes the main extension points:

- `WithService<TService>(...)`
- `WithActors(...)`
- `WithMcpServer(...)`
- `OnClient(...)`
- `WithCacheExpiration(...)`
- `WithDescriptionFromTools()`

Example with service tools:

```csharp
builder.Services.AddSingleton<IWeatherService, WeatherService>();

builder.Services.AddPlayFramework("default", framework =>
{
    framework
        .WithChatClient("default")
        .AddMainActor("Answer clearly and explain tradeoffs when useful.")
        .AddScene("Weather", "Weather queries", scene =>
        {
            scene
                .WithDescriptionFromTools()
                .WithActors(actors =>
                {
                    actors.AddActor("Use the available weather tools before guessing.");
                    actors.AddActor("If the user asks for a forecast, mention the requested city explicitly.");
                })
                .WithService<IWeatherService>(tools =>
                {
                    tools
                        .WithMethod<string>(x => x.GetCurrent(default!), "GetCurrent", "Get current weather for a city")
                        .WithMethod<string>(x => x.GetForecast(default!, default), "GetForecast", "Get forecast for a city and number of days");
                });
        });
});

public interface IWeatherService
{
    string GetCurrent(string city);
    string GetForecast(string city, int days);
}
```

Important naming detail: scene names are normalized when they are registered. For example, `AddScene("General Requests", ...)` becomes `General_Requests` internally. If you later set `SceneRequestSettings.SceneName`, use the normalized name.

## Example: client tools and continuation

`OnClient(...)` is how a scene asks the browser or mobile client to execute something locally, then resume the conversation.

This is the same pattern exercised in `ClientInteractionTests.cs` and in the TypeScript client workspace.

```csharp
builder.Services.AddMemoryCache();

builder.Services.AddPlayFramework("default", framework =>
{
    framework
        .WithChatClient("default")
        .AddCache(cache =>
        {
            cache.WithMemory()
                 .WithExpiration(TimeSpan.FromMinutes(10));
        })
        .AddScene("Browser Assistant", "Needs browser-side tools", scene =>
        {
            scene
                .WithActors(actors =>
                {
                    actors.AddActor("When the user asks for the current location, call the client tool.");
                })
                .OnClient(client =>
                {
                    client
                        .AddTool("getCurrentLocation", "Get the user's current location", timeoutSeconds: 15)
                        .AddCommand("trackAnalytics", "Track a browser-side analytics event", feedbackMode: CommandFeedbackMode.OnError, timeoutSeconds: 5);
                })
                .WithCacheExpiration(TimeSpan.FromMinutes(5));
        });
});
```

Important behavior:

- `OnClient(...)` marks the scene as cache-dependent
- continuation state is stored between the server request and the client response
- the HTTP or TypeScript client is expected to send `conversationKey` plus `clientInteractionResults` when resuming
- the published TypeScript client in `src/AI/Rystem.PlayFramework.Client/src/rystem` already automates most of this flow

## Execution modes

The runtime supports four execution modes:

- `Direct`
- `Planning`
- `DynamicChaining`
- `Scene`

You can set a default at registration time:

```csharp
builder.Services.AddPlayFramework("default", framework =>
{
    framework
        .WithChatClient("default")
        .WithExecutionMode(SceneExecutionMode.Planning)
        .WithPlanning(settings =>
        {
            settings.MaxRecursionDepth = 5;
        })
        .AddScene("Calculator", "Arithmetic operations", _ => { })
        .AddScene("Weather", "Weather queries", _ => { });
});
```

Or override per request:

```csharp
var settings = new SceneRequestSettings
{
    ExecutionMode = SceneExecutionMode.Scene,
    SceneName = "Calculator"
};

await foreach (var step in sceneManager.ExecuteAsync("5 * 7", settings: settings))
{
}
```

Use `SceneExecutionMode.Scene` when you already know the target scene and want to bypass scene selection.

## Example: memory across requests

Memory is separate from conversation persistence. It is about loading and saving summarized context across calls.

This matches the patterns used in `MemoryTests.cs`.

```csharp
builder.Services.AddPlayFramework("default", framework =>
{
    framework
        .WithChatClient("default")
        .WithMemory(memory => memory
            .WithDefaultMemoryStorage("userId")
            .WithMaxSummaryLength(1000))
        .AddScene("Assistant", "General conversation", _ => { });
});
```

Then pass matching metadata when executing:

```csharp
var metadata = new Dictionary<string, object>
{
    ["userId"] = "user-42"
};

var settings = new SceneRequestSettings
{
    ExecutionMode = SceneExecutionMode.Direct,
    ConversationKey = "conversation-42"
};

await foreach (var _ in sceneManager.ExecuteAsync("My name is Alessandro", metadata, settings))
{
}

await foreach (var _ in sceneManager.ExecuteAsync("What is my name?", metadata, settings))
{
}
```

Important behavior:

- `WithDefaultMemoryStorage("userId")` isolates memory by metadata key
- `WithDefaultMemoryStorage("userId", "tenantId")` creates a composite key
- without `WithMemory(...)`, no memory is loaded or saved

## Example: rate limiting by metadata

Rate limiting is also metadata-driven. The test coverage shows the intended usage clearly.

```csharp
builder.Services.AddPlayFramework("default", framework =>
{
    framework
        .WithChatClient("default")
        .WithRateLimit(limit => limit
            .GroupBy("userId")
            .TokenBucket(capacity: 3, refillRate: 1)
            .RejectOnExceeded())
        .AddScene("Assistant", "General conversation", _ => { });
});
```

And the request metadata drives the grouping key:

```csharp
var metadata = new Dictionary<string, object>
{
    ["userId"] = "user-42"
};

await foreach (var step in sceneManager.ExecuteAsync("Hello", metadata))
{
    Console.WriteLine($"[{step.Status}] {step.Message}");
}
```

If you prefer waiting instead of immediate rejection:

```csharp
builder.Services.AddPlayFramework("default", framework =>
{
    framework
        .WithChatClient("default")
        .WithRateLimit(limit => limit
            .GroupBy("userId")
            .TokenBucket(capacity: 1, refillRate: 10)
            .WaitOnExceeded(TimeSpan.FromSeconds(5)))
        .AddScene("Assistant", "General conversation", _ => { });
});
```

## HTTP API

`MapPlayFramework(...)` maps SSE-oriented endpoints.

### Named mapping

```csharp
app.MapPlayFramework("default", settings =>
{
    settings.BasePath = "/api/ai";
    settings.EnableConversationEndpoints = true;
    settings.EnableVoiceEndpoints = true;
});
```

Routes:

- `POST /api/ai/default`
- `POST /api/ai/default/streaming`
- `GET /api/ai/default/conversations`
- `GET /api/ai/default/conversations/{conversationKey}`
- `DELETE /api/ai/default/conversations/{conversationKey}`
- `PATCH /api/ai/default/conversations/{conversationKey}/visibility`
- `POST /api/ai/default/voice`

### Example request body

The HTTP request model is `PlayFrameworkRequest`:

```json
{
  "message": "What is the weather in Milan?",
  "contents": [],
  "metadata": {
    "userId": "123"
  },
  "settings": {
    "executionMode": "Planning",
    "maxRecursionDepth": 5
  },
  "conversationKey": null,
  "clientInteractionResults": null
}
```

Example `curl` call against the step-by-step endpoint:

```bash
curl -N https://localhost:7248/api/ai/default \
  -H "Content-Type: application/json" \
  -d '{"message":"Calculate 5 * 7","settings":{"executionMode":"Scene","sceneName":"Calculator"}}' \
  --insecure
```

Token-level streaming uses the same body shape against:

```text
POST /api/ai/default/streaming
```

### Unnamed mapping

```csharp
app.MapPlayFramework(configure: settings =>
{
    settings.BasePath = "/api/ai";
});
```

In the current implementation this maps the base path root and internally falls back to the `default` factory name. It does not create a dynamic `/{factoryName}` route despite what some comments suggest.

## Example: conversation persistence and CRUD endpoints

PlayFramework has two separate storage concepts:

- cache for in-flight execution state and client-tool continuation
- repository persistence for stored conversations

Enable stored conversations in the PlayFramework builder:

```csharp
builder.Services.AddPlayFramework("default", framework =>
{
    framework
        .WithChatClient("default")
        .UseRepository()
        .AddScene("Assistant", "General conversation", _ => { });
});
```

Then register the matching repository separately with the same factory name:

```csharp
builder.Services.AddRepository<StoredConversation, string>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory(name: "default");
});
```

Finally, enable the HTTP endpoints:

```csharp
app.MapPlayFramework("default", settings =>
{
    settings.BasePath = "/api/ai";
    settings.EnableConversationEndpoints = true;
});
```

Without that matching repository registration, conversation CRUD cannot work.

## Example: voice pipeline

Enable voice in the builder:

```csharp
builder.Services.AddVoiceAdapterForAzureOpenAI("default", settings =>
{
    settings.Endpoint = new Uri(builder.Configuration["AzureOpenAI:Endpoint"]!);
    settings.ApiKey = builder.Configuration["AzureOpenAI:Key"]!;
    settings.SttDeployment = "whisper";
    settings.TtsDeployment = "tts-1";
    settings.TtsVoice = "alloy";
});

builder.Services.AddPlayFramework("default", framework =>
{
    framework
        .WithChatClient("default")
        .WithVoice("default")
        .AddScene("Assistant", "General conversation", _ => { });
});
```

And enable the HTTP endpoint explicitly:

```csharp
app.MapPlayFramework("default", settings =>
{
    settings.BasePath = "/api/ai";
    settings.EnableVoiceEndpoints = true;
});
```

That exposes:

```text
POST /api/ai/default/voice
```

## Important caveats

### Everything is factory-based

Named PlayFramework instances, chat clients, repositories, and voice adapters all rely on matching factory names. A lot of runtime errors come from mismatched names rather than missing services.

### `OnClient(...)` requires cache support

Client-side tools depend on cached continuation state. In samples and tests this is usually `IMemoryCache` or `IDistributedCache` behind `AddCache(...)`.

### Only token-bucket rate limiting is implemented

`RateLimitBuilder` exposes `SlidingWindow`, `FixedWindow`, and `Concurrent`, but DI registration currently supports only `TokenBucket`.

### Repository persistence is separate from memory

`UseRepository()` stores conversations for CRUD endpoints. `WithMemory(...)` stores summarized conversational memory. They solve different problems and you often need both or neither.

### The HTTP voice path resolves by PlayFramework factory name first

The builder stores a `VoiceAdapterFactoryName`, but the HTTP endpoint effectively resolves `IVoiceAdapter` by PlayFramework factory name first, then unnamed default. Keep those names aligned.

### Some API settings are less active than they look

For example `DefaultFactoryName`, `EnableCompression`, and `MaxRequestBodySize` exist on `PlayFrameworkApiSettings`, but the current HTTP mapping path does not meaningfully use them.

### This package targets `net10.0`

The current project targets `net10.0` only.

## Grounded by source and tests

- `src/AI/Test/Rystem.PlayFramework.Api/Program.cs`
- `src/AI/Test/Rystem.PlayFramework.Test/Tests/FactoryPatternTests.cs`
- `src/AI/Test/Rystem.PlayFramework.Test/Tests/ClientInteractionTests.cs`
- `src/AI/Test/Rystem.PlayFramework.Test/Tests/LoadBalancingAndFallbackTests.cs`
- `src/AI/Test/Rystem.PlayFramework.Test/Tests/MemoryTests.cs`
- `src/AI/Test/Rystem.PlayFramework.Test/Tests/RateLimitingTests.cs`
- `src/AI/Test/Rystem.PlayFramework.Test/Tests/MultiModalTests.cs`

Use this package when you want the orchestration engine itself. Add an adapter package when you want a concrete model provider, and add repository infrastructure when you want stored conversations.
