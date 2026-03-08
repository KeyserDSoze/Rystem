# Rystem.PlayFramework.Adapters.FoundryLocal

`Rystem.PlayFramework.Adapters.FoundryLocal` registers local-model adapters for `Rystem.PlayFramework` through Microsoft Foundry Local.

It is aimed at development, demos, and local experimentation where you want an OpenAI-compatible endpoint without a cloud provider.

## Installation

```bash
dotnet add package Rystem.PlayFramework.Adapters.FoundryLocal
```

This package includes native/runtime-specific dependencies. Your consuming app should declare an appropriate runtime identifier.

```xml
<PropertyGroup>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
</PropertyGroup>
```

The project itself ships multiple supported runtime identifiers, but your app still needs to target one that matches its deployment environment.

## Prerequisites

Foundry Local itself must be installed.

Examples:

```bash
winget install Microsoft.FoundryLocal
```

```bash
brew tap microsoft/foundrylocal && brew install foundrylocal
```

The SDK also requires the ORT NuGet feed, so keep the feed configuration described by the package in your consuming solution when needed.

## Registering a local chat adapter

```csharp
using Rystem.PlayFramework.Adapters.FoundryLocal;

builder.Services.AddAdapterForFoundryLocal("foundry", settings =>
{
    settings.Model = "phi-4-mini";
    settings.WebServiceUrl = "http://127.0.0.1:5272";
    settings.AppName = "Rystem.PlayFramework";
});

builder.Services.AddPlayFramework("foundry", framework =>
{
    framework.WithChatClient("foundry");
});
```

## What happens on first use

When the adapter is first resolved, it does real work immediately:

1. initialize `FoundryLocalManager`
2. query the local model catalog
3. download the selected model if needed
4. load the model into memory
5. start the local OpenAI-compatible web service
6. create an `IChatClient` against `{WebServiceUrl}/v1`

That means startup or first-request latency can be significant, especially on a fresh machine.

## Chat adapter settings

`FoundryLocalSettings` exposes:

| Property | Meaning |
| --- | --- |
| `Model` | model alias, default `phi-4-mini` |
| `AppName` | app identifier used by Foundry Local |
| `WebServiceUrl` | base URL for the local OpenAI-compatible service |
| `FoundryLogLevel` | Foundry SDK log level |
| `OnDownloadProgress` | optional progress callback |
| `AudioMode` | `None`, `MultiModal`, or `SpeechToText` |
| `SpeechToTextModel` | required when `AudioMode` is `SpeechToText` |

## Voice adapter

The package also registers local `IVoiceAdapter` support.

```csharp
builder.Services.AddVoiceAdapterForFoundryLocal("foundry", settings =>
{
    settings.SttModel = "whisper";
    settings.TtsModel = "tts";
    settings.WebServiceUrl = "http://127.0.0.1:5272";
});

builder.Services.AddPlayFramework("foundry", framework =>
{
    framework.WithVoice("foundry");
});
```

`VoiceAdapterSettings` exposes:

| Property | Meaning |
| --- | --- |
| `SttModel` | local speech-to-text model alias |
| `TtsModel` | local text-to-speech model alias |
| `TtsVoice` | voice name |
| `TtsOutputFormat` | output format |
| `TtsSpeed` | speech speed multiplier |
| `WebServiceUrl` | local service URL |
| `OnDownloadProgress` | optional progress callback |

## Important caveats

### First resolution is heavy

The adapter performs blocking initialization, download, model loading, and service startup on first use. Treat it as a dev/test adapter rather than something that hides infrastructure startup cost.

### The voice adapter assumes Foundry Local is already initialized

The voice path reads `FoundryLocalManager.Instance` directly. In practice, register and initialize the chat adapter before relying on `AddVoiceAdapterForFoundryLocal(...)`.

### Hardware and model availability matter

Available models depend on your platform and hardware. `foundry model list` is the source of truth for what your machine can actually run.

### This package also targets `net10.0`

Like the rest of the PlayFramework area, this package currently targets `net10.0`.

## Grounded by source files

- `src/AI/Rystem.PlayFramework.Adapters.FoundryLocal/ServiceCollectionExtensions.cs`
- `src/AI/Rystem.PlayFramework.Adapters.FoundryLocal/FoundryLocalSettings.cs`
- `src/AI/Rystem.PlayFramework.Adapters.FoundryLocal/VoiceAdapterSettings.cs`

Use this package when you want PlayFramework backed by local Foundry models and you accept the native/runtime setup that comes with it.
