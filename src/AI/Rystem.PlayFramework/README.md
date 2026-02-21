# PlayFramework Minimal API - Usage Examples

## Setup

### 1. Register PlayFramework in Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register PlayFramework with multiple factories
builder.Services.AddPlayFramework("chat", pb => pb
    .AddChatClient("gpt-4o")
    .AddScene<WeatherScene>()
    .AddScene<SearchScene>());

builder.Services.AddPlayFramework("assistant", pb => pb
    .AddChatClient("claude-sonnet")
    .AddScene<CodeReviewScene>()
    .AddScene<DocumentationScene>());

var app = builder.Build();

// Map PlayFramework endpoints for ALL factories
// Creates: POST /api/ai/{factoryName} - Step-by-step streaming (each step as SSE event)
//          POST /api/ai/{factoryName}/streaming - Token-level streaming (each text chunk as SSE event)
app.MapPlayFramework(settings =>
{
    settings.BasePath = "/api/ai";
    settings.RequireAuthentication = true;
    settings.EnableAutoMetadata = true;
});

// OR: Map endpoints for a SPECIFIC factory
// Creates: POST /api/chat - Step-by-step streaming
//          POST /api/chat/streaming - Token-level streaming
app.MapPlayFramework("chat", settings =>
{
    settings.BasePath = "/api/chat";
});

app.Run();
```

## API Endpoints

### Non-Streaming Endpoint

**POST** `/api/ai/{factoryName}`

**Request Body:**
```json
{
  "message": "What's the weather in Milan?",
  "metadata": {
    "userId": "user123",
    "sessionId": "session456"
  },
  "settings": {
    "maxBudget": 0.10,
    "enableStreaming": false
  }
}
```

**Response (200 OK):**
```json
{
  "responses": [
    {
      "status": "Running",
      "sceneName": "WeatherScene",
      "message": "The weather in Milan is sunny, 24°C",
      "totalCost": 0.0023,
      "timestamp": "2026-02-15T10:30:00Z"
    }
  ],
  "finalMessage": "The weather in Milan is sunny, 24°C",
  "status": "Success",
  "totalCost": 0.0023,
  "totalTokens": 150,
  "elapsedMilliseconds": 1240,
  "metadata": {
    "userId": "user123",
    "sessionId": "session456",
    "ipAddress": "192.168.1.100",
    "requestId": "0HN7S8QG9K3PQ:00000001"
  }
}
```

### Streaming Endpoint (SSE)

**POST** `/api/ai/{factoryName}/streaming`

**Request Body:** (same as non-streaming)

**Response (text/event-stream):**
```
data: {"status":"Planning","message":"Creating execution plan"}

data: {"status":"ExecutingScene","sceneName":"WeatherScene","message":"Calling weather API"}

data: {"status":"Streaming","streamingChunk":"The weather","isStreamingComplete":false}

data: {"status":"Streaming","streamingChunk":" in Milan","isStreamingComplete":false}

data: {"status":"Running","message":"The weather in Milan is sunny, 24°C","totalCost":0.0023}

data: {"status":"completed"}
```

## Multi-Modal Request

### Send Image with Text

```json
{
  "message": "What's in this image?",
  "contents": [
    {
      "type": "image",
      "data": "iVBORw0KGgoAAAANSUhEUgAAAAUA...", 
      "mediaType": "image/png",
      "name": "photo.png"
    }
  ],
  "metadata": {
    "userId": "user123"
  }
}
```

### Send Audio File

```json
{
  "message": "Transcribe this audio",
  "contents": [
    {
      "type": "audio",
      "data": "UklGRnoGAABXQVZFZm10IBAAAA...",
      "mediaType": "audio/mp3",
      "name": "recording.mp3"
    }
  ]
}
```

### Multiple Contents

```json
{
  "message": "Compare these two images",
  "contents": [
    {
      "type": "image",
      "data": "base64_data_1...",
      "mediaType": "image/jpeg",
      "name": "image1.jpg"
    },
    {
      "type": "image",
      "data": "base64_data_2...",
      "mediaType": "image/jpeg",
      "name": "image2.jpg"
    }
  ]
}
```

## Client Examples

### JavaScript/TypeScript (Fetch API)

```typescript
// Non-streaming request
async function executePlayFramework(factoryName: string, message: string) {
  const response = await fetch(`/api/ai/${factoryName}`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': 'Bearer YOUR_TOKEN'
    },
    body: JSON.stringify({
      message,
      metadata: {
        userId: 'user123',
        sessionId: 'session456'
      }
    })
  });

  const result = await response.json();
  console.log('Final message:', result.finalMessage);
  console.log('Total cost:', result.totalCost);
}

// Streaming request (SSE)
async function executePlayFrameworkStreaming(factoryName: string, message: string) {
  const response = await fetch(`/api/ai/${factoryName}/streaming`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': 'Bearer YOUR_TOKEN'
    },
    body: JSON.stringify({
      message,
      metadata: { userId: 'user123' }
    })
  });

  const reader = response.body!.getReader();
  const decoder = new TextDecoder();

  while (true) {
    const { value, done } = await reader.read();
    if (done) break;

    const chunk = decoder.decode(value);
    const lines = chunk.split('\n\n');

    for (const line of lines) {
      if (line.startsWith('data: ')) {
        const data = JSON.parse(line.slice(6));
        
        if (data.status === 'Streaming') {
          process.stdout.write(data.streamingChunk);
        } else if (data.status === 'completed') {
          console.log('\n✓ Completed');
        } else {
          console.log(`[${data.status}] ${data.message || ''}`);
        }
      }
    }
  }
}
```

### C# HttpClient

```csharp
using System.Net.Http.Json;
using System.Text.Json;

