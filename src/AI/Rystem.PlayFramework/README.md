# 🎮 Rystem PlayFramework

[![NuGet](https://img.shields.io/nuget/v/Rystem.PlayFramework.svg)](https://www.nuget.org/packages/Rystem.PlayFramework/)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)

> **Orchestrated AI execution framework with multi-modal support, client-side tools, and advanced planning**

Production-ready framework for building **AI-powered applications** with:
- 🎭 **Scene-Based Architecture** - Organize AI workflows into reusable scenes
- 🔧 **Server & Client Tools** - Execute code on server or browser/mobile
- 🧠 **Execution Modes** - Direct, Planning, DynamicChaining, Scene
- 📸 **Multi-Modal Content** - Images, audio, video, PDFs, URIs
- 🔄 **Streaming** - Step-by-step or token-level SSE
- ⚖️ **Load Balancing & Fallback** - Multi-provider reliability
- 💰 **Cost Tracking** - Per-request budget limits
- 🔐 **Authorization** - Policy-based access control
- 🛡️ **Guardrails** - Prevent hallucinations and out-of-scope responses
- 📊 **Observability** - Logging, telemetry, metrics

---

## 📦 Installation

```bash
dotnet add package Rystem.PlayFramework
dotnet add package Rystem.PlayFramework.Providers.OpenAI  # Or other providers
```

**Supported Providers:**
- OpenAI (GPT-4, GPT-4o, o1, o3)
- Azure OpenAI
- Anthropic (Claude)
- Google (Gemini)
- Ollama (Local models)

---

## 🚀 Quick Start

### 1. Define a Scene

```csharp
public interface IWeatherService
{
    Task<string> GetWeatherAsync(string city);
}

public class WeatherService : IWeatherService
{
    public async Task<string> GetWeatherAsync(string city)
    {
        // Call weather API
        return $"The weather in {city} is sunny, 24°C";
    }
}
```

### 2. Register PlayFramework

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPlayFramework("default", pb => pb
    // Add LLM provider
    .AddChatClient(client => client
        .AddOpenAIChatClient("gpt-4o", Environment.GetEnvironmentVariable("OPENAI_API_KEY")!))

    // Add operational boundaries (prevents hallucinations)
    .UseDefaultGuardrails()

    // Add scene with service tool
    .AddScene("weather", "Get weather information", scene => scene
        .WithService<IWeatherService>(s => s
            .AddTool(x => x.GetWeatherAsync)))

    // Add cache for conversation state
    .WithCache(cache => cache
        .WithInMemory()
        .WithExpiration(TimeSpan.FromMinutes(30)))

    // Add custom authorization layer (optional)
    .AddAuthorizationLayer<CustomAuthorizationLayer>());

// Register services
builder.Services.AddSingleton<IWeatherService, WeatherService>();

var app = builder.Build();

// Map HTTP endpoints
app.MapPlayFramework(settings =>
{
    settings.BasePath = "/api/ai";
    settings.RequireAuthentication = false;
});

app.Run();
```

### 3. Test the API

```bash
curl -X POST http://localhost:5158/api/ai/default \
  -H "Content-Type: application/json" \
  -d '{
    "message": "What is the weather in Milan?"
  }'
```

**Response:**
```json
{
  "finalMessage": "The weather in Milan is sunny, 24°C",
  "status": "completed",
  "totalCost": 0.0023,
  "elapsedMilliseconds": 1240
}
```

---

## 🎭 Scene Definition

### Scene with Multiple Tools

```csharp
public interface ICalculatorService
{
    double Add(double augend, double addend);
    double Subtract(double minuend, double subtrahend);
    double Multiply(double multiplicand, double multiplier);
    double Divide(double dividend, double divisor);
}

builder.Services.AddPlayFramework("default", pb => pb
    .AddChatClient(...)
    .AddScene("calculator", "Perform arithmetic operations", scene => scene
        .WithService<ICalculatorService>(s => s
            .AddTool(x => x.Add)
            .AddTool(x => x.Subtract)
            .AddTool(x => x.Multiply)
            .AddTool(x => x.Divide))));
```

### Scene with System Prompt

```csharp
.AddScene("assistant", "General purpose assistant", scene => scene
    .WithSystemPrompt("You are a helpful AI assistant. Be concise and accurate.")
    .WithService<IAssistantService>(s => s.AddTool(x => x.Search)))
```

### Scene with Client-Side Tools

Execute tools on **browser/mobile** (camera, geolocation, file picker):

```csharp
.AddScene("vision", "Analyze user photos", scene => scene
    .OnClient(client => client
        .AddTool("capturePhoto", "Take photo from camera")
        .AddTool("getCurrentLocation", "Get GPS coordinates")
        .AddTool("selectFiles", "Open file picker"))
    .WithService<IVisionService>(s => s
        .AddTool(x => x.AnalyzeImage)))
```

**Client-side implementation** (TypeScript):
```typescript
registry.register("capturePhoto", async () => {
    const content = await AIContentConverter.fromCamera();
    return [content];
});
```

See [Client Interaction Guide](Docs/CLIENT_INTERACTION_ARCHITECTURE_V2.md) for details.

---

## 🎛️ Execution Modes

Control **how scenes are selected and executed**:

```csharp
var request = new PlayFrameworkRequest
{
    Message = "Book a flight to Paris and reserve a hotel",
    Settings = new SceneRequestSettings
    {
        ExecutionMode = SceneExecutionMode.Planning, // Direct | Planning | DynamicChaining | Scene
        MaxRecursionDepth = 5,
        EnableSummarization = true
    }
};
```

### Mode Comparison

| Mode | Description | Use Case |
|------|-------------|----------|
| **Direct** | Single scene, no planning | Simple queries, fast responses |
| **Planning** | Upfront multi-step plan | Known workflows (booking, checkout) |
| **DynamicChaining** | LLM decides next step live | Exploratory tasks (research, debugging) |
| **Scene** | Execute specific scene by name | Resuming after client interaction |

### Enable Planning

```csharp
builder.Services.AddPlayFramework("default", pb => pb
    .AddChatClient(...)
    .WithPlanning(planning => planning
        .MaxRecursionDepth = 5
        .Enabled = true)
    .AddScene(...));
```

### Enable Dynamic Chaining

```csharp
.AddScene("research", "Research a topic", scene => scene
    .WithService<ISearchService>(s => s.AddTool(x => x.Search))
    .WithDynamicChaining(maxScenes: 10))
```

---

## 🔧 Multi-Modal Content

### Send Images

```csharp
var request = new PlayFrameworkRequest
{
    Message = "Describe this image",
    Contents = new List<ContentItem>
    {
        new()
        {
            Type = ContentType.Image,
            Base64Data = Convert.ToBase64String(imageBytes),
            MediaType = "image/jpeg",
            Name = "photo.jpg"
        }
    }
};
```

### Send Audio

```csharp
new ContentItem
{
    Type = ContentType.Audio,
    Base64Data = Convert.ToBase64String(audioBytes),
    MediaType = "audio/mp3",
    Name = "recording.mp3"
}
```

### Send PDFs via URI

```csharp
new ContentItem
{
    Type = ContentType.Uri,
    Uri = "https://example.com/document.pdf",
    MediaType = "application/pdf"
}
```

---

## ⚙️ Configuration & Settings

### Global Settings

```csharp
builder.Services.AddPlayFramework("default", pb => pb
    .AddChatClient(...)

    // Planning
    .WithPlanning(planning =>
    {
        planning.Enabled = true;
        planning.MaxRecursionDepth = 5;
    })

    // Summarization
    .WithSummarization(summarization =>
    {
        summarization.Enabled = true;
        summarization.MaxTokensBeforeSummarization = 8000;
    })

    // Director (multi-scene orchestration)
    .WithDirector(director =>
    {
        director.Enabled = true;
        director.MaxScenes = 5;
    })

    // Cache (for conversation state)
    .WithCache(cache => cache
        .WithRedis("localhost:6379")
        .WithExpiration(TimeSpan.FromMinutes(30)))

    // Rate Limiting
    .WithRateLimiting(rateLimiting =>
    {
        rateLimiting.Strategy = RateLimitingStrategy.TokenBucket;
        rateLimiting.MaxTokensPerMinute = 10000;
        rateLimiting.MaxRequestsPerMinute = 60;
    })

    // Cost Settings
    .WithCostSettings(cost =>
    {
        cost.InputTokenCost = 0.00001m;   // $0.01 per 1K tokens
        cost.OutputTokenCost = 0.00003m;  // $0.03 per 1K tokens
        cost.Currency = "USD";
    })

    // Guardrails (operational boundaries)
    .UseDefaultGuardrails()  // Prevents hallucinations and out-of-scope responses
    // OR
    .UseCustomGuardrails("You must only answer questions about..."));
```

### Per-Request Settings

```csharp
var request = new PlayFrameworkRequest
{
    Message = "Your query",
    Settings = new SceneRequestSettings
    {
        // Execution mode
        ExecutionMode = SceneExecutionMode.Planning,

        // Planning
        MaxRecursionDepth = 5,
        MaxDynamicScenes = 10,

        // Features
        EnableSummarization = true,
        EnableDirector = false,
        EnableStreaming = true,

        // Model overrides
        ModelId = "gpt-4o",
        Temperature = 0.7f,
        MaxTokens = 4096,

        // Caching
        CacheBehavior = CacheBehavior.Default,
        ConversationKey = "user-123-session-1",

        // Budget
        MaxBudget = 0.50m, // $0.50 max cost

        // Scene selection
        SceneName = "SpecificScene" // For SceneExecutionMode.Scene
    },
    Metadata = new Dictionary<string, object>
    {
        { "userId", "user-123" },
        { "sessionId", "session-abc" }
    }
};
```

---

## 🔄 Conversation State & Caching

Use `conversationKey` for **multi-turn conversations**:

```csharp
var conversationKey = Guid.NewGuid().ToString();

// First request
var response1 = await sceneManager.ExecuteAsync(new PlayFrameworkRequest
{
    Message = "What's the weather in Paris?",
    Settings = new SceneRequestSettings
    {
        ConversationKey = conversationKey
    }
});

// Follow-up request (uses cached context)
var response2 = await sceneManager.ExecuteAsync(new PlayFrameworkRequest
{
    Message = "And in London?",
    Settings = new SceneRequestSettings
    {
        ConversationKey = conversationKey
    }
});
// LLM remembers Paris context!
```

### Cache Configuration

**In-Memory** (single-server):
```csharp
.WithCache(cache => cache
    .WithInMemory()
    .WithExpiration(TimeSpan.FromMinutes(30)))
```

**Redis** (distributed):
```csharp
.WithCache(cache => cache
    .WithRedis("localhost:6379,password=secret")
    .WithExpiration(TimeSpan.FromMinutes(60)))
```

---

## 💾 Conversation Persistence (Repository Pattern)

PlayFramework supports **persistent storage** of conversations using **Rystem Repository Pattern**. This enables:
- ✅ **Multi-user** conversation history
- ✅ **Public/Private** conversations
- ✅ **Search & filtering** across conversations
- ✅ **Authorization** checks (owner-only access)
- ✅ **REST API** for conversation management

### Enable Repository Persistence

```csharp
builder.Services.AddPlayFramework("default", pb => pb
    .AddChatClient(...)
    .AddScene(...)

    // Enable caching (required for Repository to work)
    .WithCache(cache => cache
        .WithMemory()
        .WithExpiration(TimeSpan.FromMinutes(30)))

    // Enable repository persistence
    .UseRepository(repo => repo
        .WithEntityFramework<AppDbContext>())); // Or WithInMemory(), WithCosmos(), etc.
```

### StoredConversation Model

```csharp
public class StoredConversation : IEntity<string>
{
    public string ConversationKey { get; set; }  // Unique ID (primary key)
    public string? UserId { get; set; }          // Owner (from IAuthorizationLayer)
    public bool IsPublic { get; set; }           // Public vs Private
    public DateTimeOffset Timestamp { get; set; } // Created/Updated
    public List<StoredMessage> Messages { get; set; } // Conversation history
    public ExecutionState? ExecutionState { get; set; } // Planning/Director state
}
```

### Conversation Authorization

Conversations are **automatically saved** with:
- **UserId** from `IAuthorizationLayer.AuthorizeAsync()` or `settings.UserId`
- **IsPublic** flag (default: `false`)

**Private conversations** require userId match when loading from repository:
```csharp
// In SceneManager
if (!storedConversation.IsPublic && storedConversation.UserId != currentUserId)
{
    return AiSceneResponse.Unauthorized("Access denied to private conversation");
}
```

### REST API Endpoints

Enable conversation management endpoints:

```csharp
app.MapPlayFramework("default", settings =>
{
    settings.BasePath = "/api/ai";
    settings.EnableConversationEndpoints = true;  // Enable CRUD endpoints
    settings.MaxConversationsPageSize = 100;      // Max results per query
});
```

**Available endpoints:**

#### List Conversations
**GET** `/api/ai/default/conversations`

Query parameters:
- `searchText` - Filter by message content
- `includePublic` - Include public conversations (default: `true`)
- `includePrivate` - Include private conversations (default: `true`)
- `orderBy` - Sort order: `TimestampDescending` | `TimestampAscending` (default: `TimestampDescending`)
- `skip` - Pagination offset (default: `0`)
- `take` - Page size (default: `50`, max: `MaxConversationsPageSize`)

**Example:**
```bash
curl "http://localhost:5158/api/ai/default/conversations?searchText=weather&orderBy=TimestampDescending&take=20"
```

**Response:**
```json
[
  {
    "conversationKey": "abc-123",
    "userId": "user@example.com",
    "isPublic": false,
    "timestamp": "2025-01-15T10:30:00Z",
    "messages": [...],
    "executionState": {...}
  }
]
```

#### Get Conversation
**GET** `/api/ai/default/conversations/{conversationKey}`

Returns single conversation. **Authorization check**: private conversations require userId match.

**Response:**
- `200 OK` - Conversation found and authorized
- `403 Forbidden` - Private conversation, unauthorized
- `404 Not Found` - Conversation not found

#### Delete Conversation
**DELETE** `/api/ai/default/conversations/{conversationKey}`

**Owner-only** operation. Requires userId match.

**Response:**
- `204 No Content` - Successfully deleted
- `403 Forbidden` - Not the owner
- `404 Not Found` - Conversation not found

#### Update Visibility
**PATCH** `/api/ai/default/conversations/{conversationKey}/visibility`

**Request:**
```json
{
  "isPublic": true
}
```

Toggles conversation between public/private. **Owner-only** operation.

**Response:**
- `200 OK` - Returns updated conversation
- `403 Forbidden` - Not the owner
- `404 Not Found` - Conversation not found

### Custom Query Example

Use Rystem Repository to build custom queries:

```csharp
var repository = repositoryFactory.Create("default");

// Find conversations for specific user
var userConversations = await repository
    .Where(x => x.UserId == "user@example.com")
    .OrderByDescending(x => x.Timestamp)
    .Take(50)
    .ToListAsEntityAsync();

// Search by message content
var searchResults = await repository
    .Where(x => x.Messages.Any(m => m.Text.Contains("weather")))
    .ToListAsEntityAsync();

// Public conversations only
var publicConversations = await repository
    .Where(x => x.IsPublic)
    .OrderByDescending(x => x.Timestamp)
    .ToListAsEntityAsync();
```

### Storage Backends

PlayFramework repository supports all Rystem storage providers:

**Entity Framework Core:**
```csharp
.UseRepository(repo => repo
    .WithEntityFramework<AppDbContext>())
```

**Cosmos DB:**
```csharp
.UseRepository(repo => repo
    .WithCosmosDb("connection-string", "database", "container"))
```

**Azure Table Storage:**
```csharp
.UseRepository(repo => repo
    .WithAzureTable("connection-string", "tableName"))
```

**In-Memory (testing):**
```csharp
.UseRepository(repo => repo
    .WithInMemory())
```

### Multi-Tenant Scenarios

Use **Factory Pattern** for tenant-specific repositories:

```csharp
// Register multiple factories
builder.Services.AddPlayFramework("tenant-a", pb => pb
    .AddChatClient(...)
    .UseRepository(repo => repo
        .WithEntityFramework<TenantADbContext>()));

builder.Services.AddPlayFramework("tenant-b", pb => pb
    .AddChatClient(...)
    .UseRepository(repo => repo
        .WithEntityFramework<TenantBDbContext>()));

// Map separate endpoints
app.MapPlayFramework("tenant-a", settings =>
{
    settings.BasePath = "/api/ai";
    settings.EnableConversationEndpoints = true;
});

app.MapPlayFramework("tenant-b", settings =>
{
    settings.BasePath = "/api/ai";
    settings.EnableConversationEndpoints = true;
});
```

**Endpoints:**
- `/api/ai/tenant-a` - Chat for Tenant A
- `/api/ai/tenant-a/conversations` - Conversations for Tenant A
- `/api/ai/tenant-b` - Chat for Tenant B
- `/api/ai/tenant-b/conversations` - Conversations for Tenant B

---

## 📸 Multi-Modal Content (Images, Audio, Video, PDFs)

PlayFramework supports **base64-encoded media content** in messages. To optimize performance and reduce payload size, the `includeContents` parameter controls whether to include or exclude media when fetching conversations.

### Overview

**StoredMessage** model supports multi-modal content:
```csharp
public class StoredMessage
{
    public string Role { get; set; }       // user | assistant | system | tool
    public string? Text { get; set; }      // Text content
    public List<AIContent>? Contents { get; set; } // Multi-modal attachments
}

public class AIContent
{
    public string Type { get; set; }       // "text" | "data"
    public string? Text { get; set; }      // For type="text"
    public string? Data { get; set; }      // Base64 encoded (for type="data")
    public string? MediaType { get; set; } // MIME type: image/jpeg, audio/mp3, application/pdf
}
```

### includeContents Parameter

By default, `Contents` are **excluded** from list operations to reduce bandwidth and improve performance. Use `includeContents=true` when you need to display media.

#### REST API Examples

**List conversations WITHOUT media (faster, smaller payload):**
```bash
GET /api/ai/default/conversations?includeContents=false&take=50
```

**Get single conversation WITH media (for display):**
```bash
GET /api/ai/default/conversations/abc-123?includeContents=true
```

**Response without contents:**
```json
{
  "conversationKey": "abc-123",
  "userId": "user@example.com",
  "isPublic": false,
  "timestamp": "2025-01-15T10:30:00Z",
  "messages": [
    {
      "role": "user",
      "text": "Analyze this image",
      "contents": null  // ← Excluded to reduce payload
    }
  ]
}
```

**Response with contents:**
```json
{
  "conversationKey": "abc-123",
  "messages": [
    {
      "role": "user",
      "text": "Analyze this image",
      "contents": [
        {
          "type": "data",
          "data": "iVBORw0KGgoAAAANSUhEUgAA...",  // ← Base64 image
          "mediaType": "image/jpeg"
        }
      ]
    },
    {
      "role": "assistant",
      "text": "The image shows a sunset over mountains.",
      "contents": null
    }
  ]
}
```

### Storage Optimization with Repository Framework

PlayFramework uses **Repository Framework metadata** to control content inclusion:

```csharp
var result = await queryBuilder
    .Skip(parameters.Skip)
    .Take(pageSize)
    .AddMetadata(nameof(parameters.IncludeContents), parameters.IncludeContents.ToString())
    .ToListAsEntityAsync();
```

**Backend automatically excludes Contents** when `includeContents=false`:
```csharp
if (!includeContents)
{
    foreach (var message in conversation.Messages)
    {
        message.Contents = null;  // Reduce JSON payload size
    }
}
```

### When to Use includeContents

| Operation | includeContents | Reason |
|-----------|----------------|--------|
| **List conversations** | `false` (default) | Faster, smaller payload - only need titles/previews |
| **Load single conversation for display** | `true` | Need to render images/PDFs/audio in UI |
| **Search conversations** | `false` | Only searching text content |
| **Export conversation** | `true` | Full conversation with attachments |

### Performance Impact

**Without contents** (includeContents=false):
- ✅ 100 conversations ≈ **50 KB** payload
- ✅ Query time: **~50ms**

**With contents** (includeContents=true):
- ⚠️ 100 conversations ≈ **5-50 MB** payload (depending on images/PDFs)
- ⚠️ Query time: **~500ms**

**Best Practice**: Always use `includeContents=false` for list operations, `true` only when loading a single conversation for display.

### Frontend Integration

For TypeScript/React clients, see [PlayFramework TypeScript Client README](../Rystem.PlayFramework.Client/src/rystem/README.md) for:
- `ContentUrlConverter` helper (Base64 → Blob URL conversion)
- React viewer components (ImageViewer, AudioPlayer, VideoPlayer, PDFViewer)
- Memory management best practices

---

### Authorization Best Practices

1. **HTTP Policies** - Validate JWT tokens, require authentication
2. **IAuthorizationLayer** - Extract `userId` for ownership
3. **Repository Checks** - Prevent cross-user access to private conversations

**Example flow:**
```csharp
public class CustomAuthorizationLayer : IAuthorizationLayer
{
    public async Task<AuthorizationResult> AuthorizeAsync(
        SceneContext context,
        SceneRequestSettings settings,
        CancellationToken cancellationToken)
    {
        // Extract userId from JWT claims (set by HTTP middleware)
        if (!context.Metadata.TryGetValue("userId", out var userId))
        {
            return new AuthorizationResult
            {
                IsAuthorized = false,
                Reason = "User ID not found"
            };
        }

        // Return userId for conversation ownership
        return new AuthorizationResult
        {
            IsAuthorized = true,
            UserId = userId.ToString()
        };
    }
}
```

**Middleware to extract userId:**
```csharp
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var userId = context.User.Identity.Name;
        // Will be available in PlayFramework via context.Metadata["userId"]
    }
    await next();
});
```

See [Repository Pattern Documentation](https://rystem.net/mcp/tools/repository-setup.md) for more storage options.

---

## ⚖️ Load Balancing & Fallback

Use **multiple LLM providers** for reliability:

```csharp
builder.Services.AddPlayFramework("default", pb => pb
    .AddChatClient(client => client
        // Primary pool (round-robin)
        .AddOpenAIChatClient("openai-primary", apiKey)
        .AddOpenAIChatClient("openai-secondary", apiKey)
        .AddAzureOpenAIChatClient("azure-backup", endpoint, apiKey)

        // Fallback chain (if primary pool fails)
        .AddFallback(fallback => fallback
            .AddAnthropicChatClient("claude-fallback", apiKey)
            .AddGoogleChatClient("gemini-fallback", apiKey))

        // Load balancing
        .WithLoadBalancing(LoadBalancingMode.RoundRobin) // RoundRobin | Random | LeastCost

        // Retry
        .WithRetry(retry =>
        {
            retry.MaxRetryAttempts = 3;
            retry.RetryBaseDelaySeconds = 1;
        })));
