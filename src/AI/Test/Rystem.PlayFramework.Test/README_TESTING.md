# PlayFramework - Testing Guide

This guide explains how to run tests with **mock** or **real Azure OpenAI** integration.

## Test Types

### 1. Unit Tests (Mock)
These tests use a `MockChatClient` and **do not** require Azure OpenAI credentials.

**Run all mock tests:**
```bash
dotnet test src/AI/Test/Rystem.PlayFramework.Test/Rystem.PlayFramework.Test.csproj
```

### 2. Integration Tests (Azure OpenAI)
These opt-in tests use the production `AddAdapterForAzureOpenAI` registration and require
valid credentials. They never fall back to `MockChatClient`.

## Setup Azure OpenAI Credentials

### Option 1: User Secrets (Recommended for local development)

1. Navigate to the test project directory:
```bash
cd src/AI/Test/Rystem.PlayFramework.Test
```

2. Initialize user secrets (already configured with UserSecretsId):
```bash
dotnet user-secrets set "OpenAi:ApiKey" "YOUR_AZURE_OPENAI_API_KEY"
dotnet user-secrets set "OpenAi:Endpoint" "https://YOUR_RESOURCE_NAME.openai.azure.com/"
dotnet user-secrets set "OpenAi:Deployments:Responses" "YOUR_RESPONSES_DEPLOYMENT"
```

3. List secrets to verify:
```bash
dotnet user-secrets list
```

### Option 2: appsettings.json (Not recommended - do not commit!)

Edit `src/AI/Test/Rystem.PlayFramework.Test/appsettings.json`:
```json
{
  "OpenAi": {
    "ApiKey": "YOUR_API_KEY",
    "AzureResourceName": "YOUR_RESOURCE_NAME",
    "ModelName": "gpt-4o"
  }
}
```

**⚠️ WARNING: Do NOT commit credentials to source control!**

## Running Azure OpenAI Integration Tests

Set the opt-in flag and Azure settings before running a suite:

```bash
export RYSTEM_RUN_AZURE_OPENAI_INTEGRATION=1
export AZURE_OPENAI_ENDPOINT="https://<resource>.openai.azure.com/"
export AZURE_OPENAI_DEPLOYMENT="<deployment>"
export AZURE_OPENAI_API_KEY="<key>"
```

The complete live matrix uses these deployment names:

| Variable | Required capability | Suggested model family |
| --- | --- | --- |
| `AZURE_OPENAI_DEPLOYMENT` | Responses API, streaming, tools and cost usage | your already validated Responses deployment |
| `AZURE_OPENAI_CHAT_DEPLOYMENT` | Chat Completions, streaming and tools | may be the same deployment if it supports Chat Completions |
| `AZURE_OPENAI_FILES_DEPLOYMENT` | Responses plus `input_file` / hosted files | a vision-capable Responses model that accepts files; `gpt-4o` or a compatible GPT-5 deployment |
| `AZURE_OPENAI_VISION_DEPLOYMENT` | Responses plus image input | a vision-enabled deployment such as GPT-4.1, GPT-4o, o-series or GPT-5 |
| `AZURE_OPENAI_AUDIO_DEPLOYMENT` | Chat Completions with inline audio input | an audio model such as `gpt-audio`, `gpt-audio-mini`, `gpt-audio-1.5`, or a supported GPT-4o audio deployment |
| `AZURE_OPENAI_STT_DEPLOYMENT` | `/openai/v1/audio/transcriptions` | `gpt-transcribe`, `gpt-4o-transcribe`, `gpt-4o-mini-transcribe`, or `whisper` |
| `AZURE_OPENAI_TTS_DEPLOYMENT` | `/openai/v1/audio/speech` | `gpt-4o-mini-tts`, `tts`, or `tts-hd` |

The values are Azure **deployment names**, not necessarily the underlying catalog model IDs.
You can point several variables at the same deployment only when that deployment supports
all the corresponding capabilities.

Azure currently routes STT and TTS through the deployment-specific Audio API.
Both transcription and speech synthesis use `api-version=2025-04-01-preview` by default (the surface
that documents `gpt-4o-transcribe` / `gpt-4o-mini-transcribe` alongside classic `whisper-1`
deployments); override via `AdapterSettings.SpeechToTextApiVersion` /
`VoiceAdapterSettings.SttApiVersion` if a specific resource needs a different transcription
`api-version`. Responses, Chat, Files, and inline audio use the v1 endpoint. In Sweden Central,
`tts-hd` version `001`, `gpt-audio-mini`, and `gpt-4o-transcribe` are valid choices.

Example configuration:

```bash
export AZURE_OPENAI_CHAT_DEPLOYMENT="<chat-deployment>"
export AZURE_OPENAI_FILES_DEPLOYMENT="<file-capable-deployment>"
export AZURE_OPENAI_VISION_DEPLOYMENT="<vision-deployment>"
export AZURE_OPENAI_AUDIO_DEPLOYMENT="<audio-chat-deployment>"
export AZURE_OPENAI_STT_DEPLOYMENT="<transcription-deployment>"
export AZURE_OPENAI_TTS_DEPLOYMENT="<speech-deployment>"
```

The same values can be stored as user secrets under `OpenAi:Deployments`:

```bash
dotnet user-secrets set "OpenAi:Deployments:Responses" "<responses-deployment>"
dotnet user-secrets set "OpenAi:Deployments:Chat" "<chat-deployment>"
dotnet user-secrets set "OpenAi:Deployments:Files" "<files-deployment>"
dotnet user-secrets set "OpenAi:Deployments:Vision" "<vision-deployment>"
dotnet user-secrets set "OpenAi:Deployments:Audio" "<audio-chat-deployment>"
dotnet user-secrets set "OpenAi:Deployments:SpeechToText" "<transcription-deployment>"
dotnet user-secrets set "OpenAi:Deployments:TextToSpeech" "<speech-deployment>"
```

