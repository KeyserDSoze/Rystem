# Rystem.PlayFramework.Adapters

Azure OpenAI adapter for [Rystem.PlayFramework](https://github.com/KeyserDSoze/Rystem).  
Supports **Responses API**, automatic **file upload** via Files API, and **SHA256-based multi-level caching**.

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

## Related Packages

- **[Rystem.PlayFramework.Adapters.FoundryLocal](https://www.nuget.org/packages/Rystem.PlayFramework.Adapters.FoundryLocal)** — Run AI models locally for dev/testing using Microsoft.AI.Foundry.Local SDK.

## Dependencies

- [Rystem.PlayFramework](https://www.nuget.org/packages/Rystem.PlayFramework)
- [Azure.AI.OpenAI](https://www.nuget.org/packages/Azure.AI.OpenAI) (2.8.0-beta.1)
- [Microsoft.Extensions.AI.OpenAI](https://www.nuget.org/packages/Microsoft.Extensions.AI.OpenAI) (10.3.0)
- [Azure.Identity](https://www.nuget.org/packages/Azure.Identity)

## License

MIT — see [LICENSE](https://github.com/KeyserDSoze/Rystem/blob/master/LICENSE.txt)