```

---

## 🚫 Rate Limiting

**Token Bucket** strategy (recommended):

```csharp
.WithRateLimiting(rateLimiting =>
{
    rateLimiting.Strategy = RateLimitingStrategy.TokenBucket;
    rateLimiting.MaxTokensPerMinute = 10000;      // 10K tokens/min
    rateLimiting.MaxRequestsPerMinute = 60;       // 60 requests/min
    rateLimiting.RefillIntervalSeconds = 10;      // Refill every 10s
})
```

**Redis-based** (distributed):
```csharp
.WithRateLimiting(rateLimiting =>
{
    rateLimiting.UseRedis("localhost:6379");
    rateLimiting.Strategy = RateLimitingStrategy.TokenBucket;
})
```

---

## 📊 Observability

### Logging

```csharp
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddApplicationInsights();
    logging.SetMinimumLevel(LogLevel.Information);
});
```

**Log Output:**
```
[13:45:23 INF] 🎯 Trying LoadBalanced client: openai-primary (Attempt 1/3)
[13:45:24 INF] ✅ LoadBalanced client openai-primary succeeded (Tokens: 150→89, Cost: $0.0023)
[13:45:24 INF] ⚙️ Executing tool: GetWeatherAsync (City: Milan)
[13:45:24 INF] ✅ Scene 'weather' completed (Cost: $0.0023, Tokens: 239)
```

### Telemetry

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddMeter("Rystem.PlayFramework"))
    .WithTracing(tracing => tracing
        .AddSource("Rystem.PlayFramework"));
```

