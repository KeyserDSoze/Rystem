# Rystem.PlayFramework.Api Sample

`src/AI/Test/Rystem.PlayFramework.Api` is the local ASP.NET Core sample backend used to exercise the PlayFramework HTTP API and the TypeScript client workspace.

It is not the library package itself. It is a runnable sample that wires:

- `Rystem.PlayFramework`
- `Rystem.PlayFramework.Adapters`
- optional `Rystem.PlayFramework.Adapters.FoundryLocal`
- in-memory cache and repository persistence for the `default` factory
- HTTP endpoints through `MapPlayFramework(...)`

## What the sample currently does

The sample registers two PlayFramework factories in `Program.cs`:

- `default` - the main sample backend, with cache, repository persistence, planning, telemetry, client tools, calculator tools, shape tools, and voice endpoints
- `foundry` - a second factory intended for Foundry Local experiments

The HTTP surface is mapped under:

- `POST /api/ai/default`
- `POST /api/ai/default/streaming`
- `GET /api/ai/default/conversations`
- `GET /api/ai/default/conversations/{conversationKey}`
- `DELETE /api/ai/default/conversations/{conversationKey}`
- `PATCH /api/ai/default/conversations/{conversationKey}/visibility`
- `POST /api/ai/default/voice`
- `POST /api/ai/foundry`
- `POST /api/ai/foundry/streaming`

It also exposes:

- `GET /health`
- OpenAPI + Scalar in development

## Run the sample

From this folder:

```bash
dotnet run
```

The launch settings currently use:

```text
https://localhost:7248
http://localhost:5158
```

## Optional Azure OpenAI configuration

If you want the `default` factory to talk to Azure OpenAI, configure:

```bash
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://<your-resource>.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:Key" "<your-api-key>"
dotnet user-secrets set "AzureOpenAI:Deployment" "gpt-4o"
```

Optional voice settings:

```bash
dotnet user-secrets set "AzureOpenAI:Voice:SttDeployment" "whisper"
dotnet user-secrets set "AzureOpenAI:Voice:TtsDeployment" "tts-1"
dotnet user-secrets set "AzureOpenAI:Voice:TtsVoice" "alloy"
dotnet user-secrets set "AzureOpenAI:Voice:TtsOutputFormat" "mp3"
dotnet user-secrets set "AzureOpenAI:Voice:TtsSpeed" "1.0"
```

Optional cost tracking settings:

```bash
dotnet user-secrets set "AzureOpenAI:CostTracking:Currency" "USD"
dotnet user-secrets set "AzureOpenAI:CostTracking:InputTokenCostPer1K" "0.005"
dotnet user-secrets set "AzureOpenAI:CostTracking:OutputTokenCostPer1K" "0.015"
```

If those settings are missing, the sample falls back to a direct `MockChatClient` registration so the API can still be exercised locally.

## Frontend pairing

The sample frontend workspace lives in `src/AI/Rystem.PlayFramework.Client`.

Run it with:

```bash
npm install
npm run dev
```

That workspace currently runs on the port configured in `src/AI/Rystem.PlayFramework.Client/vite.config.ts`:

```text
http://localhost:3000
```

## Request shape

The sample uses the real `PlayFrameworkRequest` contract, so the payload is based on `message`, top-level `conversationKey`, top-level `clientInteractionResults`, and nested `settings`.

Example step-by-step request:

```json
{
  "message": "Calculate 5 * 7",
  "settings": {
    "executionMode": "Scene",
    "sceneName": "Calculator",
    "forcedTools": [
      {
        "sceneName": "Calculator",
        "toolName": "Add",
        "sourceType": "Service",
        "sourceName": "ICalculatorService",
        "memberName": "Add"
      }
    ]
  }
}
```

Token-streaming uses the same body shape against:

```text
POST /api/ai/default/streaming
```

The sample also exposes discovery metadata for the frontend:

```text
GET /api/ai/default/discovery
```

Use that endpoint to inspect normalized scene names plus DI/client/MCP tool metadata and then send the selected values back through `settings.forcedTools`.

## Sample scenes

The `default` factory currently includes these scenes:

- `General Requests` - general conversation, browser/client tools, and browser commands
- `Calculator` - arithmetic through `ICalculatorService`
- `Shape Operations` - shape descriptions and area calculations through `IShapeService`
- `Technical Documentation Estimator` - long-form estimation prompting for Microsoft-centric project sizing

Those display names are normalized internally when scenes are registered. For direct scene execution, names with spaces become underscore-based keys such as `General_Requests`, `Shape_Operations`, and `Technical_Documentation_Estimator`.

The sample also adds a main actor and enables:

- default guardrails
- in-memory cache with 30 minute expiration
- repository persistence for conversations
- planning with max recursion depth `5`
- retry and telemetry
- voice pipeline on the `default` factory

## Conversation persistence

Conversation endpoints are enabled only because the sample does both:

- calls `UseRepository()` in the `default` PlayFramework builder
- registers `IRepository<StoredConversation, string>` with the matching factory name `default`

That repository is in-memory, so conversation data is reset when the app restarts.

## CORS and local dev behavior

The sample uses:

```csharp
policy.AllowAnyOrigin()
      .AllowAnyHeader()
      .AllowAnyMethod();
```

So it is intentionally permissive for local testing. It is not a production CORS configuration.

## Important caveats

### The sample request body is not `prompt`

Older examples sometimes show `prompt` and top-level `sceneName`. The current API uses `message` and `settings.sceneName`.

### The `foundry` factory is only partially wired by default

`Program.cs` maps `foundry` endpoints, but the actual `AddAdapterForFoundryLocal(...)` registration is commented out. Uncomment and configure that section before expecting `foundry` routes to work.

### The `foundry` conversation path is not fully aligned out of the box

The sample repository registration is only named `default`, while `foundry` also enables conversation endpoints. If you want persisted conversations for `foundry`, add a matching repository registration for that factory too.

### The Azure adapter path deserves a quick review before relying on it

The sample registers Azure OpenAI as a named adapter and falls back to a direct `MockChatClient` when Azure settings are absent. If you customize the sample, keep PlayFramework factory names, chat-client names, repository names, and voice-adapter names aligned.

## Useful references

- `src/AI/Test/Rystem.PlayFramework.Api/Program.cs`
- `src/AI/Rystem.PlayFramework/README.md`
- `src/AI/Rystem.PlayFramework.Adapters/README.md`
- `src/AI/Rystem.PlayFramework.Adapters.FoundryLocal/README.md`
- `src/AI/Rystem.PlayFramework.Client/README.md`
- `src/AI/Rystem.PlayFramework.Client/src/rystem/README.md`

Use this sample when you want a runnable local backend for PlayFramework HTTP and client integration testing.
