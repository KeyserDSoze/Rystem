# Client-Side Interaction Architecture
## Architettura OnClient() per Tool Eseguiti Lato Client con Type Safety End-to-End

---

## 📋 Executive Summary

Feature che permette al PlayFramework di delegare l'esecuzione di specifici tool al client (browser/mobile app) tramite builder pattern **`scene.OnClient()`**, con supporto nativo per **Microsoft.Extensions.AI** types e **type safety end-to-end**:
- ✅ **AddTool<T>()** generico con modelli fortemente tipizzati
- ✅ **JSON Schema automatico** dall'LLM generato da type `T`
- ✅ **DataContent nativo** (immagini, PDF, audio, video)
- ✅ **UriContent** (riferimenti esterni)
- ✅ **TextContent** (testo)
- ✅ **Liste multiple di AIContent[]**
- ✅ **Type safety C# ↔ TypeScript**

---

## 🔄 Flusso di Esecuzione Completo

```
┌─────────────────────────────────────────────────────────────────────────┐
│ FASE 1: Client Invia Richiesta Iniziale                                │
└─────────────────────────────────────────────────────────────────────────┘

Client (Browser)                    Server (PlayFramework)
     │                                      │
     ├──POST /api/ai/default───────────────>│
     │  {                                   │
     │    prompt: "Take a photo",           │
     │    conversationKey: "conv-abc123",   │
     │    sceneName: "VisionAnalysis"       │
     │  }                                   │
     │                                      │
     │                              ┌───────┴────────┐
     │                              │ SceneManager   │
     │                              │ - Esegue scene │
     │                              │ - LLM identifica│
     │                              │   necessità di │
     │                              │   CapturePhoto │
     │                              └───────┬────────┘
     │                                      │
     │<──SSE Event: AwaitingClient──────────┤
     │  {                                   │
     │    status: "AwaitingClient",         │
     │    conversationKey: "conv-abc123",   │
     │    clientInteraction: {              │
     │      interactionId: "guid-123",      │
     │      toolName: "CapturePhoto",       │
     │      arguments: {                    │ ◄── Dall'LLM validati da schema
     │        quality: "high",              │
     │        maxWidth: 1920,               │
     │        frontCamera: false            │
     │      },                              │
     │      argumentsSchema: "{ ... }"      │ ◄── JSON Schema da AddTool<T>
     │    }                                 │
     │  }                                   │

┌─────────────────────────────────────────────────────────────────────────┐
│ FASE 2: Client Esegue Tool (Multi-Modale con Type Safety)              │
└─────────────────────────────────────────────────────────────────────────┘

Client (Browser)
     │
     ├──navigator.mediaDevices.getUserMedia()
     │  │  - maxWidth: 1920 (dall'LLM)
     │  │  - frontCamera: false (dall'LLM)
     │  │
     │  └──> Blob (image/jpeg, 2.5MB)
     │        │
     │        └──> Converti a DataContent (Base64)

┌─────────────────────────────────────────────────────────────────────────┐
│ FASE 3: Client Invia AIContent[] Nativo al Server                      │
└─────────────────────────────────────────────────────────────────────────┘

Client                              Server
     │                                      │
     ├──POST /api/ai/default───────────────>│
     │  {                                   │
     │    conversationKey: "conv-abc123",   │
     │    clientInteractionResults: [       │
     │      {                               │
     │        interactionId: "guid-123",    │
     │        contents: [                   │ ◄── AIContent[] nativo
     │          {                           │
     │            "$type": "data",          │ ◄── DataContent
     │            "data": "iVBORw0K...",    │ ◄── Base64 BinaryData
     │            "mediaType": "image/jpeg" │
     │          },                          │
     │          {                           │
     │            "$type": "text",          │ ◄── TextContent
     │            "text": "Photo captured" │
     │          }                           │
     │        ]                             │
     │      }                               │
     │    ]                                 │
     │  }                                   │
     │                                      │
     │                              ┌───────┴────────┐
     │                              │ SceneManager   │
     │                              │ - Riprende     │
     │                              │ - Aggiunge     │
     │                              │   AIContent[]  │
     │                              │   a chat       │
     │                              └───────┬────────┘
     │                                      │
     │<──SSE Event: Completed───────────────┤
     │  {                                   │
     │    status: "Completed",              │
     │    message: "I can see mountains..." │
     │  }                                   │
```

---

## 🏗️ Architettura Dettagliata

### **A. Builder Pattern con Type Safety**

#### **1. Modelli Fortemente Tipizzati (Server)**

