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

    // Add scene with service tool
    .AddScene("weather", "Get weather information", scene => scene
        .WithService<IWeatherService>(s => s
            .AddTool(x => x.GetWeatherAsync)))

    // Add cache for conversation state
    .WithCache(cache => cache
        .WithInMemory()
        .WithExpiration(TimeSpan.FromMinutes(30))));

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
    }));
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

### Global Policies

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Authenticated", policy => 
        policy.RequireAuthenticatedUser());

    options.AddPolicy("PlayFrameworkAccess", policy =>
        policy.RequireClaim("feature", "ai"));
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

### Factory-Specific Policies

```csharp
app.MapPlayFramework("premium", settings =>
{
    settings.BasePath = "/api/ai/premium";
    settings.FactoryPolicies = new Dictionary<string, List<string>>
    {
        { "premium", new List<string> { "PremiumUser" } }
    };
});
```

See [AUTHORIZATION_EXAMPLE.md](AUTHORIZATION_EXAMPLE.md) for details.

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
