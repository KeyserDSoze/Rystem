# Rystem.PlayFramework.Adapters

`Rystem.PlayFramework.Adapters` is the Azure OpenAI adapter package for `Rystem.PlayFramework`.

It registers named `IChatClient` and `IVoiceAdapter` instances, defaults to the Azure OpenAI Responses API, and adds a file-upload wrapper for non-image, non-audio content.

## Installation

```bash
dotnet add package Rystem.PlayFramework.Adapters
```

## What this package adds

The public registration methods are:

- `AddAdapterForAzureOpenAI(...)`
- `AddVoiceAdapterForAzureOpenAI(...)`

These methods register singleton factory-backed services so they can be referenced by PlayFramework instance name.

## Registering a chat adapter

Unnamed registration:

```csharp
builder.Services.AddAdapterForAzureOpenAI(settings =>
{
    settings.Endpoint = new Uri("https://your-resource.openai.azure.com/");
    settings.ApiKey = builder.Configuration["AzureOpenAI:Key"];
    settings.Deployment = "gpt-4o";
});
```

Named registration, which is the most common pattern with PlayFramework:

```csharp
builder.Services.AddAdapterForAzureOpenAI("default", settings =>
{
    settings.Endpoint = new Uri(builder.Configuration["AzureOpenAI:Endpoint"]!);
    settings.ApiKey = builder.Configuration["AzureOpenAI:Key"]!;
    settings.Deployment = builder.Configuration["AzureOpenAI:Deployment"] ?? "gpt-4o";
});

builder.Services.AddPlayFramework("default", framework =>
{
    framework.WithChatClient("default");
});
```

Managed identity mode:

```csharp
builder.Services.AddAdapterForAzureOpenAI("default", settings =>
{
    settings.Endpoint = new Uri("https://your-resource.openai.azure.com/");
    settings.UseAzureCredential = true;
    settings.Deployment = "gpt-4o";
});
```

## Chat adapter settings

`AdapterSettings` exposes:

| Property | Meaning |
| --- | --- |
| `Endpoint` | Azure OpenAI endpoint |
| `ApiKey` | API key when not using managed identity |
| `UseAzureCredential` | use `DefaultAzureCredential` instead of `ApiKey` |
| `Deployment` | deployment/model name, default `gpt-4o` |
| `UseResponsesApi` | use `GetResponsesClient(...)` instead of chat completions |
| `EnableFileUpload` | wrap the chat client with file-upload behavior |
| `AudioMode` | `None`, `MultiModal`, or `SpeechToText` |
| `SpeechToTextDeployment` | required when `AudioMode` is `SpeechToText` |

## File upload behavior

When both of these are true:

- `UseResponsesApi = true`
- `EnableFileUpload = true`

the adapter wraps the inner `IChatClient` with `MultiModalChatClient`.

That wrapper:

- uploads non-image, non-audio `DataContent` through the Files API
- replaces inline content with `HostedFileContent(file_id)`
- caches uploads by SHA256 hash
- tries remote file reuse by filename if hash lookup misses

Cache priority is:

1. `IDistributedCache`
2. `IMemoryCache`
3. internal in-memory dictionary

## Audio modes

The chat adapter supports three audio paths:

- `AudioMode.None` - audio is passed through as-is
- `AudioMode.MultiModal` - audio is sent inline to a model that natively supports it
- `AudioMode.SpeechToText` - audio is transcribed through Whisper and injected as text

Example:

```csharp
builder.Services.AddAdapterForAzureOpenAI("default", settings =>
{
    settings.Endpoint = new Uri("https://your-resource.openai.azure.com/");
    settings.ApiKey = "...";
    settings.Deployment = "gpt-4o";
    settings.AudioMode = AudioMode.SpeechToText;
    settings.SpeechToTextDeployment = "whisper";
});
```

## Voice adapter

The same package also registers `IVoiceAdapter` for the PlayFramework voice pipeline.

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
    framework.WithVoice("default");
});
```

`VoiceAdapterSettings` exposes:

| Property | Meaning |
| --- | --- |
| `Endpoint` | Azure OpenAI endpoint |
| `ApiKey` | API key when not using managed identity |
| `UseAzureCredential` | use `DefaultAzureCredential` |
| `SttDeployment` | speech-to-text deployment, default `whisper` |
| `TtsDeployment` | text-to-speech deployment, default `tts-1` |
| `TtsVoice` | voice name, default `alloy` |
| `TtsOutputFormat` | output format such as `mp3`, `wav`, `pcm` |
| `TtsSpeed` | speech speed multiplier |

## Cost tracking

Set `CostTracking` on `AdapterSettings` to record input/output token costs per LLM call:

```csharp
builder.Services.AddAdapterForAzureOpenAI("default", settings =>
{
    settings.Endpoint = new Uri(builder.Configuration["AzureOpenAI:Endpoint"]!);
    settings.ApiKey = builder.Configuration["AzureOpenAI:Key"]!;
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
```

`TokenCostSettings` exposes:

| Property | Meaning |
| --- | --- |
| `Enabled` | enable or disable cost tracking, default `true` |
| `Currency` | currency label for display, default `USD` |
| `InputTokenCostPer1K` | cost per 1 000 input tokens |
| `OutputTokenCostPer1K` | cost per 1 000 output tokens |
| `CachedInputTokenCostPer1K` | cost per 1 000 cached input tokens, default `0` |
| `ModelCosts` | `Dictionary<string, ModelCostSettings>` per-model price overrides |
| `ClientCosts` | `Dictionary<string, ClientCostSettings>` per-client price overrides |

When `CostTracking` is set the adapter wraps the underlying `IChatClient` with `CostTrackingChatClient` — a `DelegatingChatClient` that reads `response.Usage` after every LLM call and embeds a `CostCalculation` into `AdditionalProperties`. `ChatClientManager` picks that value up passively and surfaces it as `AiSceneResponse.Cost` (per-call) and `AiSceneResponse.TotalCost` (cumulative across the full request).

> **Critical**: you must call `.WithChatClient("name")` in `AddPlayFramework` with the **same factory name** used for the adapter. Without it the framework falls back to a direct `IChatClient` resolution path that does not see the cost wrapper and reports zero for all costs.

See [Example: cost tracking](https://github.com/KeyserDSoze/Rystem/tree/master/src/AI/Rystem.PlayFramework#example-cost-tracking) in the PlayFramework README for budget enforcement, response fields, and per-model / per-client pricing.

---

## Important caveats

### The streaming path blocks during file preprocessing

`GetStreamingResponseAsync(...)` preprocesses uploadable files with a sync wait via `GetAwaiter().GetResult()`. It works, but it is not the cleanest async path.

### Remote file reuse is filename-based after hash miss

If the local content-hash cache misses, the wrapper checks Azure's existing file list by filename. That is convenient, but it is not a content-identity guarantee.

### Images and audio are not part of the file-upload wrapper

The Files API wrapper only handles non-image, non-audio binary content. Images stay inline, and audio is handled through the adapter's audio-mode logic.

### Factory names should match your PlayFramework names

If you register the adapter as `"default"`, your PlayFramework builder should typically reference `WithChatClient("default")` and `WithVoice("default")`.

## Grounded by source files

- `src/AI/Rystem.PlayFramework.Adapters/ServiceCollectionExtensions.cs`
- `src/AI/Rystem.PlayFramework.Adapters/MultiModalChatClient.cs`
- `src/AI/Test/Rystem.PlayFramework.Api/Program.cs`

Use this package when your PlayFramework backend should run on Azure OpenAI with optional file upload, speech-to-text, and voice output.
