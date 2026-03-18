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

## Example: guardrails (operational boundaries)

Guardrails prevent the LLM from hallucinating tools or responding outside the system's declared capabilities. When enabled, a system prompt is injected at the start of every new conversation.

```csharp
builder.Services.AddPlayFramework("default", framework =>
{
    framework
        .WithChatClient("default")
        // Default prompt: operate only within registered scenes/actors/tools
        .UseDefaultGuardrails()
        .AddScene("Calculator", "Arithmetic operations", _ => { });
});
```

For domain-specific systems, replace the default prompt with a custom one:

```csharp
builder.Services.AddPlayFramework("default", framework =>
{
    framework
        .WithChatClient("default")
        .UseCustomGuardrails(
            """
            You are a customer support assistant for Acme Corp.
            You can ONLY help with: order status, returns, and product questions.
            Do NOT discuss pricing or process refunds over $500. Escalate those to the manager team.
            """)
        .AddScene("Orders", "Order management", _ => { });
});
```

Important behavior:

- Guardrails are added only for new conversations (not when resuming from cache)
- The default prompt consumes approximately 100–150 tokens per request
- Guardrails do not replace `IAuthorizationLayer`; they address prompt scope, not user permissions

## Example: authorization layer

`IAuthorizationLayer` runs after initialization but before scene execution. It is the right place for user-specific quota checks, feature flags, and budget enforcement.

```csharp
public sealed class CustomAuthorizationLayer : IAuthorizationLayer
{
    private readonly IUserService _userService;

    public CustomAuthorizationLayer(IUserService userService)
        => _userService = userService;

    public async Task<AuthorizationResult> AuthorizeAsync(
        SceneContext context,
        SceneRequestSettings settings,
        CancellationToken cancellationToken)
    {
        if (!context.Metadata.TryGetValue("userId", out var userIdObj))
            return new AuthorizationResult { IsAuthorized = false, Reason = "userId not found in metadata" };

        var user = await _userService.GetUserAsync(userIdObj.ToString()!, cancellationToken);

        if (user.MonthlyQuota <= 0)
            return new AuthorizationResult { IsAuthorized = false, Reason = "Monthly quota exceeded" };

        return new AuthorizationResult { IsAuthorized = true };
    }
}
```

Register it in the PlayFramework builder:

```csharp
builder.Services.AddPlayFramework("default", framework =>
{
    framework
        .WithChatClient("default")
        .AddAuthorizationLayer<CustomAuthorizationLayer>()
        .AddScene("Assistant", "General conversation", _ => { });
});

builder.Services.AddScoped<IUserService, UserService>();
```

HTTP-level authorization (ASP.NET Core policies) and `IAuthorizationLayer` are complementary:

- HTTP policies run before any PlayFramework processing (token/claims validation)
- `IAuthorizationLayer` runs after initialization (business logic, quotas, feature flags)

## Example: request context injection (IContext)

`IContext` lets you inject dynamic, per-request context data into the system message at the start of every new conversation. The typical use case is enriching the LLM with information only available at runtime: the current user's profile, tenant settings, permissions, locale, or anything else that comes from the HTTP layer (JWT claims, headers, session).

Implement the interface and return any object (or a plain string). If the return value is not a string it is serialized as JSON:

```csharp
public sealed class UserContextProvider : IContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserService _userService;

    public UserContextProvider(IHttpContextAccessor httpContextAccessor, IUserService userService)
    {
        _httpContextAccessor = httpContextAccessor;
        _userService = userService;
    }

    public async Task<dynamic?> RetrieveAsync(
        SceneContext context,
        SceneRequestSettings settings,
        CancellationToken cancellationToken)
    {
        var userId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
        if (userId is null) return null;

        var user = await _userService.GetUserAsync(userId, cancellationToken);

        return new
        {
            user.DisplayName,
            user.Email,
            user.Role,
            user.PreferredLanguage
        };
    }
}
```

Register it with `AddContext<T>()` in the PlayFramework builder:

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddPlayFramework("default", framework =>
{
    framework
        .WithChatClient("default")
        .AddContext<UserContextProvider>()
        .AddMainActor("You are a helpful assistant. Address the user by name when appropriate.")
        .AddScene("Assistant", "General conversation", _ => { });
});
```

At the start of each new conversation the framework calls `RetrieveAsync`, serializes the result, and prepends the following block to the system message (before main actor instructions):

```
[Request Context]
{"displayName":"Alessandro","email":"a@example.com","role":"Admin","preferredLanguage":"it"}