```csharp
// Definire modelli con Data Annotations per JSON Schema
public class CapturePhotoArgs
{
    [Description("Image quality: low, medium, or high")]
    public string Quality { get; init; } = "high";
    
    [Description("Maximum image width in pixels")]
    [Range(320, 4096)]
    public int MaxWidth { get; init; } = 1920;
    
    [Description("Use front-facing camera instead of rear")]
    public bool FrontCamera { get; init; } = false;
}

public class SelectFilesArgs
{
    [Description("Allowed file MIME types")]
    public string[] AllowedTypes { get; init; } = ["*/*"];
    
    [Description("Maximum number of files")]
    [Range(1, 10)]
    public int MaxFiles { get; init; } = 5;
}
```

#### **2. Scene Configuration con AddTool<T>**

```csharp
builder.AddScene("VisionAnalysis", "Analyze photos", scene =>
{
    scene.OnClient(client =>
    {
        // ═══════════════════════════════════════════════════════════
        // AddTool<T>: generazione automatica JSON Schema per LLM
        // ═══════════════════════════════════════════════════════════
        client.AddTool<CapturePhotoArgs>("CapturePhoto", 
            description: "Capture photo from device camera",
            timeoutSeconds: 60);
            
        client.AddTool<SelectFilesArgs>("SelectFiles",
            description: "Let user select files");
            
        // Tool semplici senza arguments
        client.AddTool("PlaySound",
            description: "Play notification");
    });
    
    scene.WithActors(actors =>
    {
        actors.AddActor("Use CapturePhoto to get images. " +
                       "Specify quality (low/medium/high), maxWidth, frontCamera.");
    });
});
```

---

## 📁 File da Modificare/Creare

### **FASE 1: Server-Side (.NET)**

#### ✅ Nuovi File

| # | Path | Descrizione |
|---|------|-------------|
| 1 | `ClientInteractionRequest.cs` | Request model |
| 2 | `ClientInteractionResult.cs` | Result con AIContent[] |
| 3 | `ClientInteractionBuilder.cs` | Builder con AddTool<T>() |
| 4 | `ClientInteractionDefinition.cs` | Definition interna |
| 5 | `IClientInteractionHandler.cs` | Handler interface |
| 6 | `ClientInteractionHandler.cs` | Handler implementation |

#### 📝 File Esistenti da Modificare

| # | Path | Modifiche |
|---|------|-----------|
| 7 | `AiResponseStatus.cs` | + `AwaitingClient` enum |
| 8 | `AiSceneResponse.cs` | + `ClientInteractionRequest?` property |
| 9 | `PlayFrameworkRequest.cs` | + `ClientInteractionResults?` property |
| 10 | `SceneBuilder.cs` | + `.OnClient()` method |
| 11 | `SceneManager.cs` | + Logica intercettazione e resume |
| 12 | `StreamingHelper.cs` | + Gestione AwaitingClient |

---

### **FASE 2: Client-Side (TypeScript)**

#### ✅ Nuovi File

| # | Path | Descrizione |
|---|------|-------------|
| 13 | `ClientInteractionRequest.ts` | TypeScript interfaces |
| 14 | `ClientInteractionResult.ts` | Result interfaces |
| 15 | `AIContent.ts` | DataContent, TextContent, UriContent |
| 16 | `ClientInteractionRegistry.ts` | Registry handlers |
| 17 | `AIContentConverter.ts` | File→DataContent helper |

#### 📝 File Esistenti da Modificare

| # | Path | Modifiche |
|---|------|-----------|
| 18 | `AiSceneResponse.ts` | + AwaitingClient status |
| 19 | `PlayFrameworkRequest.ts` | + clientInteractionResults property |
| 20 | `PlayFrameworkClient.ts` | + Loop automatico |
| 21 | `PlayFrameworkSettings.ts` | + registerClientInteraction<T>() |
| 22 | `index.ts` | + Export nuove API |

---

### **FASE 3: Demo & Testing**

| # | Path | Descrizione |
|---|------|-------------|
| 23 | `VisionAnalysisScene.cs` | Demo scene camera |
| 24 | `Program.cs` | Configurazione |
| 25 | `App.tsx` | UI + registrazione handlers |

**Totale: 25 file (12 nuovi + 13 modificati)**

---

## 🔧 Componenti Chiave

### **1. ClientInteractionBuilder (Server)**

**Path**: `src/AI/Rystem.PlayFramework/Configuration/ClientInteractionBuilder.cs`

```csharp
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

public sealed class ClientInteractionBuilder
{
    private readonly List<ClientInteractionDefinition> _definitions = [];
    
    /// <summary>
    /// Registra tool con arguments fortemente tipizzati.
    /// JSON Schema generato automaticamente da T per l'LLM.
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
            TimeoutSeconds = timeoutSeconds
        });
        
        return this;
    }
    
    private static string GenerateJsonSchema<T>() where T : class
    {
        var options = new JsonSerializerOptions();
        var schema = options.GetTypeInfo(typeof(T)).CreateJsonSchema();
        return schema.ToString();
    }
    
    internal IReadOnlyList<ClientInteractionDefinition> Build() 
        => _definitions.AsReadOnly();
}
```

