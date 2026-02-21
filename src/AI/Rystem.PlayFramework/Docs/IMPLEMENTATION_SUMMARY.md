# PlayFramework Minimal API - Proposta Implementativa

## 📋 Sommario

Ho implementato una soluzione completa per esporre PlayFramework tramite **Minimal API** con supporto per:

✅ **Endpoint streaming** (Server-Sent Events)  
✅ **Endpoint non-streaming** (JSON response)  
✅ **URI configurabile** da settings  
✅ **Multi-modal support** (immagini, audio, video, file)  
✅ **Metadata automatici** (userId, IP, requestId)  
✅ **Multiple factory** support  

## 🏗️ Struttura Creata

### 1. **PlayFrameworkApiSettings.cs**
Configurazione per gli endpoint HTTP:
- `BasePath`: percorso base (default: `/playframework`)
- `EnableCompression`: compressione per streaming
- `MaxRequestBodySize`: limite dimensione request (10MB default)
- `EnableAutoMetadata`: estrazione automatica di userId, IP, requestId
- `RequireAuthentication`: abilita autenticazione

### 2. **PlayFrameworkRequest.cs** (Models)
Contract per le richieste HTTP:
```csharp
{
  "message": "User message",
  "contents": [/* multi-modal items */],
  "metadata": {/* userId, sessionId, etc. */},
  "settings": {/* override defaults */}
}
```

### 3. **PlayFrameworkResponse.cs** (Models)
Contract per le risposte HTTP non-streaming:
```csharp
{
  "responses": [/* tutti gli AiSceneResponse */],
  "finalMessage": "Last running message",
  "status": "Success",
  "totalCost": 0.0023,
  "totalTokens": 150,
  "elapsedMilliseconds": 1240
}
```

### 4. **WebApplicationExtensions.cs**
Extension methods per `WebApplication`:

#### **Opzione A: Tutti i factory (dinamico)**
```csharp
app.MapPlayFramework(settings => 
{
    settings.BasePath = "/api/ai";
});
```
Crea endpoint:
- `POST /api/ai/{factoryName}`
- `POST /api/ai/{factoryName}/streaming`

#### **Opzione B: Factory specifico**
```csharp
app.MapPlayFramework("chat", settings => 
{
    settings.BasePath = "/api/chat";
});
```
Crea endpoint:
- `POST /api/chat`
- `POST /api/chat/streaming`

## 🚀 Pattern di Utilizzo

### Setup Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Registra PlayFramework con multiple factory
builder.Services.AddPlayFramework("chat", pb => pb
    .AddChatClient("gpt-4o")
    .AddScene<WeatherScene>());

builder.Services.AddPlayFramework("assistant", pb => pb
    .AddChatClient("claude-sonnet")
    .AddScene<CodeReviewScene>());

var app = builder.Build();

// OPZIONE 1: Map per TUTTI i factory
app.MapPlayFramework(settings =>
{
    settings.BasePath = "/api/ai";
    settings.RequireAuthentication = true;
});

// OPZIONE 2: Map per factory SPECIFICI
app.MapPlayFramework("chat", settings =>
{
    settings.BasePath = "/api/chat";
});

app.Run();
```

## 📡 Endpoint Patterns

### Pattern 1: Multi-Factory Dinamico

**URI**: `/playframework/{factoryName}`

```http
POST /playframework/chat
POST /playframework/chat/streaming
POST /playframework/assistant
POST /playframework/assistant/streaming
```

**Vantaggi**:
- ✅ Un solo gruppo di endpoint per tutti i factory
- ✅ Scalabile (aggiungi factory senza modificare endpoint)
- ✅ Perfetto per API pubbliche

### Pattern 2: Factory Dedicati

**URI**: Customizzabile per factory

```http
POST /api/chat
POST /api/chat/streaming
POST /api/assistant
POST /api/assistant/streaming
```

**Vantaggi**:
- ✅ URI più pulite e specifiche
- ✅ Rate limiting separato per factory
- ✅ Autenticazione/CORS personalizzati
- ✅ Perfetto per micro-servizi

## 🎯 Esempi Client

### JavaScript (Non-Streaming)

```javascript
const response = await fetch('/api/ai/chat', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    message: "What's the weather in Milan?",
    metadata: { userId: 'user123' }
  })
});

const result = await response.json();
console.log(result.finalMessage);
console.log('Cost:', result.totalCost);
```

### JavaScript (Streaming - SSE)

```javascript
const response = await fetch('/api/ai/chat/streaming', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    message: "Write a poem",
    metadata: { userId: 'user123' }
  })
});

const reader = response.body.getReader();
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
      }
    }
  }
}
```

### C# HttpClient (Non-Streaming)

```csharp
using var client = new HttpClient();