// Non-streaming request
async Task<PlayFrameworkResponse> ExecuteAsync(string factoryName, string message)
{
    using var client = new HttpClient();
    client.DefaultRequestHeaders.Add("Authorization", "Bearer YOUR_TOKEN");

    var request = new PlayFrameworkRequest
    {
        Message = message,
        Metadata = new Dictionary<string, object>
        {
            ["userId"] = "user123",
            ["sessionId"] = "session456"
        }
    };

    var response = await client.PostAsJsonAsync(
        $"https://your-api.com/api/ai/{factoryName}",
        request);

    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<PlayFrameworkResponse>();
}

// Streaming request
async Task ExecuteStreamingAsync(string factoryName, string message)
{
    using var client = new HttpClient();
    client.DefaultRequestHeaders.Add("Authorization", "Bearer YOUR_TOKEN");

    var request = new PlayFrameworkRequest
    {
        Message = message,
        Metadata = new Dictionary<string, object> { ["userId"] = "user123" }
    };

    var response = await client.PostAsJsonAsync(
        $"https://your-api.com/api/ai/{factoryName}/streaming",
        request);

    response.EnsureSuccessStatusCode();

    await using var stream = await response.Content.ReadAsStreamAsync();
    using var reader = new StreamReader(stream);

    while (!reader.EndOfStream)
    {
        var line = await reader.ReadLineAsync();
        if (string.IsNullOrEmpty(line)) continue;

        if (line.StartsWith("data: "))
        {
            var json = line.Substring(6);
            var data = JsonSerializer.Deserialize<AiSceneResponse>(json);

            if (data?.Status == AiResponseStatus.Streaming)
            {
                Console.Write(data.StreamingChunk);
            }
            else
            {
                Console.WriteLine($"[{data?.Status}] {data?.Message}");
            }
        }
    }
}
```

## Advanced Configuration

### Custom Base Path per Factory

```csharp
// Chat factory at /api/chat
app.MapPlayFramework("chat", settings =>
{
    settings.BasePath = "/api/chat";
    settings.RequireAuthentication = false;
});

// Assistant factory at /api/assistant
app.MapPlayFramework("assistant", settings =>
{
    settings.BasePath = "/api/assistant";
    settings.RequireAuthentication = true;
});
```

### Rate Limiting Integration

```csharp
using Microsoft.AspNetCore.RateLimiting;

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("playframework", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});

var app = builder.Build();
app.UseRateLimiter();

app.MapPlayFramework(settings =>
{
    settings.BasePath = "/api/ai";
})
.RequireRateLimiting("playframework");
```

### CORS Configuration

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowPlayFramework", policy =>
    {
        policy.WithOrigins("https://your-frontend.com")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();
app.UseCors("AllowPlayFramework");

app.MapPlayFramework().RequireCors("AllowPlayFramework");
```

## Error Handling

**Error Response (500 Internal Server Error):**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "PlayFramework Execution Failed",
  "status": 500,
  "detail": "Scene 'WeatherScene' not found"
}
```

**Error in Streaming (SSE):**
```
data: {"status":"error","errorMessage":"Scene 'WeatherScene' not found"}
```

## Authorization Policies

PlayFramework supports **ASP.NET Core authorization policies** for fine-grained access control.

### Global Policies

Apply policies to all factories:

```csharp
// Register policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Authenticated", policy => 
        policy.RequireAuthenticatedUser());
    
    options.AddPolicy("PlayFrameworkAccess", policy =>
        policy.RequireClaim("feature", "ai"));
    
    options.AddPolicy("PremiumUser", policy =>
        policy.RequireClaim("subscription", "premium"));
});

// Apply to endpoints
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

Apply different policies per factory (only for single-factory endpoints):

```csharp
// Premium factory with premium-only access
app.MapPlayFramework("premium", settings =>
{
    settings.BasePath = "/api/ai/premium";
    settings.RequireAuthentication = true;
    
    // Global policies
    settings.AuthorizationPolicies = new List<string>
    {
        "Authenticated"
    };
    
    // Factory-specific policies
    settings.FactoryPolicies = new Dictionary<string, List<string>>
    {
        { "premium", new List<string> { "PremiumUser" } }
    };
});

// Admin factory with admin-only access
app.MapPlayFramework("admin", settings =>
{
    settings.BasePath = "/api/ai/admin";
    settings.RequireAuthentication = true;
    settings.AuthorizationPolicies = new List<string> { "Authenticated" };
    settings.FactoryPolicies = new Dictionary<string, List<string>>
    {
        { "admin", new List<string> { "AdminOnly" } }
    };
});
```

**Note**: Factory-specific policies are **only available** in single-factory endpoints (`MapPlayFramework("factoryName")`). Multi-factory endpoints (`MapPlayFramework()`) only support global policies.

### Multiple Policies

All policies must pass for authorization to succeed:

```csharp
app.MapPlayFramework("secure", settings =>
{
    settings.AuthorizationPolicies = new List<string>
    {
        "Authenticated",
        "PlayFrameworkAccess",
        "RateLimitApproved"
    };
    settings.FactoryPolicies = new Dictionary<string, List<string>>
    {
        { "secure", new List<string> { "AdminOnly", "AuditLogged" } }
    };
});
```

See [AUTHORIZATION_EXAMPLE.md](AUTHORIZATION_EXAMPLE.md) for comprehensive examples.
