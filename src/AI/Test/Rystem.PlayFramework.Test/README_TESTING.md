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
These tests use real Azure OpenAI and require valid credentials.

## Setup Azure OpenAI Credentials

### Option 1: User Secrets (Recommended for local development)

1. Navigate to the test project directory:
```bash
cd src/AI/Test/Rystem.PlayFramework.Test
```

2. Initialize user secrets (already configured with UserSecretsId):
```bash
dotnet user-secrets set "OpenAi:ApiKey" "YOUR_AZURE_OPENAI_API_KEY"
dotnet user-secrets set "OpenAi:AzureResourceName" "YOUR_RESOURCE_NAME"
dotnet user-secrets set "OpenAi:ModelName" "gpt-4o"
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

### Step 1: Remove Skip Attributes

Open test files and remove the `Skip` parameter:

**Before:**
```csharp
[Fact(Skip = "Requires Azure OpenAI - Remove Skip attribute to run")]
public async Task AzureOpenAI_ShouldConnect() { ... }
```

**After:**
```csharp
[Fact]
public async Task AzureOpenAI_ShouldConnect() { ... }
```

### Step 2: Run Tests

**Run all integration tests:**
```bash
dotnet test src/AI/Test/Rystem.PlayFramework.Test/Rystem.PlayFramework.Test.csproj --filter "FullyQualifiedName~AzureOpenAIIntegrationTests"
```

**Run specific test:**
```bash
dotnet test --filter "FullyQualifiedName~AzureOpenAI_ShouldConnect"
```

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
        // Uses AzureOpenAIChatClientAdapter
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

### "ApiKey is empty"
Ensure user secrets are configured correctly:
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
    OpenAi__ApiKey: ${{ secrets.AZURE_OPENAI_API_KEY }}
    OpenAi__AzureResourceName: ${{ secrets.AZURE_RESOURCE_NAME }}
    OpenAi__ModelName: "gpt-4o"
  run: |
    dotnet test --filter "FullyQualifiedName~AzureOpenAIIntegrationTests"
```

## Next Steps

- Implement tool calling support (function calling)
- Add streaming response tests
- Test with different models (gpt-4, gpt-3.5-turbo)
- Add rate limiting and retry logic
- Test error handling scenarios