**Metrics:**
- `playframework.request.duration`
- `playframework.request.cost`
- `playframework.request.tokens`
- `playframework.scene.executions`

---

## 🔐 Authorization

PlayFramework supports **two levels of authorization**:

1. **HTTP Endpoint Authorization** - ASP.NET Core policies (token validation, claims, roles)
2. **Business Logic Authorization** - Custom `IAuthorizationLayer` (user permissions, quotas, feature flags)

### HTTP Endpoint Authorization (ASP.NET Core Policies)

Apply standard ASP.NET Core authorization at the **HTTP endpoint level**:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Authenticated", policy => 
        policy.RequireAuthenticatedUser());

    options.AddPolicy("PlayFrameworkAccess", policy =>
        policy.RequireClaim("feature", "ai"));

    options.AddPolicy("PremiumUser", policy =>
        policy.RequireClaim("subscription", "premium"));
});

app.MapPlayFramework(settings =>
{
    settings.BasePath = "/api/ai";
    settings.RequireAuthentication = true;
    settings.AuthorizationPolicies = new List<string>
    {
        "Authenticated",
        "PlayFrameworkAccess"
    };
});
```

**When to use**: Token validation, JWT claims, role-based access, rate limiting policies.

---

### Business Logic Authorization (IAuthorizationLayer)

Implement **custom authorization logic** that runs **after initialization** but **before scene execution**:

```csharp
public class CustomAuthorizationLayer : IAuthorizationLayer
{
    private readonly IUserService _userService;
    private readonly ILogger<CustomAuthorizationLayer> _logger;

