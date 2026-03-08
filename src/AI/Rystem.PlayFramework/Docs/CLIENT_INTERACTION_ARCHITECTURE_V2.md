# Client-Side Interaction Architecture V2
## Architettura OnClient() con Continuation Token e Cache

> Status note: this document describes an earlier design iteration. The current HTTP contract no longer uses request-level `Prompt` or `ContinuationToken` fields. The implemented API resumes client interactions with top-level `ConversationKey` plus `ClientInteractionResults`, and direct scene selection lives under `Settings.SceneName`.

---

## 📋 Executive Summary

Feature che permette al PlayFramework di delegare l'esecuzione di specifici tool al client (browser/mobile app) tramite:
- ✅ Builder pattern **`scene.OnClient()`** dichiarativo
- ✅ **Continuation Token** per riprendere esecuzione dopo client action
- ✅ **Cache obbligatoria** (in-memory o Redis) per stato scene
- ✅ **Connessione non aperta**: risposta immediata + nuova POST
- ✅ Supporto multi-modale con **DataContent** (file/immagini Base64) e **TextContent** (messaggi)
- ✅ Type safety end-to-end con **`AddTool<T>()`** e JSON Schema generation

---

## 🔄 Flusso di Esecuzione Completo (con Continuation Token)

```
┌─────────────────────────────────────────────────────────────────────────┐
│ FASE 1: Client Invia Richiesta Iniziale                                │
└─────────────────────────────────────────────────────────────────────────┘

Client (Browser)                    Server (PlayFramework)
     │                                      │
     ├──POST /api/ai/default───────────────>│
     │  {                                   │
     │    message: "Take a photo and       │
     │              analyze it",            │
     │    conversationKey: "conv-abc123",  │
     │    settings: {                        │
     │      executionMode: "Scene",        │
     │      sceneName: "VisionAnalysis"    │
     │    }                                  │
     │  }                                   │
     │                                      │
     │                              ┌───────┴────────┐
     │                              │ SceneManager   │
     │                              │ - Esegue scene │
     │                              │ - Trova tool   │
     │                              │   OnClient()   │
     │                              │ - SALVA stato  │
     │                              │   in CACHE     │
     │                              └───────┬────────┘
     │                                      │
     │<──SSE Event: AwaitingClient──────────┤
     │  {                                   │
     │    status: "AwaitingClient",         │
     │    conversationKey: "conv-abc123",   │
     │    continuationToken: "token-xyz",   │ ◄── Genera GUID
     │    clientInteraction: {              │
     │      interactionId: "guid-123",      │
     │      toolName: "CapturePhoto",       │
     │      arguments: {                    │
     │        quality: "high",              │
     │        maxWidth: 1920                │
     │      },                              │
     │      argumentsSchema: "{...}",       │ ◄── JSON Schema generato
     │      description: "Capture photo...",│
     │      timeoutSeconds: 60              │
     │    }                                 │
     │  }                                   │
     │                                      │
     │<──Connection CLOSED──────────────────┤ Stato salvato in cache!
     │                                      │ TTL: 5 minuti

┌─────────────────────────────────────────────────────────────────────────┐
│ FASE 2: Client Esegue Tool Locale                                      │
│         ⏱️ Può impiegare secondi/minuti - server NON aspetta            │
└─────────────────────────────────────────────────────────────────────────┘

Client (Browser)
     │
     │ [Tempo indefinito - utente interagisce]
     │
     ├──navigator.mediaDevices.getUserMedia()
     │  │
     │  └──> Utente scatta foto (⏱️ 5-30 secondi)
     │        │
     │        └──> Blob (image/jpeg, 2.5MB)
     │             │
     │             ├──> AIContentConverter.fromFile(blob)
     │             │    → DataContent { data: "base64...", mediaType: "image/jpeg" }
     │             │
     │             └──> Prepara ClientInteractionResult

┌─────────────────────────────────────────────────────────────────────────┐
│ FASE 3: Client Invia Risultato con Continuation Token                  │
└─────────────────────────────────────────────────────────────────────────┘

Client                              Server
     │                                      │
     ├──POST /api/ai/default───────────────>│ ◄── NUOVA richiesta HTTP
     │  {                                   │
     │    conversationKey: "conv-abc123",   │ ◄── STESSO ConversationKey
     │    continuationToken: "token-xyz",   │ ◄── Token per riprendere
     │    clientInteractionResults: [       │
     │      {                               │
     │        interactionId: "guid-123",    │
     │        contents: [                   │ ◄── AIContent array
     │          {                           │
     │            "data": "iVBORw0K...",    │ ◄── Base64
     │            "mediaType": "image/jpeg" │
     │          },                          │
     │          {                           │
     │            "text": "Photo captured  │
     │                     successfully"    │
     │          }                           │
     │        ],                            │
     │        executedAt: "2026-02-15..."   │
     │      }                               │
     │    ]                                 │
     │  }                                   │
     │                                      │
     │                              ┌───────┴────────┐
     │                              │ ClientInteractionHandler
     │                              │ - Valida token │
     │                              │ - CARICA stato │
     │                              │   da CACHE     │
     │                              │ - Deserializza │
     │                              │   AIContent[]  │
     │                              │ - Rimuove      │
     │                              │   token cache  │
     │                              └───────┬────────┘
     │                                      │
     │                              ┌───────┴────────┐
     │                              │ SceneManager   │
     │                              │ RESUME esecuzione
     │                              │ - Aggiunge     │
     │                              │   AIContent[]  │
     │                              │   a chat       │
     │                              │ - Invia a LLM  │
     │                              └───────┬────────┘
     │                                      │
     │<──SSE Event: Running─────────────────┤
     │  {                                   │
     │    status: "Running",                │
     │    message: "Analyzing photo..."     │
     │  }                                   │
     │                                      │
     │<──SSE Event: Completed───────────────┤
     │  {                                   │
     │    status: "Completed",              │
     │    message: "Beautiful sunset with  │
     │             mountains in background"│
     │  }                                   │
     │                                      │
```