Environment variables take precedence over user secrets. Values equal to the checked-in
`in secrets` placeholder are treated as missing.

Run the API key suite:

```bash
dotnet test src/AI/Test/Rystem.PlayFramework.Test/Rystem.PlayFramework.Test.csproj --filter "Category=AzureOpenAIApiKey"
```

That command executes every configured API-key gate and reports tests whose specialized
deployment variable is missing as skipped. Individual areas can be run with:

```bash
dotnet test src/AI/Test/Rystem.PlayFramework.Test/Rystem.PlayFramework.Test.csproj --filter "Category=AzureOpenAIResponses"
dotnet test src/AI/Test/Rystem.PlayFramework.Test/Rystem.PlayFramework.Test.csproj --filter "Category=AzureOpenAIChat"
dotnet test src/AI/Test/Rystem.PlayFramework.Test/Rystem.PlayFramework.Test.csproj --filter "Category=AzureOpenAIFiles"
dotnet test src/AI/Test/Rystem.PlayFramework.Test/Rystem.PlayFramework.Test.csproj --filter "Category=AzureOpenAIVision"
dotnet test src/AI/Test/Rystem.PlayFramework.Test/Rystem.PlayFramework.Test.csproj --filter "Category=AzureOpenAIAudioInline"
dotnet test src/AI/Test/Rystem.PlayFramework.Test/Rystem.PlayFramework.Test.csproj --filter "Category=AzureOpenAIVoice"
```

Run the Entra ID suite after removing `AZURE_OPENAI_API_KEY` and signing in with a
supported `DefaultAzureCredential` source:

```bash
dotnet test src/AI/Test/Rystem.PlayFramework.Test/Rystem.PlayFramework.Test.csproj --filter "Category=AzureOpenAIEntra"
```

Without `RYSTEM_RUN_AZURE_OPENAI_INTEGRATION=1`, all live tests are reported as skipped.
The Files gate deletes the uniquely named remote artifact it creates. Voice gates use
short generated audio and the STT gate transcribes audio produced by the TTS deployment.
The inline-audio functional gate is currently skipped because the configured deployment
does not process the attached audio despite a valid `input_audio` request. This limitation
is tracked separately; the suite does not count a non-empty refusal as successful coverage.

## Test Configuration

### PlayFrameworkTestBase

Base class for tests with two modes:

**Mock mode (default):**
```csharp
public class MyTests : PlayFrameworkTestBase
{
    // Uses MockChatClient
}
```

**Azure OpenAI mode:**
```csharp
public class MyTests : PlayFrameworkTestBase
{
    public MyTests() : base(useRealAzureOpenAI: true)
    {
        // Uses the production AddAdapterForAzureOpenAI registration
    }
}
```

## Available Integration Tests

### `AzureOpenAIIntegrationTests`

| Test | Description |
|------|-------------|
| `AzureOpenAI_ShouldConnect` | Basic connection test |
| `PlayFramework_WithAzureOpenAI_ShouldExecuteCalculation` | Calculator scene with tool calling |
| `PlayFramework_WithAzureOpenAI_ShouldHandleMultipleOperations` | Complex multi-step calculations |
| `PlayFramework_ShouldTrackCostsAccurately` | Cost and token tracking |

## Troubleshooting

### "AZURE_OPENAI_API_KEY is required"
Ensure the API key suite has an environment variable or user secret configured:
```bash
dotnet user-secrets list
```

### "Deployment not found"
Verify your model deployment name matches `OpenAi:ModelName` in settings.

### "Quota exceeded"
Check your Azure OpenAI quota in Azure Portal.

### Tests are slow
Integration tests make real API calls and can take several seconds per test.

## Cost Considerations

⚠️ **Running integration tests will consume Azure OpenAI tokens and incur costs!**

- Each test makes real API calls
- Calculator tests: ~100-500 tokens per test
- Complex multi-step tests: ~500-2000 tokens
- Costs tracked in test output

**Estimate:** Running all integration tests (~4-5 tests) ≈ $0.01-0.05 USD

## CI/CD Integration

For CI/CD pipelines, use environment variables or Azure Key Vault:

```yaml
# GitHub Actions example
- name: Run Integration Tests
  env:
    RYSTEM_RUN_AZURE_OPENAI_INTEGRATION: "1"
    AZURE_OPENAI_API_KEY: ${{ secrets.AZURE_OPENAI_API_KEY }}
    AZURE_OPENAI_ENDPOINT: ${{ secrets.AZURE_OPENAI_ENDPOINT }}
    AZURE_OPENAI_DEPLOYMENT: ${{ vars.AZURE_OPENAI_DEPLOYMENT }}
  run: |
    dotnet test src/AI/Test/Rystem.PlayFramework.Test/Rystem.PlayFramework.Test.csproj --filter "Category=AzureOpenAIResponses"
```

## Live gate coverage

- Responses and Chat Completions, streaming and non-streaming
- Required function/tool declaration on both APIs
- API key and Microsoft Entra ID
- hosted file upload, use, listing, and cleanup
- inline image input
- inline audio input gate retained but skipped pending the separately tracked deployment issue
- speech-to-text through both `IVoiceAdapter` and `SpeechToTextChatClient`
- text-to-speech output
- usage and adapter-owned cost tracking
