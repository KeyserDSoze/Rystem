# Rystem.PlayFramework.Adapters

Azure OpenAI adapter for [Rystem.PlayFramework](https://github.com/KeyserDSoze/Rystem).  
Supports **Responses API**, automatic **file upload** via Files API, **SHA256-based multi-level caching**, and **Voice Pipeline** (Whisper STT + TTS-1).

## Installation

```bash
dotnet add package Rystem.PlayFramework.Adapters
```

---

## Azure OpenAI

### Basic Usage (default registration)

```csharp
builder.Services.AddAdapterForAzureOpenAI(settings =>
{
    settings.Endpoint = new Uri("https://YOUR-RESOURCE.openai.azure.com/");
    settings.ApiKey = "YOUR-API-KEY";
    settings.Deployment = "gpt-4o";
});
```

### Named Factory (matches PlayFramework name)

```csharp
builder.Services.AddAdapterForAzureOpenAI("default", settings =>
{
    settings.Endpoint = new Uri("https://YOUR-RESOURCE.openai.azure.com/");
    settings.ApiKey = "YOUR-API-KEY";
    settings.Deployment = "gpt-4o";
});
```

### Azure Managed Identity

```csharp
builder.Services.AddAdapterForAzureOpenAI("default", settings =>
{
    settings.Endpoint = new Uri("https://YOUR-RESOURCE.openai.azure.com/");
    settings.UseAzureCredential = true; // Uses DefaultAzureCredential
    settings.Deployment = "gpt-4o";
});
```

### Configuration

| Property | Type | Default | Description |
|---|---|---|---|
| `Endpoint` | `Uri?` | — | Azure OpenAI endpoint URI (**required**) |
| `ApiKey` | `string?` | — | API key (required unless `UseAzureCredential = true`) |
| `UseAzureCredential` | `bool` | `false` | Use `DefaultAzureCredential` (Managed Identity / Azure CLI) |
| `Deployment` | `string` | `"gpt-4o"` | Model deployment name |
| `UseResponsesApi` | `bool` | `true` | Use the Responses API (supports `input_file`, `input_image`) |
| `EnableFileUpload` | `bool` | `true` | Automatically upload non-image files via the Files API |

### File Upload

When `UseResponsesApi` and `EnableFileUpload` are both `true` (the default), non-image files (PDF, DOCX, CSV, etc.) are automatically:

1. **Uploaded** to Azure OpenAI via the Files API
2. **Referenced** by `file_id` in the Responses API request
3. **Cached** to avoid re-uploading the same content (SHA256 hash-based deduplication)

#### Cache Strategy (first match wins)

| Priority | Provider | Registration |
|---|---|---|
| 1 | `IDistributedCache` | `services.AddStackExchangeRedisCache(...)` or similar |
| 2 | `IMemoryCache` | `services.AddMemoryCache()` |
| 3 | In-memory `Dictionary` | Automatic fallback (no registration needed) |

---

## Voice Adapter (Whisper + TTS-1)

The adapter package also provides an `IVoiceAdapter` implementation for the PlayFramework **voice pipeline** (STT → LLM → TTS). It uses **Whisper** for speech-to-text and **TTS-1** for text-to-speech.

### Basic Usage

```csharp
builder.Services.AddVoiceAdapterForAzureOpenAI(settings =>
{
    settings.Endpoint = new Uri("https://YOUR-RESOURCE.openai.azure.com/");
    settings.ApiKey = "YOUR-API-KEY";
    settings.SttDeployment = "whisper";   // Whisper model deployment name
    settings.TtsDeployment = "tts-1";     // TTS model deployment name
});
```

### Named Factory

```csharp
builder.Services.AddVoiceAdapterForAzureOpenAI("default", settings =>
{
    settings.Endpoint = new Uri("https://YOUR-RESOURCE.openai.azure.com/");
    settings.ApiKey = "YOUR-API-KEY";
    settings.SttDeployment = "whisper";
    settings.TtsDeployment = "tts-1";
    settings.TtsVoice = "nova";           // alloy, echo, fable, onyx, nova, shimmer
});

// Wire it up in PlayFramework
builder.Services.AddPlayFramework("default", pb => pb
    .WithChatClient("gpt-4o")
    .WithVoice("default")  // matches the voice adapter factory name
    .AddScene("chat", "Conversation", scene => { }));
```

### Azure Managed Identity

```csharp
builder.Services.AddVoiceAdapterForAzureOpenAI("default", settings =>
{
    settings.Endpoint = new Uri("https://YOUR-RESOURCE.openai.azure.com/");
    settings.UseAzureCredential = true;
    settings.SttDeployment = "whisper";
    settings.TtsDeployment = "tts-1";
});
```

### Configuration

| Property | Type | Default | Description |
|---|---|---|---|
| `Endpoint` | `Uri?` | — | Azure OpenAI endpoint URI (**required**) |
| `ApiKey` | `string?` | — | API key (required unless `UseAzureCredential = true`) |
| `UseAzureCredential` | `bool` | `false` | Use `DefaultAzureCredential` (Managed Identity / Azure CLI) |
| `SttDeployment` | `string` | `"whisper"` | Whisper model deployment name for STT |
| `TtsDeployment` | `string` | `"tts-1"` | TTS model deployment name |
| `TtsVoice` | `string` | `"alloy"` | Voice: `alloy`, `echo`, `fable`, `onyx`, `nova`, `shimmer` |
| `TtsOutputFormat` | `string` | `"mp3"` | Output format: `mp3`, `opus`, `aac`, `flac`, `wav`, `pcm` |
| `TtsSpeed` | `float` | `1.0` | Speech speed multiplier (0.25 – 4.0) |

### How It Works

1. **STT**: Audio bytes are sent to the Whisper deployment → returns transcribed text + detected language
2. **TTS**: Text is sent to the TTS-1 deployment with the configured voice → returns audio bytes

The PlayFramework voice pipeline orchestrates the complete flow:
```
User Audio → Whisper (STT) → PlayFramework Scenes → Sentence Accumulator → TTS-1 → Audio Chunks (SSE)
```

When voice mode is active, a **voice-style system instruction** is automatically injected to make the LLM respond conversationally (no tables, no markdown). This is fully customizable via `VoiceSettings.VoiceStyleInstruction`.

See the [PlayFramework README](https://github.com/KeyserDSoze/Rystem/tree/master/src/AI/Rystem.PlayFramework#%EF%B8%8F-voice-pipeline-stt--llm--tts) for full voice pipeline configuration.

---

## 💰 Cost Tracking

Set `CostTracking` on `AdapterSettings` to record input/output token costs per LLM call and surface them on every `AiSceneResponse`.

```csharp
builder.Services.AddAdapterForAzureOpenAI("default", settings =>
{
    settings.Endpoint = new Uri("https://YOUR-RESOURCE.openai.azure.com/");
    settings.ApiKey = "YOUR-API-KEY";
    settings.Deployment = "gpt-4o";
    settings.CostTracking = new TokenCostSettings
    {
        Enabled = true,
        Currency = "USD",
        InputTokenCostPer1K = 0.005m,
        OutputTokenCostPer1K = 0.015m,
        CachedInputTokenCostPer1K = 0.0025m, // optional: cached input is usually ~50% of regular
    };
});

// PlayFramework builder — factory name MUST match the adapter name
builder.Services.AddPlayFramework("default", pb => pb
    .WithChatClient("default")  // ← must match adapter name, required for cost tracking to work
    .AddScene("Chat", "General conversation.", sceneBuilder => { }));
```

### TokenCostSettings reference

| Property | Type | Default | Description |
|---|---|---|---|
| `Enabled` | `bool` | `true` | Enable or disable cost tracking |
| `Currency` | `string` | `"USD"` | Currency label for display |
| `InputTokenCostPer1K` | `decimal` | `0` | Cost per 1 000 input tokens |
| `OutputTokenCostPer1K` | `decimal` | `0` | Cost per 1 000 output tokens |
| `CachedInputTokenCostPer1K` | `decimal` | `0` | Cost per 1 000 cached input tokens |
| `ModelCosts` | `Dictionary<string, ModelCostSettings>` | empty | Per-model price overrides |
| `ClientCosts` | `Dictionary<string, ClientCostSettings>` | empty | Per-client price overrides |

### How it works

When `CostTracking` is set the adapter wraps the underlying `IChatClient` with `CostTrackingChatClient` — a `DelegatingChatClient` that reads `response.Usage` after every LLM call and embeds a `CostCalculation` in `AdditionalProperties`. The framework reads that value passively and surfaces it on:

- `AiSceneResponse.Cost` — cost for the current LLM call
- `AiSceneResponse.TotalCost` — cumulative cost across all calls in the request

> ⚠️ **Critical**: `.WithChatClient("name")` in the PlayFramework builder must use the **same factory name** as the adapter registration. Without it the framework skips the adapter's cost wrapper and reports zero for all costs.

---

## Related Packages

- **[Rystem.PlayFramework.Adapters.FoundryLocal](https://www.nuget.org/packages/Rystem.PlayFramework.Adapters.FoundryLocal)** — Run AI models locally for dev/testing using Microsoft.AI.Foundry.Local SDK.

## Dependencies

- [Rystem.PlayFramework](https://www.nuget.org/packages/Rystem.PlayFramework)
- [Azure.AI.OpenAI](https://www.nuget.org/packages/Azure.AI.OpenAI) (2.8.0-beta.1)
- [Microsoft.Extensions.AI.OpenAI](https://www.nuget.org/packages/Microsoft.Extensions.AI.OpenAI) (10.3.0)
- [Azure.Identity](https://www.nuget.org/packages/Azure.Identity)

## License

MIT — see [LICENSE](https://github.com/KeyserDSoze/Rystem/blob/master/LICENSE.txt)