[System Instructions]
- You are a helpful assistant. Address the user by name when appropriate.
```

Important behavior:

- `RetrieveAsync` is called only once per **new** conversation. When resuming a cached or stored conversation the existing context is reused.
- Returning `null` skips the `[Request Context]` block entirely.
- Returning a `string` injects it verbatim; any other object is serialized to JSON.
- `IContext` is transient by default. Services you inject into it may be scoped (e.g. `IHttpContextAccessor`).
- Only one `IContext` implementation can be registered per named PlayFramework instance. A second call to `AddContext<T>()` replaces the previous registration.

## Example: director, summarization, and planning configuration

The director evaluates the execution result and may re-run the scene. Summarization compresses conversation history when it grows too large. Both are configured on the builder.

```csharp
builder.Services.AddPlayFramework("default", framework =>
{
    framework
        .WithChatClient("default")

        // Planning: multi-step orchestration across scenes
        .WithPlanning(planning =>
        {
            planning.MaxRecursionDepth = 5;
        })

        // Director: post-execution evaluation and optional re-execution
        .WithDirector(director =>
        {
            director.Enabled = true;
            director.MaxReExecutions = 3;
        })

        // Summarization: compresses history above thresholds
        .WithSummarization(summarization =>
        {
            summarization.Enabled = true;
            summarization.CharacterThreshold = 15_000;
            summarization.ResponseCountThreshold = 20;
        })

        .AddScene("Assistant", "General conversation", _ => { });
});
```

You can also override the director or summarizer with a custom implementation (see Custom extensibility below).

## Example: main actors (dynamic and async variants)

Beyond a static string, `AddMainActor` accepts a delegate or a typed service.

```csharp
builder.Services.AddPlayFramework("default", framework =>
{
    framework
        .WithChatClient("default")

        // Static
        .AddMainActor("You are a professional assistant for Acme Corp.")

        // Sync delegate from request metadata
        .AddMainActor(context =>
            $"Current user: {context.Metadata.GetValueOrDefault("userName")}")

        // Async delegate — cached after the first call for the lifetime of the request
        .AddMainActor(async (context, ct) =>
        {
            var svc = context.ServiceProvider.GetRequiredService<IUserService>();
            var user = await svc.GetUserAsync(context.Metadata["userId"].ToString()!, ct);
            return $"User preferences: {user.Preferences}";
        }, cacheForSubsequentCalls: true)

        // Typed service that implements IActor
        .AddMainActor<CustomActorService>()

        .AddScene("Assistant", "General conversation", _ => { });
});
```

## Example: RAG and web search

RAG and web search are both opt-in at framework level and can be overridden or disabled per scene.

```csharp
// Register your RAG implementation
builder.Services.AddSingleton<IRagService, AzureSearchRagService>();

// Register your web search implementation
builder.Services.AddSingleton<IWebSearchService, BingWebSearchService>();

