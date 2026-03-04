# Rystem.PlayFramework.Adapters.FoundryLocal

Foundry Local adapter for [Rystem.PlayFramework](https://github.com/KeyserDSoze/Rystem).  
Run AI models **locally** on your device for development and testing — no cloud, no API keys.  
Uses the **[Microsoft.AI.Foundry.Local](https://github.com/microsoft/Foundry-Local)** SDK to automatically download, load, and start a local OpenAI-compatible web service.

## Installation

```bash
dotnet add package Rystem.PlayFramework.Adapters.FoundryLocal
```

> **Note:** This package includes native ONNX runtime dependencies. Consuming projects **must** specify a `RuntimeIdentifier`:
>
> ```xml
> <PropertyGroup>
>   <RuntimeIdentifier>win-x64</RuntimeIdentifier>
>   <!-- Or: linux-x64, osx-arm64, etc. -->
> </PropertyGroup>
> ```

### NuGet Feed Configuration

The Foundry Local SDK requires the ORT feed. Add a `nuget.config` in your project:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="ORT" value="https://aiinfra.pkgs.visualstudio.com/PublicPackages/_packaging/ORT/nuget/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
    <packageSource key="ORT">
      <package pattern="*Foundry*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
```

## Prerequisites

```bash
# Install Foundry Local
winget install Microsoft.FoundryLocal   # Windows
brew tap microsoft/foundrylocal && brew install foundrylocal  # macOS
```

## Usage

```csharp
using Rystem.PlayFramework.Adapters.FoundryLocal;

builder.Services.AddAdapterForFoundryLocal("default", settings =>
{
    settings.Model = "phi-4-mini";
    // settings.WebServiceUrl = "http://127.0.0.1:5272";  // default
    // settings.AppName = "Rystem.PlayFramework";          // default
});
```

The adapter automatically:
1. Initializes **FoundryLocalManager**
2. **Downloads** the model (skips if already cached)
3. **Loads** the model into memory
4. **Starts** the OpenAI-compatible web service
5. Creates an `OpenAIClient` pointed at the local endpoint

## Configuration

| Property | Type | Default | Description |
|---|---|---|---|
| `Model` | `string` | `"phi-4-mini"` | Model alias (run `foundry model list` to see available) |
| `WebServiceUrl` | `string` | `"http://127.0.0.1:5272"` | URL where the local web service listens |
| `AppName` | `string` | `"Rystem.PlayFramework"` | Application name for Foundry identification |
| `FoundryLogLevel` | `LogLevel` | `Information` | Foundry Local SDK log level |
| `OnDownloadProgress` | `Action<float>?` | `null` | Optional callback for model download progress (0–100%) |

## Available Models

Run `foundry model list` to see models for your hardware. Common aliases:

| Alias | Size | Notes |
|---|---|---|
| `phi-4-mini` | ~2.4 GB | Good for general testing |
| `qwen2.5-0.5b` | ~0.5 GB | Lightweight, fast |
| `gpt-oss-20b` | ~20 GB | Larger, needs 16 GB VRAM (CUDA) |

## Dependencies

- [Rystem.PlayFramework](https://www.nuget.org/packages/Rystem.PlayFramework)
- [Microsoft.AI.Foundry.Local](https://github.com/microsoft/Foundry-Local) (0.8.2.1)
- [Microsoft.Extensions.AI.OpenAI](https://www.nuget.org/packages/Microsoft.Extensions.AI.OpenAI) (10.3.0)

## Related Packages

- **[Rystem.PlayFramework.Adapters](https://www.nuget.org/packages/Rystem.PlayFramework.Adapters)** — Azure OpenAI adapter (Responses API, file upload, caching). Lightweight, no native dependencies.

## License

MIT — see [LICENSE](https://github.com/KeyserDSoze/Rystem/blob/master/LICENSE.txt)