---

## 🏗️ Modifiche Architetturali

### **A. Builder Pattern nella Scene**

#### **scene.OnClient() Configuration**

```csharp
// ═══════════════════════════════════════════════════════════════
// Definire modelli fortemente tipizzati per arguments
// ═══════════════════════════════════════════════════════════════
public class CapturePhotoArgs
{
    [Description("Image quality (low, medium, high)")]
    public string Quality { get; init; } = "high";
    
    [Description("Maximum image width in pixels")]
    [Range(320, 4096)]
    public int MaxWidth { get; init; } = 1920;
    
    [Description("Use front camera instead of back")]
    public bool FrontCamera { get; init; } = false;
}

// ═══════════════════════════════════════════════════════════════
// Configurare scene con OnClient() e AddTool<T>
// ═══════════════════════════════════════════════════════════════
builder.AddScene("VisionAnalysis", "Analyze photos from camera", scene =>
{
    scene.OnClient(client =>
    {
        // AddTool<T> genera automaticamente JSON Schema per LLM
        client.AddTool<CapturePhotoArgs>("CapturePhoto", 
            description: "Capture photo from device camera",
            timeoutSeconds: 60);
            
        // AddTool semplice senza arguments
        client.AddTool("PlaySound",
            description: "Play a notification sound");
    });
    
    scene.WithActors(actors =>
    {
        actors.AddActor("When user asks to analyze something visual, " +
                       "use CapturePhoto tool to get an image. " +
                       "You can specify quality and maxWidth.");
    });
});
```

---

### **B. Modelli Dati**

#### **ClientInteractionRequest**

```csharp
// src/AI/Rystem.PlayFramework/Domain/Models/ClientInteractionRequest.cs

namespace Rystem.PlayFramework;

/// <summary>
/// Richiesta di esecuzione tool lato client.
/// </summary>
public sealed class ClientInteractionRequest
{
    public required string InteractionId { get; init; }
    public required string ToolName { get; init; }
    
    /// <summary>
    /// Arguments deserializzati dal modello T.
    /// </summary>
    public Dictionary<string, object?>? Arguments { get; init; }
    
    /// <summary>
    /// JSON Schema degli arguments generato da AddTool<T>.
    /// </summary>
    public string? ArgumentsSchema { get; init; }
    
    public string? Description { get; init; }
    public int TimeoutSeconds { get; init; } = 30;
}
```