builder.Services.AddPlayFramework("default", framework =>
{
    framework
        .WithChatClient("default")

        // Global RAG
        .WithRag(rag =>
        {
            rag.TopK = 10;
            rag.SearchAlgorithm = VectorSearchAlgorithm.CosineSimilarity;
            rag.MinimumScore = 0.7;
        })

        // Global web search
        .WithWebSearch(ws =>
        {
            ws.MaxResults = 10;
            ws.SafeSearch = true;
        })

        // Scene that overrides RAG settings
        .AddScene("Search", "Document search", scene =>
        {
            scene.WithRag(rag => { rag.TopK = 5; });
        })

        // Scene that disables both (e.g., pure arithmetic)
        .AddScene("Calculator", "Arithmetic", scene =>
        {
            scene
                .WithoutRag()
                .WithoutWebSearch()
                .WithService<ICalculatorService>(tools =>
                {
                    tools.WithMethod<double>(x => x.Add(default, default), "Add", "Add two numbers");
                });
        });
});
```

## Example: MCP server integration

A scene can connect to an external Model Context Protocol server to expose its tools alongside any registered services.

```csharp
builder.Services.AddPlayFramework("default", framework =>
{
    framework
        .WithChatClient("default")
        .AddScene("Dev Tools", "Development utilities", scene =>
        {
            scene
                .WithMcpServer("mcp-server-name")
                .WithService<IDevService>(tools =>
                {
                    tools.WithMethod<string>(x => x.GetStatus(), "GetStatus", "Get system status");
                });
        });
});
```

The MCP server name must match a registered `IMcpServerManager` factory entry.

## Example: cost tracking

Cost per call and cumulative totals are recorded in `AiSceneResponse.Cost` / `AiSceneResponse.TotalCost` when the adapter is configured with pricing. See the [Rystem.PlayFramework.Adapters README](https://github.com/KeyserDSoze/Rystem/tree/master/src/AI/Rystem.PlayFramework.Adapters#cost-tracking) for how to set `AdapterSettings.CostTracking`.

Per-request budget enforcement uses `SceneRequestSettings.MaxBudget`. When the cumulative cost exceeds it, execution stops with status `BudgetExceeded`:

```csharp
var settings = new SceneRequestSettings
{
    MaxBudget = 0.05m, // $0.05 maximum per request
};
```

Response cost fields:

| Field | Description |
| --- | --- |
| `AiSceneResponse.Cost` | cost of the current LLM call (null when no LLM call was made) |
| `AiSceneResponse.TotalCost` | cumulative cost across all calls in this request |
| `AiSceneResponse.InputTokens` | input tokens used in this call |
| `AiSceneResponse.OutputTokens` | output tokens generated in this call |
| `AiSceneResponse.CachedInputTokens` | cached input tokens used in this call |

## Example: custom extensibility

Core pipeline components can be swapped out without forking the framework.

```csharp
builder.Services.AddPlayFramework("default", framework =>
{
    framework
        .WithChatClient("default")

        // Replace the built-in planner
        .AddCustomPlanner<MyPlanner>()

        // Replace the built-in summarizer
        .AddCustomSummarizer<MySummarizer>()

        // Replace the built-in director
        .AddCustomDirector<MyDirector>()

        // Replace the built-in JSON service (used for tool argument serialization)
        .AddCustomJsonService<MyJsonService>()
        // Or with a factory delegate
        .AddCustomJsonService(sp => new MyJsonService(sp.GetRequiredService<IOptions<JsonOptions>>()))

        // Inject additional context into SceneContext before execution
        .AddContext<MyContextProvider>()

        .AddScene("Assistant", "General conversation", _ => { });
});
```

`IJsonService` is the most commonly replaced component. If the default `System.Text.Json`-based serialization does not handle a custom type (such as `AnyOf<T0, T1>` with non-standard converters), provide a custom implementation here.

## Per-request settings reference

`SceneRequestSettings` controls all per-call behavior and can be passed to both the programmatic API and the HTTP request body.

| Property | Type | Description |
|---|---|---|
| `ExecutionMode` | `SceneExecutionMode` | `Direct`, `Planning`, `DynamicChaining`, `Scene` |
| `SceneName` | `string?` | Target scene name (required for `Scene` mode). Use the normalized name (e.g. `General_Requests`) |
| `ConversationKey` | `string?` | Key for multi-turn conversation state |
| `MaxRecursionDepth` | `int` | Max planning depth |
| `MaxDynamicScenes` | `int` | Max scenes in dynamic chaining |
| `EnableSummarization` | `bool` | Override summarization on/off for this request |
| `EnableDirector` | `bool` | Override director on/off for this request |
| `CacheBehavior` | `CacheBehavior` | `Default`, `ForceRefresh`, `ReadOnly` |
| `MaxBudget` | `decimal?` | Max allowed cost for this request |
| `ModelId` | `string?` | Override model deployment |
| `Temperature` | `float?` | Override temperature |
| `MaxTokens` | `int?` | Override max output tokens |
| `IsVoiceMode` | `bool` | Inject voice-style system instruction |
| `UserId` | `string?` | Override user identity for conversation ownership |

## Voice pipeline settings reference

`VoiceSettings` controls sentence accumulation and language behavior.

| Property | Type | Default | Description |
|---|---|---|---|
| `SentenceDelimiters` | `string` | `".!?\n"` | Characters that flush accumulated text to TTS |
| `MinCharsBeforeTts` | `int` | `20` | Min chars before flushing to TTS |
| `MaxCharsBeforeTts` | `int` | `500` | Max chars before forcing a flush |
| `LanguageInstruction` | `string?` | built-in | System instruction template; `{language}` is replaced with the STT-detected language |
| `VoiceStyleInstruction` | `string?` | built-in | Instructs the LLM to respond conversationally (no markdown, no tables). Set to `null` to disable |

## Response status codes reference

All possible values of `AiResponseStatus`:

| Status | Description |
|---|---|
| `Completed` | Execution finished successfully |
| `Streaming` | Token-level streaming chunk in progress |
| `ExecutingScene` | Engine is inside a scene |
| `FunctionRequest` | Server-side tool call started |
| `FunctionCompleted` | Server-side tool call finished |
| `AwaitingClient` | Waiting for a client-side tool response |
| `CommandClient` | Fire-and-forget command sent to client |
| `Planning` | Creating an execution plan |
| `ExecutingPlan` | Executing a plan step |
| `Summarizing` | Compressing conversation history |
| `Directing` | Director evaluating scene output |
| `BudgetExceeded` | Request exceeded `MaxBudget` |
| `Error` | Unhandled error during execution |
| `Unauthorized` | `IAuthorizationLayer` rejected the request |
| `RateLimited` | Rate limit exceeded with `RejectOnExceeded` |
| `Cached` | Response served from conversation cache |

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