    public CustomAuthorizationLayer(
        IUserService userService,
        ILogger<CustomAuthorizationLayer> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public async Task<AuthorizationResult> AuthorizeAsync(
        SceneContext context,
        SceneRequestSettings settings,
        CancellationToken cancellationToken)
    {
        // Extract userId from metadata
        if (!context.Metadata.TryGetValue("userId", out var userIdObj) || userIdObj is not string userId)
        {
            return new AuthorizationResult
            {
                IsAuthorized = false,
                Reason = "User ID not found in request metadata"
            };
        }

        // Check user quota
        var user = await _userService.GetUserAsync(userId, cancellationToken);
        if (user.MonthlyQuota <= 0)
        {
            _logger.LogWarning("User {UserId} exceeded monthly quota", userId);
            return new AuthorizationResult
            {
                IsAuthorized = false,
                Reason = $"Monthly quota exceeded. Resets on {user.QuotaResetDate:yyyy-MM-dd}"
            };
        }

        // Check feature flag for specific scene
        if (settings.SceneName == "PremiumScene" && !user.HasFeature("premium-scenes"))
        {
            return new AuthorizationResult
            {
                IsAuthorized = false,
                Reason = "Premium subscription required for this feature"
            };
        }

        // Check budget limits
        if (settings.MaxBudget.HasValue && settings.MaxBudget.Value > user.MaxBudgetPerRequest)
        {
            return new AuthorizationResult
            {
                IsAuthorized = false,
                Reason = $"Requested budget ${settings.MaxBudget.Value} exceeds user limit ${user.MaxBudgetPerRequest}"
            };
        }

        // All checks passed
        _logger.LogInformation("User {UserId} authorized (Quota: {Quota}, Features: {Features})",
            userId, user.MonthlyQuota, string.Join(", ", user.Features));

        return new AuthorizationResult
        {
            IsAuthorized = true
        };
    }
}
```

**Register in PlayFramework:**
```csharp
builder.Services.AddPlayFramework("default", pb => pb
    .AddChatClient(...)
    .AddScene(...)
    .AddAuthorizationLayer<CustomAuthorizationLayer>());

// Register dependencies
builder.Services.AddSingleton<IUserService, UserService>();
```

**Response when authorization fails:**
```json
{
  "status": "error",
  "errorMessage": "Authorization failed: Monthly quota exceeded. Resets on 2025-03-01",
  "message": "You are not authorized to perform this action."
}
```

**When to use**:
- ✅ User-specific quotas (requests per month, tokens per day)
- ✅ Feature flags (beta features, premium scenes)
- ✅ Budget limits (max cost per request/user)
- ✅ Time-based restrictions (business hours only)
- ✅ Content filtering (block specific inputs)
- ✅ Multi-tenancy (tenant-specific permissions)

**Execution flow:**
1. **HTTP Request** → ASP.NET Core policies check (JWT, claims, roles)
2. **Initialization** → Load cache, initialize context, execute main actors
3. **Authorization Layer** → Custom business logic checks ← **YOU ARE HERE**
4. **Scene Execution** → If authorized, execute selected scenes
5. **Response** → Return results

---

### Combining Both Levels

```csharp
// 1. HTTP Endpoint Authorization (ASP.NET Core)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Authenticated", policy => 
        policy.RequireAuthenticatedUser());
});