#### **ClientInteractionResult**

```csharp
// src/AI/Rystem.PlayFramework/Domain/Models/ClientInteractionResult.cs

using Microsoft.Extensions.AI;

namespace Rystem.PlayFramework;

/// <summary>
/// Risultato esecuzione tool lato client.
/// </summary>
public sealed class ClientInteractionResult
{
    public required string InteractionId { get; init; }
    
    /// <summary>
    /// Contenuti multi-modali nativi Microsoft.Extensions.AI.
    /// </summary>
    public IList<AIContent>? Contents { get; init; }
    
    public string? Error { get; init; }
    public DateTimeOffset ExecutedAt { get; init; } = DateTimeOffset.UtcNow;
}
```

#### **SceneContinuation (NUOVO - Stato in Cache)**

```csharp
// src/AI/Rystem.PlayFramework/Domain/Models/SceneContinuation.cs

namespace Rystem.PlayFramework;

/// <summary>
/// Stato della scena salvato in cache per riprendere l'esecuzione.
/// </summary>
internal sealed class SceneContinuation
{
    public required string ConversationKey { get; init; }
    public required string ContinuationToken { get; init; }
    public required string SceneName { get; init; }
    
    /// <summary>
    /// Stato completo del contesto (messages, metadata, etc).
    /// </summary>
    public required SceneExecutionContext Context { get; init; }
    
    /// <summary>
    /// ID dell'interaction attesa.
    /// </summary>
    public required string PendingInteractionId { get; init; }
    
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; init; }
}
```

#### **PlayFrameworkRequest (Esteso)**

```csharp
// src/AI/Rystem.PlayFramework/Api/Models/PlayFrameworkRequest.cs

public sealed class PlayFrameworkRequest
{
    public string? Message { get; init; }
    public List<ContentItem>? Contents { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
    public string? ConversationKey { get; init; }
    
    /// <summary>
    /// Risultati di tool eseguiti lato client.
    /// </summary>
    public List<ClientInteractionResult>? ClientInteractionResults { get; init; }
    
    public SceneRequestSettings? Settings { get; init; }
}
```

#### **AiSceneResponse (Esteso)**

```csharp
// src/AI/Rystem.PlayFramework/Domain/Models/AiSceneResponse.cs

public sealed class AiSceneResponse
{
    public required AiResponseStatus Status { get; init; }
    public string? Message { get; init; }
    public string? ConversationKey { get; init; }
    
    /// <summary>
    /// Token per riprendere esecuzione.
    /// </summary>
    public string? ContinuationToken { get; init; }
    
    /// <summary>
    /// Richiesta tool da eseguire lato client.
    /// </summary>
    public ClientInteractionRequest? ClientInteractionRequest { get; init; }
}
```

#### **AiResponseStatus (Esteso)**

```csharp
// src/AI/Rystem.PlayFramework/Domain/Models/AiResponseStatus.cs

public enum AiResponseStatus
{
    Running,
    Completed,
    Error,
    
    /// <summary>
    /// In attesa che client esegua tool e ritorni risultato.
    /// </summary>
    AwaitingClient
}
```

---

### **C. ClientInteractionBuilder (Server)**

**Path**: `src/AI/Rystem.PlayFramework/Configuration/ClientInteractionBuilder.cs`