---

### **2. AIContentConverter (Client)**

**Path**: `src/AI/Rystem.PlayFramework.Client/src/rystem/src/utils/AIContentConverter.ts`

```typescript
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
            $type: 'data',
            data: base64,
            mediaType: mediaType
        };
    }
    
    private static toBase64(file: File | Blob): Promise<string> {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = () => {
                const result = reader.result as string;
                const base64 = result.includes(',') 
                    ? result.split(',')[1] 
                    : result;
                resolve(base64);
            };
            reader.onerror = reject;
            reader.readAsDataURL(file);
        });
    }
    
    static fromText(text: string): TextContent {
        return { $type: 'text', text };
    }
    
    static fromUrl(url: string, mediaType?: string): UriContent {
        return { $type: 'uri', uri: url, mediaType };
    }
}
```

---

## 🎯 Esempi Completi

### **Esempio 1: Camera + Vision Analysis**

**Server (C#)**:
```csharp
public class CapturePhotoArgs
{
    [Description("Image quality")]
    public string Quality { get; init; } = "high";
    
    [Range(320, 4096)]
    public int MaxWidth { get; init; } = 1920;
    
    public bool FrontCamera { get; init; } = false;
}

scene.OnClient(client =>
{
    client.AddTool<CapturePhotoArgs>("CapturePhoto", 
        description: "Capture photo from camera",
        timeoutSeconds: 60);
});
```

**Client (TypeScript)**:
```typescript
interface CapturePhotoArgs {
    quality?: 'low' | 'medium' | 'high';
    maxWidth?: number;
    frontCamera?: boolean;
}

settings.registerClientInteraction<CapturePhotoArgs>(
    "CapturePhoto", 
    async (request) => {
        const args = request.arguments!;
        
        const stream = await navigator.mediaDevices.getUserMedia({ 
            video: { 
                width: { ideal: args.maxWidth || 1920 },
                facingMode: args.frontCamera ? 'user' : 'environment'
            } 
        });
        
        // Capture + convert to blob
        const blob = await captureFrame(stream, args.quality);
        const imageContent = await AIContentConverter.fromFile(blob);
        
        return { contents: [imageContent] };
    }
);
```

---

### **Esempio 2: File Upload con Validazione**

**Server:**
```csharp
public class SelectFilesArgs
{
    public string[] AllowedTypes { get; init; } = ["*/*"];
    
    [Range(1, 10)]
    public int MaxFiles { get; init; } = 5;
    
    public int MaxSizeMB { get; init; } = 10;
}

client.AddTool<SelectFilesArgs>("SelectFiles");
```

**Client:**
```typescript
interface SelectFilesArgs {
    allowedTypes?: string[];
    maxFiles?: number;
    maxSizeMB?: number;
}

settings.registerClientInteraction<SelectFilesArgs>(
    "SelectFiles",
    async (request) => {
        const args = request.arguments!;
        const files = await showFilePicker(args);
        
        // Validazione
        const maxBytes = args.maxSizeMB! * 1024 * 1024;
        const valid = files.filter(f => f.size <= maxBytes)
                           .slice(0, args.maxFiles);
        
        const contents = await Promise.all(
            valid.map(f => AIContentConverter.fromFile(f))
        );
        
        return { contents };
    }
);
```

---

## ✅ Vantaggi Architettura

| Vantaggio | Descrizione |
|-----------|-------------|
| **End-to-End Type Safety** | `AddTool<T>()` con C# + TypeScript matching |
| **Auto JSON Schema** | LLM riceve schema da `T` automaticamente |
| **LLM Aware** | AI conosce parametri esatti da inviare |
| **Native AIContent** | Microsoft.Extensions.AI senza conversioni |
| **Validation** | Data Annotations server-side |
| **Seamless Loop** | Client gestisce resume automaticamente |
| **Zero Boilerplate** | Builder pattern dichiarativo |

---

## 📝 Decisioni Pre-Implementation

1. ✅ **JSON Schema Generator**: Usare JsonSchemaExporter (.NET 9+)?
2. ✅ **Validation**: Data Annotations + FluentValidation?
3. ✅ **Type Mismatch**: Fallback se client ritorna dati non conformi?
4. ✅ **Storage**: In-memory, Redis, o database per ConversationKey?
5. ✅ **Timeout**: Cosa fare se client non risponde?
6. ✅ **Max Size**: Limite DataContent in MB?

**Pronto per l'OK per procedere! 🚀**