app.MapPlayFramework(settings =>
{
    settings.BasePath = "/api/ai";
    settings.RequireAuthentication = true;
    settings.AuthorizationPolicies = new List<string> { "Authenticated" };
});

// 2. Business Logic Authorization (IAuthorizationLayer)
builder.Services.AddPlayFramework("default", pb => pb
    .AddChatClient(...)
    .AddAuthorizationLayer<CustomAuthorizationLayer>());
```

**Both must pass** for execution to proceed:
- ❌ **Fail at HTTP level** → 401/403 HTTP error (no execution)
- ✅ **Pass HTTP level** → Continue to initialization
- ❌ **Fail at business level** → Custom error response (after initialization)
- ✅ **Pass business level** → Execute scenes

See [AUTHORIZATION_EXAMPLE.md](AUTHORIZATION_EXAMPLE.md) for comprehensive examples.

---

## 🛡️ Guardrails (Operational Boundaries)

**Guardrails** prevent the AI from **hallucinating** or responding to requests **outside the system's capabilities**. When enabled, a system prompt is automatically added at the beginning of every new conversation to define what the AI can and cannot do.

### Why Use Guardrails?

Without guardrails, LLMs may:
- ❌ Invent tools or scenes that don't exist
- ❌ Reference external systems or APIs not available
- ❌ Respond to questions unrelated to your application's purpose
- ❌ Hallucinate function signatures or parameters

**With guardrails**, the AI:
- ✅ Uses ONLY registered scenes, actors, and tools
- ✅ Stays within the defined context
- ✅ Suggests alternatives when a request is out of scope
- ✅ Asks for clarification instead of inventing capabilities

### Enable Default Guardrails

The **default prompt** (~150 tokens) instructs the AI to operate strictly within available capabilities:

```csharp
builder.Services.AddPlayFramework("default", pb => pb
    .AddChatClient(...)
    .UseDefaultGuardrails()  // Adds default operational boundaries
    .AddScene("weather", "Get weather information", scene => scene
        .WithService<IWeatherService>(s => s.AddTool(x => x.GetWeatherAsync)))
    .AddScene("calculator", "Perform calculations", scene => scene
        .WithService<ICalculatorService>(s => s.AddTool(x => x.Add))));