```csharp
namespace Rystem.PlayFramework.Configuration;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

public sealed class ClientInteractionBuilder
{
    private readonly List<ClientInteractionDefinition> _definitions = [];
    
    /// <summary>
    /// Registra tool con arguments fortemente tipizzati.
    /// Lo schema JSON di T viene generato automaticamente per l'LLM.
    /// </summary>
    public ClientInteractionBuilder AddTool<T>(
        string toolName,
        string? description = null,
        int timeoutSeconds = 30) where T : class
    {
        var jsonSchema = GenerateJsonSchema<T>();
        
        _definitions.Add(new ClientInteractionDefinition
        {
            ToolName = toolName,
            Description = description,
            TimeoutSeconds = timeoutSeconds,
            ArgumentsType = typeof(T),
            ArgumentsSchema = jsonSchema
        });
        
        return this;
    }
    
    /// <summary>
    /// Registra tool semplice senza arguments.
    /// </summary>
    public ClientInteractionBuilder AddTool(
        string toolName,
        string? description = null,
        int timeoutSeconds = 30)
    {
        _definitions.Add(new ClientInteractionDefinition
        {
            ToolName = toolName,
            Description = description,
            TimeoutSeconds = timeoutSeconds,
            ArgumentsType = null,
            ArgumentsSchema = null
        });
        
        return this;
    }
    
    private static string GenerateJsonSchema<T>() where T : class
    {
        // Usa JsonSchemaExporter (.NET 9+)
        var options = new JsonSerializerOptions();
        var schema = options.GetTypeInfo(typeof(T)).CreateJsonSchema();
        return schema.ToString();
    }
    
    internal IReadOnlyList<ClientInteractionDefinition> Build() 
        => _definitions.AsReadOnly();
}

internal sealed class ClientInteractionDefinition
{
    public required string ToolName { get; init; }
    public string? Description { get; init; }
    public int TimeoutSeconds { get; init; }
    public Type? ArgumentsType { get; init; }
    public string? ArgumentsSchema { get; init; }
}
```

---

### **D. SceneBuilder.OnClient() Extension**

**Path**: `src/AI/Rystem.PlayFramework/Configuration/SceneBuilder.cs`

```csharp
public SceneBuilder OnClient(Action<ClientInteractionBuilder> configure)
{
    var builder = new ClientInteractionBuilder();
    configure(builder);
    
    _clientInteractionDefinitions = builder.Build();
    
    // Flag: questa scene richiede cache
    _requiresCache = true;
    
    return this;
}

public SceneBuilder WithCacheExpiration(TimeSpan expiration)
{
    _cacheExpiration = expiration;
    return this;
}
```

---

### **E. Cache Configuration (OBBLIGATORIA)**

**Path**: `src/AI/Test/Rystem.PlayFramework.Api/Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════
// CACHE OBBLIGATORIA quando si usa OnClient()
// ═══════════════════════════════════════════════════════════════

// OPZIONE 1: In-Memory (sviluppo/testing)
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();

// OPZIONE 2: Redis (produzione)
// builder.Services.AddStackExchangeRedisCache(options =>
// {
//     options.Configuration = builder.Configuration.GetConnectionString("Redis");
// });

// ═══════════════════════════════════════════════════════════════
// PlayFramework valida automaticamente cache se OnClient() usato
// ═══════════════════════════════════════════════════════════════
builder.Services.AddPlayFramework("default", services =>
{
    services.AddScene("VisionAnalysis", "Analyze photos", scene =>
    {
        scene.OnClient(client =>
        {
            client.AddTool<CapturePhotoArgs>("CapturePhoto");
        });
        
        // Configura TTL cache per continuation tokens
        scene.WithCacheExpiration(TimeSpan.FromMinutes(5));
    });
});
```

**Validazione a startup**:
```csharp
// In ServiceCollectionExtensions.cs
if (scenesWithClientTools.Any() && !services.HasDistributedCache())
{
    throw new InvalidOperationException(
        "Scenes with OnClient() require cache configuration. " +
        "Add AddDistributedMemoryCache() or Redis cache."
    );
}
```

---

### **F. SceneManager con Continuation Token**

**Path**: `src/AI/Rystem.PlayFramework/Services/SceneManager.cs`