var request = new PlayFrameworkRequest
{
    Message = "What's the weather in Milan?",
    Metadata = new Dictionary<string, object> 
    { 
        ["userId"] = "user123" 
    }
};

var response = await client.PostAsJsonAsync(
    "https://your-api.com/api/ai/chat",
    request);

var result = await response.Content
    .ReadFromJsonAsync<PlayFrameworkResponse>();

Console.WriteLine(result.FinalMessage);
```

## 🔒 Funzionalità Avanzate

### Rate Limiting

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("playframework", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});

app.UseRateLimiter();

app.MapPlayFramework()
   .RequireRateLimiting("playframework");
```

### CORS

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://your-frontend.com")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

app.UseCors("AllowFrontend");

app.MapPlayFramework()
   .RequireCors("AllowFrontend");
```

### Authentication

```csharp
app.MapPlayFramework(settings =>
{
    settings.BasePath = "/api/ai";
    settings.RequireAuthentication = true;
    settings.EnableAutoMetadata = true; // Estrae userId da claims
});
```

### Multi-Modal Content

**Request con immagine**:
```json
{
  "message": "What's in this image?",
  "contents": [
    {
      "type": "image",
      "data": "iVBORw0KGgoAAAANSUhEUgAAAAUA...",
      "mediaType": "image/png",
      "name": "screenshot.png"
    }
  ]
}
```

## 🔄 Response Format

### Non-Streaming Response

```json
{
  "responses": [
    {
      "status": "Planning",
      "message": "Creating execution plan",
      "timestamp": "2026-02-15T10:30:00Z"
    },
    {
      "status": "ExecutingScene",
      "sceneName": "WeatherScene",
      "message": "Calling weather API"
    },
    {
      "status": "Running",
      "message": "The weather in Milan is sunny, 24°C",
      "totalCost": 0.0023,
      "totalTokens": 150
    }
  ],
  "finalMessage": "The weather in Milan is sunny, 24°C",
  "status": "Success",
  "totalCost": 0.0023,
  "totalTokens": 150,
  "elapsedMilliseconds": 1240,
  "metadata": {
    "userId": "user123",
    "ipAddress": "192.168.1.100",
    "requestId": "0HN7S8QG9K3PQ:00000001"
  }
}
```

### Streaming Response (SSE)

```
data: {"status":"Planning","message":"Creating execution plan"}

data: {"status":"Streaming","streamingChunk":"The weather","isStreamingComplete":false}

data: {"status":"Streaming","streamingChunk":" in Milan","isStreamingComplete":false}

data: {"status":"Running","message":"The weather in Milan is sunny, 24°C","totalCost":0.0023}

data: {"status":"completed"}
```

## ✅ Vantaggi della Soluzione

1. **Configurabile**: URI base personalizzabile per ambiente
2. **Flessibile**: Supporta multi-factory o factory dedicati
3. **Type-safe**: Contract models per request/response
4. **Streaming nativo**: SSE per real-time updates
5. **Multi-modal ready**: Supporto completo per immagini/audio/file
6. **Production-ready**: Rate limiting, CORS, auth, telemetry
7. **Auto-metadata**: Estrazione automatica di userId, IP, requestId
8. **Error handling**: Gestione errori standardizzata (Problem Details)

## 📦 File Creati

```
Rystem.PlayFramework/
├── Api/
│   ├── PlayFrameworkApiSettings.cs
│   ├── WebApplicationExtensions.cs
│   ├── README.md (esempi completi)
│   └── Models/
│       ├── PlayFrameworkRequest.cs
│       └── PlayFrameworkResponse.cs
└── Rystem.PlayFramework.csproj (aggiunto FrameworkReference)
```

## 🎓 Prossimi Passi

1. **Test**: Creare test di integrazione per gli endpoint
2. **Swagger**: Aggiungere annotazioni OpenAPI
3. **Versioning**: Supporto per versioning API (v1, v2)
4. **WebSocket**: Alternativa a SSE per streaming bidirezionale
5. **gRPC**: Endpoint gRPC per performance elevate
6. **Health checks**: Endpoint `/health` per monitoring
7. **Metrics**: Esporta metriche Prometheus

## 💡 Note Tecniche

- **FrameworkReference**: Aggiunto `Microsoft.AspNetCore.App` al `.csproj` per minimal API
- **SSE Format**: Standard `data: {json}\n\n` per compatibilità browser
- **Token budget**: Response accumulate per reporting completo
- **Cancellation**: Supporto CancellationToken per timeout/abort
- **Compression**: Opzionale per streaming (default: enabled)

---

**Build Status**: ✅ Build succeeded (solo warning NuGet, non bloccanti)