```

**Default prompt:**
```plaintext
You are a PlayFramework AI orchestrator. You can ONLY respond using:
- Available Scenes (specialized handlers for specific tasks)
- Available Actors (context providers and data enrichers)
- Available Tools (functions you can call)

RULES:
1. Use ONLY the scenes, actors, and tools explicitly registered in this system
2. If a request is outside available capabilities, explain what you CAN do instead
3. When selecting a scene, match user intent to scene purpose
4. When calling tools, use exact function signatures provided
5. Stay within the context provided by main actors and system context
6. Do NOT invent capabilities, hallucinate tools, or reference external systems

If unsure, ask for clarification within your available capabilities.
```

### Enable Custom Guardrails

Define **your own boundaries** for domain-specific applications:

```csharp
builder.Services.AddPlayFramework("default", pb => pb
    .AddChatClient(...)
    .UseCustomGuardrails(@"
        You are an AI assistant for the XYZ Corporation customer support system.

        CAPABILITIES:
        - Check order status (GetOrderStatus tool)
        - Process returns (InitiateReturn tool)
        - Answer product questions using product database

        RESTRICTIONS:
        - Do NOT discuss pricing, discounts, or promotions (direct to sales team)
        - Do NOT process refunds over $500 (escalate to manager)
        - Do NOT share customer data from other accounts
        - Stay professional and empathetic

        If a request is outside these capabilities, politely explain what you CAN help with.
    ")
    .AddScene("orders", "Manage customer orders", scene => scene
        .WithService<IOrderService>(s => s
            .AddTool(x => x.GetOrderStatus)
            .AddTool(x => x.InitiateReturn))));
```

### When to Use Guardrails

| Scenario | Recommendation |
|----------|---------------|
| **General-purpose chatbot** | ✅ Use default guardrails |
| **Domain-specific app** (e.g., HR, finance, healthcare) | ✅ Use custom guardrails with strict policies |
| **Open exploration** (research assistant, creative writing) | ⚠️ Consider disabling (but monitor for misuse) |
| **Production systems** | ✅ Always enable (default or custom) |

### Execution Flow with Guardrails

1. **New Conversation** → Guardrails prompt added as **first system message**
2. **Context & Actors** → Main actors provide additional context
3. **User Message** → User's actual request
4. **Scene Selection** → LLM chooses scene based on guardrails + context
5. **Tool Execution** → LLM uses only allowed tools

**Note:** Guardrails are only added for **new conversations** (not when resuming from cache).

### Example Behavior

**Without Guardrails:**
```
User: "Can you book a flight to Paris?"
AI: "Sure! I'll use the FlightBookingAPI to search for flights..." ❌ (invented tool)
```

**With Default Guardrails:**
```
User: "Can you book a flight to Paris?"
AI: "I don't have a flight booking capability. I can help with: weather information and calculations. Would you like to check the weather in Paris instead?" ✅
```

**With Custom Guardrails (customer support):**
```
User: "Can you give me a 50% discount?"
AI: "I'm not able to discuss pricing or discounts. Please contact our sales team at sales@xyz.com for promotional offers." ✅
```

### Best Practices

1. **✅ Enable in production** - Always use guardrails to prevent unexpected behavior
2. **✅ Keep prompts concise** - Guardrails consume tokens in every request (~100-200 tokens)
3. **✅ Test edge cases** - Verify AI rejects out-of-scope requests gracefully
4. **✅ Combine with authorization** - Use `IAuthorizationLayer` for user-specific restrictions
5. **✅ Monitor logs** - Track when guardrails prevent hallucinations

---

## 📡 API Endpoints

### Step-by-Step Streaming (SSE)

**POST** `/api/ai/{factoryName}`

**Request:**
```json
{
  "message": "What's the weather in Milan?",
  "settings": {
    "executionMode": "Direct",
    "maxBudget": 0.10
  }
}
```

**Response (text/event-stream):**
```
data: {"status":"executingScene","sceneName":"weather","message":"Calling weather API"}

data: {"status":"streaming","streamingChunk":"The weather"}

data: {"status":"streaming","streamingChunk":" in Milan"}

data: {"status":"completed","message":"The weather in Milan is sunny, 24°C"}
```

### Token-Level Streaming

**POST** `/api/ai/{factoryName}/streaming`

Returns **individual text chunks** as they're generated.

### Non-Streaming

Set `enableStreaming: false` in settings to get full response at once.

---

## 🧪 Testing

### Unit Test Example

```csharp
[Fact]
public async Task Scene_Should_Execute_Successfully()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddPlayFramework("test", pb => pb
        .AddChatClient(client => client
            .AddMockChatClient("mock", responses: new[] { "Sunny, 24°C" }))
        .AddScene("weather", "Get weather", scene => scene
            .WithService<IWeatherService>(s => s.AddTool(x => x.GetWeatherAsync))));

    services.AddSingleton<IWeatherService, WeatherService>();

    var provider = services.BuildServiceProvider();
    var sceneManager = provider.GetRequiredService<ISceneManager>();

    // Act
    var response = await sceneManager.ExecuteAsync(new PlayFrameworkRequest
    {
        Message = "Weather in Milan?",
        Settings = new SceneRequestSettings { FactoryName = "test" }
    });

    // Assert
    Assert.Equal(AiResponseStatus.Completed, response.Status);
    Assert.Contains("Sunny", response.FinalMessage);
}
```

---

## 💡 Complete Example

### E-Commerce Assistant

```csharp
public interface IProductService
{
    Task<List<Product>> SearchProductsAsync(string query);
    Task<Product> GetProductDetailsAsync(int productId);
}