```csharp
public async IAsyncEnumerable<AiSceneResponse> ExecuteAsync(
    PlayFrameworkRequest request,
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    SceneExecutionContext context;
    
    // ═══════════════════════════════════════════════════════════════
    // Check se questa è una RESUME con ContinuationToken
    // ═══════════════════════════════════════════════════════════════
    if (!string.IsNullOrEmpty(request.ContinuationToken))
    {
        // Carica stato da cache
        var continuation = await _cache.GetAsync<SceneContinuation>(
            request.ContinuationToken,
            cancellationToken
        );
        
        if (continuation == null)
        {
            yield return CreateErrorResponse("Continuation token expired");
            yield break;
        }
        
        context = continuation.Context; // Ripristina stato completo
        
        // Applica risultati client interaction
        if (request.ClientInteractionResults?.Any() == true)
        {
            foreach (var result in request.ClientInteractionResults)
            {
                if (result.Contents?.Any() == true)
                {
                    // Aggiungi AIContent[] alla chat history
                    context.Messages.Add(new ChatMessage(
                        ChatRole.User,
                        result.Contents
                    ));
                }
            }
        }
        
        // Rimuovi token dalla cache
        await _cache.RemoveAsync(request.ContinuationToken, cancellationToken);
    }
    else
    {
        // Nuova esecuzione
        context = await InitializeContextAsync(request, cancellationToken);
    }
    
    // ═══════════════════════════════════════════════════════════════
    // Esecuzione scene - Check per OnClient tools
    // ═══════════════════════════════════════════════════════════════
    foreach await (var step in ExecuteSceneStepsAsync(context, cancellationToken))
    {
        var clientInteraction = _clientInteractionHandler.CheckForClientTool(
            context.Scene,
            step.ToolName
        );
        
        if (clientInteraction != null)
        {
            // ───────────────────────────────────────────────────────
            // Genera continuation token e salva stato
            // ───────────────────────────────────────────────────────
            var continuationToken = Guid.NewGuid().ToString();
            
            var continuation = new SceneContinuation
            {
                ConversationKey = context.ConversationKey,
                ContinuationToken = continuationToken,
                SceneName = context.Scene.Name,
                Context = context,
                PendingInteractionId = clientInteraction.InteractionId,
                ExpiresAt = DateTimeOffset.UtcNow.Add(context.Scene.CacheExpiration)
            };
            
            await _cache.SetAsync(
                continuationToken,
                continuation,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = continuation.ExpiresAt
                },
                cancellationToken
            );
            
            // Yield AwaitingClient e CHIUDI connessione
            yield return new AiSceneResponse
            {
                Status = AiResponseStatus.AwaitingClient,
                ConversationKey = context.ConversationKey,
                ContinuationToken = continuationToken,
                ClientInteractionRequest = clientInteraction
            };
            
            yield break; // Connessione si chiude
        }
        
        yield return step;
    }
}
```

---

### **G. TypeScript Client Loop con Continuation Token**

**Path**: `src/AI/Rystem.PlayFramework.Client/src/rystem/src/engine/PlayFrameworkClient.ts`

```typescript
async *executeStepByStep(
    request: PlayFrameworkRequest,
    signal?: AbortSignal
): AsyncIterableIterator<AiSceneResponse> {
    const settings = this.settings;
    const url = `${settings.baseUrl}/${settings.factoryName}`;
    
    let currentRequest = { ...request };
    let maxIterations = 10;
    let iteration = 0;
    
    while (iteration++ < maxIterations) {
        let awaitingClient: AiSceneResponse | null = null;
        
        // ────────────────────────────────────────────────────────
        // POST e stream SSE (connessione si chiude dopo AwaitingClient)
        // ────────────────────────────────────────────────────────
        for await (const response of this.streamSSE(url, currentRequest, signal)) {
            yield response;
            
            if (response.status === "AwaitingClient" 
                && response.clientInteractionRequest 
                && response.continuationToken) {
                awaitingClient = response;
                // Connessione SSE chiusa - server ha salvato stato
            }
            
            if (response.status === "Completed" || response.status === "Error") {
                return;
            }
        }
        
        if (!awaitingClient?.clientInteractionRequest) {
            break;
        }
        
        // ────────────────────────────────────────────────────────
        // Esegui tool lato client (può impiegare tempo)
        // ────────────────────────────────────────────────────────
        const result = await settings.clientInteractionRegistry.execute(
            awaitingClient.clientInteractionRequest
        );
        
        // ────────────────────────────────────────────────────────
        // NUOVA POST con continuation token per riprendere
        // ────────────────────────────────────────────────────────
        currentRequest = {
            conversationKey: currentRequest.conversationKey,
            continuationToken: awaitingClient.continuationToken!, // ◄── CHIAVE
            clientInteractionResults: [result]
            // NO sceneName/settings - server riprende da cache
        };
    }
    
    if (iteration >= maxIterations) {
        throw new Error("Max client interaction iterations exceeded");
    }
}
```

---

### **H. AIContentConverter (TypeScript)**

**Path**: `src/AI/Rystem.PlayFramework.Client/src/rystem/src/utils/AIContentConverter.ts`

```typescript
// AIContent types (semplificati per client tools)
export interface DataContent {
    data: string;        // Base64
    mediaType: string;   // es. "image/jpeg"
}

export interface TextContent {
    text: string;
}

export type AIContent = DataContent | TextContent;

/**
 * Converte File/Blob browser in AIContent per il server.
 */
export class AIContentConverter {
    /**
     * Converte File/Blob in DataContent con Base64.
     */
    static async fromFile(file: File | Blob): Promise<DataContent> {
        const mediaType = file instanceof File 
            ? file.type 
            : 'application/octet-stream';
        
        const base64 = await this.toBase64(file);
        
        return {
            data: base64,
            mediaType: mediaType
        };
    }
    
    private static toBase64(file: File | Blob): Promise<string> {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            
            reader.onload = () => {
                const result = reader.result as string;
                // Rimuovi prefix "data:image/jpeg;base64," se presente
                const base64 = result.includes(',') 
                    ? result.split(',')[1] 
                    : result;
                resolve(base64);
            };
            
            reader.onerror = reject;
            reader.readAsDataURL(file);
        });
    }
    
    /**
     * Crea TextContent per messaggi testuali.
     */
    static fromText(text: string): TextContent {
        return { text };
    }
}
```

---

## 📂 File Checklist (33 totali)

### **Server-Side (.NET)**

#### ✅ Nuovi File (14)

| # | Path | Descrizione |
|---|------|-------------|
| 1 | `Domain/Models/ClientInteractionRequest.cs` | Request model |
| 2 | `Domain/Models/ClientInteractionResult.cs` | Result model |
| 3 | `Domain/Models/SceneContinuation.cs` | **Stato cache** |
| 4 | `Configuration/ClientInteractionBuilder.cs` | Builder pattern |
| 5 | `Services/ClientInteraction/IClientInteractionHandler.cs` | Handler interface |
| 6 | `Services/ClientInteraction/ClientInteractionHandler.cs` | Handler impl |
| 7 | `Services/ClientInteraction/ClientInteractionDefinition.cs` | Definition model |
| 8 | `Test/Rystem.PlayFramework.Api/Scenes/VisionAnalysisScene.cs` | Demo scene |
| 9 | `Test/Rystem.PlayFramework.Api/Scenes/LocationScene.cs` | Demo scene |

#### 📝 File Esistenti Modificati (8)

| # | Path | Modifiche |
|---|------|-------------|
| 10 | `Domain/Models/AiResponseStatus.cs` | + `AwaitingClient` enum |
| 11 | `Domain/Models/AiSceneResponse.cs` | + `ContinuationToken?`<br>+ `ClientInteractionRequest?` |
| 12 | `Api/Models/PlayFrameworkRequest.cs` | + `ContinuationToken?`<br>+ `ClientInteractionResults?` |
| 13 | `Configuration/SceneBuilder.cs` | + `.OnClient()`<br>+ `.WithCacheExpiration()` |
| 14 | `Services/SceneManager.cs` | + Continuation token logic<br>+ Cache save/load<br>+ Resume with AIContent[] |
| 15 | `Services/Helpers/IStreamingHelper.cs` | + Handle AwaitingClient |
| 16 | `Services/Helpers/StreamingHelper.cs` | + Implementation |
| 17 | `Configuration/ServiceCollectionExtensions.cs` | + Cache validation |