public interface ICartService
{
    Task AddToCartAsync(int productId, int quantity);
    Task<Cart> GetCartAsync(string userId);
}

builder.Services.AddPlayFramework("shop", pb => pb
    .AddChatClient(client => client
        .AddOpenAIChatClient("gpt-4o", apiKey))

    // Search scene
    .AddScene("product-search", "Search for products", scene => scene
        .WithSystemPrompt("You are a helpful shopping assistant. Help users find products.")
        .WithService<IProductService>(s => s
            .AddTool(x => x.SearchProductsAsync)
            .AddTool(x => x.GetProductDetailsAsync)))

    // Cart scene
    .AddScene("cart-management", "Manage shopping cart", scene => scene
        .WithService<ICartService>(s => s
            .AddTool(x => x.AddToCartAsync)
            .AddTool(x => x.GetCartAsync)))

    // Confirmation scene (client-side)
    .AddScene("checkout", "Complete purchase", scene => scene
        .OnClient(client => client
            .AddTool("getUserConfirmation", "Ask user to confirm purchase"))
        .WithService<ICheckoutService>(s => s
            .AddTool(x => x.ProcessPaymentAsync)))

    // Enable planning for multi-step workflows
    .WithPlanning(planning =>
    {
        planning.Enabled = true;
        planning.MaxRecursionDepth = 5;
    })

    // Cache for conversation state
    .WithCache(cache => cache
        .WithRedis("localhost:6379")
        .WithExpiration(TimeSpan.FromMinutes(30)))

    // Rate limiting
    .WithRateLimiting(rateLimiting =>
    {
        rateLimiting.Strategy = RateLimitingStrategy.TokenBucket;
        rateLimiting.MaxTokensPerMinute = 5000;
    }));

app.MapPlayFramework("shop", settings =>
{
    settings.BasePath = "/api/shop";
    settings.RequireAuthentication = true;
});
```

**Usage:**
```bash
curl -X POST http://localhost:5158/api/shop/shop \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "message": "Find me a red t-shirt, size M, add to cart and checkout",
    "settings": {
      "executionMode": "Planning",
      "conversationKey": "user-123-session-1"
    }
  }'
```

**Flow:**
1. **Planning** - Creates plan: Search → Add to Cart → Checkout
2. **Product Search** - Calls `SearchProductsAsync("red t-shirt")`
3. **Cart Management** - Calls `AddToCartAsync(productId, quantity)`
4. **Checkout** - Triggers client-side confirmation → Calls `ProcessPaymentAsync`

---

## 📚 Additional Resources

- 📖 **Full Documentation**: https://rystem.net/playframework
- 💻 **GitHub**: https://github.com/KeyserDSoze/Rystem
- 📦 **NuGet**: https://www.nuget.org/packages/Rystem.PlayFramework
- 🎯 **MCP Tools**: https://rystem.net/mcp
- 📘 **Client Interaction Guide**: [Docs/CLIENT_INTERACTION_ARCHITECTURE_V2.md](Docs/CLIENT_INTERACTION_ARCHITECTURE_V2.md)
- 🔐 **Authorization Examples**: [AUTHORIZATION_EXAMPLE.md](AUTHORIZATION_EXAMPLE.md)
- 📊 **Streaming Technical Details**: [Docs/STREAMING_TECHNICAL.md](Docs/STREAMING_TECHNICAL.md)

---

## 🤝 Contributing

Contributions welcome! Please open an issue or PR.

---

## 📄 License

MIT © [Alessandro Rapiti](https://github.com/KeyserDSoze)

---

**Made with ❤️ by the Rystem team**