### **Client-Side (TypeScript)**

#### ✅ Nuovi File (7)

| # | Path | Descrizione |
|---|------|-------------|
| 18 | `models/ClientInteractionRequest.ts` | TypeScript interface |
| 19 | `models/ClientInteractionResult.ts` | Result interface |
| 20 | `engine/ClientInteractionRegistry.ts` | Handler registry |
| 21 | `utils/AIContentConverter.ts` | File/Blob converter + AIContent types inline |

#### 📝 File Esistenti Modificati (5)

| # | Path | Modifiche |
|---|------|-------------|
| 23 | `models/AiSceneResponse.ts` | + `AwaitingClient`<br>+ `continuationToken?`<br>+ `clientInteractionRequest?` |
| 22 | `models/AiSceneResponse.ts` | + `AwaitingClient`<br>+ `continuationToken?`<br>+ `clientInteractionRequest?` |
| 23 | `models/PlayFrameworkRequest.ts` | + `continuationToken?`<br>+ `clientInteractionResults?` |
| 24 | `engine/PlayFrameworkClient.ts` | + Loop con continuation token<br>+ Nuova POST |
| 25 | `servicecollection/PlayFrameworkSettings.ts` | + `.registerClientInteraction()` |
| 26 | `index.ts` | + Export nuove API |

### **Demo & Testing**

| # | Path | Modifiche |
|---|------|-------------|
| 27 | `Test/Rystem.PlayFramework.Api/Program.cs` | + **Cache config obbligatoria**<br>+ Scene setup |
| 28 | `Rystem.PlayFramework.Client/src/App.tsx` | + Handler registration<br>+ UI test |

**Totale: 28 file (13
---

## 🎯 Esempi d'Uso Completi

### **Esempio 1: Camera Capture con Continuation Token**

**Server**:
```csharp
public class CapturePhotoArgs
{
    [Description("Image quality")]
    public string Quality { get; init; } = "high";
    
    [Range(320, 4096)]
    public int MaxWidth { get; init; } = 1920;
}

builder.AddScene("VisionAnalysis", "Analyze photos", scene =>
{
    scene.OnClient(client =>
    {
        client.AddTool<CapturePhotoArgs>("CapturePhoto", 
            description: "Capture photo from camera",
            timeoutSeconds: 60);
    });
    
    scene.WithCacheExpiration(TimeSpan.FromMinutes(5));
});
```

**Client**:
```typescript
// Registra handler
settings.registerClientInteraction<CapturePhotoArgs>("CapturePhoto", async (request) => {
    const args = request.arguments!;
    
    const stream = await navigator.mediaDevices.getUserMedia({ 
        video: { width: { ideal: args.maxWidth } }
    });
    
    // ... capture frame ...
    
    const blob = await new Promise<Blob>((resolve) => {
        canvas.toBlob(blob => resolve(blob!), 'image/jpeg', 0.9);
    });
    
    const imageContent = await AIContentConverter.fromFile(blob);
    const textContent = AIContentConverter.fromText("Photo captured");
    
    return {
        contents: [imageContent, textContent]
    };
});

// Uso
for await (const response of client.executeStepByStep({ 
    prompt: "Take a photo and describe it",
    sceneName: "VisionAnalysis"
})) {
    // Automaticamente gestisce AwaitingClient + continuation token
    console.log(response.message);
}
```

---

## 🚀 Piano di Implementazione (5 Fasi)

### **Phase 1: Foundation (Server)**
1. Creare `ClientInteractionRequest`, `ClientInteractionResult`, `SceneContinuation`
2. Creare `ClientInteractionBuilder` con `AddTool<T>()`
3. Estendere `SceneBuilder` con `.OnClient()`
4. Aggiungere `AwaitingClient` status
5. Estendere models con `ContinuationToken`

### **Phase 2: SceneManager + Cache (Server)**
6. Implementare `ClientInteractionHandler`
7. Aggiornare `SceneManager` con cache logic
8. Resume execution con continuation token
9. Cache validation a startup

### **Phase 3: Foundation (Client)**
10. Creare TypeScript models
11. Creare `AIContent` interfaces
12. Creare `ClientInteractio (ClientInteractionRequest, ClientInteractionResult)
11. Creare `ClientInteractionRegistry`
12# **Phase 4: Utilities (Client)**
14. Creare `AIContentConverter`
15. Camera API helper
16. Geolocation helper
13. Creare `AIContentConverter` (File/Blob → DataContent Base64 + TextContent)
14. Camera API helper
15. Geolocation helper
16. Scene VisionAnalysis
19. Scene Location
20. Demo app UI
27. Scene VisionAnalysis
18. Scene Location
19. Demo app UI
20. End-to-end testing
21
## 🔑 Design Decisions

### **1. Continuation Token Flow**

✅ **Connessione NON rimane aperta**
- Server risponde con `AwaitingClient` + `continuationToken` (GUID)
- Chiude connessione SSE immediatamente
- Salva stato completo in cache con TTL

✅ **Client fa nuova POST per riprendere**
- `conversationKey` + `continuationToken` + `clientInteractionResults`
- Server carica stato da cache e riprende esecuzione

✅ **Vantaggi**:
- Non blocca connessioni HTTP/SSE per tempo indefinito
- Client può impiegare secondi/minuti (es. utente scatta foto)
- Scalabile: stato in cache distribuita (Redis)
- Fault tolerant: se client non riprende, TTL expira cache automaticamente

---

### **2. Cache Obbligatoria**

✅ **Se scene usa `.OnClient()`, cache DEVE essere configurata**

```csharp
// Minimo richiesto
builder.Services.AddDistributedMemoryCache();

// O Redis per produzione
builder.Services.AddStackExchangeRedisCache(...);
```

✅ **Validazione a startup**:
```csharp
if (scenesWithClientTools.Any() && !services.HasDistributedCache())
{
    throw new InvalidOperationException(
        "Scenes with OnClient() require cache. " +
        "Add AddDistributedMemoryCache() or Redis."
    );
}
```

✅ **TTL default**: 5 minuti (configurabile per scene)

---

### **3. Type Safety End-to-End**

✅ **AddTool<T>()** genera JSON Schema automaticamente:
```csharp
client.AddTool<CapturePhotoArgs>("CapturePhoto");
// → LLM riceve schema JSON di CapturePhotoArgs
```

✅ **TypeScript client riceve schema**:
```typescript
settings.registerClientInteraction<CapturePhotoArgs>("CapturePhoto", async (request) => {
    const args = request.arguments!; // Typed!
    // args.quality, args.maxWidth sono typed
});
```

---

## ❓ Open Questions

1. **Max file size**: Limite MB per `DataContent` da client? (es. 10MB?)
2. **Cache TTL default**: 5 minuti OK o serve configurabile?
3. **MediaType validation**: Whitelist MediaType permessi?
4. **Timeout handling**: Se client non riprende entro TTL, notificare utente?
5. **JSON Schema library**: JsonSchemaExporter (.NET 9) o alternativa?
6. **State serialization**: Context serializable? Serve [Serializable] decorator?
7. **Redis vs In-Memory**: Forzare Redis in produzione tramite validation?

---

## 📊 Vantaggi Finali

| Feature | Beneficio |
|---------|-----------|
| **Continuation Token** | Non blocca connessioni, scalabile |
| **Cache Distribuita** | Stato persistente, multi-instance support |
| **Connessione Chiusa** | Efficiente, no timeout HTTP/SSE |
| **AIContent Nativo** | Zero conversion, LLM-ready |
| **DataContent + Base64** | Client invia file/immagini come base64, server converte per LLM
| **Builder Pattern** | API dichiarativa e intuitiva |
| **Automatic Resume** | Client loop gestisce tutto automaticamente |
| **Fault Tolerant** | TTL cache gestisce client che non rispondono |

**Pronto per l'implementazione!** 🚀
