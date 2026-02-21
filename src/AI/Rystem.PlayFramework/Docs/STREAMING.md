# Streaming Support

## 🎬 Overview

The **Streaming Support** feature enables real-time, progressive delivery of LLM responses token-by-token, providing a better user experience similar to ChatGPT's typing effect.

## 🎯 Key Features

- ✅ **Progressive Delivery**: Receive responses as they're generated, not all at once
- ✅ **Better UX**: Users see results immediately, reducing perceived latency
- ✅ **Selective Streaming**: Only text responses stream (function calls require complete JSON)
- ✅ **Accumulative Messages**: Each chunk includes both the new piece and the full accumulated text
- ✅ **Budget Compatible**: Works seamlessly with budget limits
- ✅ **Cost Tracking**: Costs tracked when streaming completes (if available from LLM)

## 🚀 Quick Start

### Basic Usage

```csharp
var settings = new SceneRequestSettings
{
    EnableStreaming = true // Enable streaming
};

await foreach (var response in sceneManager.ExecuteAsync("Write a story", settings))
{
    if (response.Status == AiResponseStatus.Streaming)
    {
        // Progressive chunk
        Console.Write(response.StreamingChunk); // "C'" → "era" → " una" → ...
    }
    else if (response.IsStreamingComplete)
    {
        // Final chunk
        Console.WriteLine($"\n\nComplete: {response.Message}");
    }
}
```

### Console Output Example

```
User: "Write a short story about a robot"

AI (streaming):
C'era una volta un robot di nome R2...
```

**What you see progressively:**
```
C'
C'era
C'era una
C'era una volta
C'era una volta un
C'era una volta un robot
...
```

## 📊 How It Works

### Architecture

```
┌─────────────────────┐
│   User Request      │
│  "Write a story"    │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│   Scene Selection   │  ← Function call (NOT streamed)
│   (Tool Calling)    │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│   Tool Execution    │  ← Function calls (NOT streamed)
│    Loop (N calls)   │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│  Final Text Response│  ✅ STREAMED token-by-token!
│ "C'era una volta..."│
└─────────────────────┘
```

### Response Composition

Each streaming chunk builds upon the previous:

```csharp
Chunk 1: { StreamingChunk: "C'", Message: "C'", IsStreamingComplete: false }
Chunk 2: { StreamingChunk: "era", Message: "C'era", IsStreamingComplete: false }
Chunk 3: { StreamingChunk: " una", Message: "C'era una", IsStreamingComplete: false }
Chunk 4: { StreamingChunk: " volta", Message: "C'era una volta", IsStreamingComplete: false }
...
Chunk N: { StreamingChunk: "fine", Message: "C'era una volta... fine", IsStreamingComplete: true }
```

## 🎨 Usage Patterns

### Pattern 1: Console Streaming

```csharp
var settings = new SceneRequestSettings { EnableStreaming = true };

Console.Write("AI: ");

await foreach (var response in sceneManager.ExecuteAsync(query, settings))
{
    if (response.Status == AiResponseStatus.Streaming)
    {
        Console.Write(response.StreamingChunk); // Print each chunk
    }
    else if (response.IsStreamingComplete)
    {
        Console.WriteLine(); // New line after complete
        Console.WriteLine($"[Cost: ${response.Cost:F6}]");
    }
}
```

### Pattern 2: Web API Streaming (SignalR)

```csharp
// SignalR Hub
public async Task StreamQuery(string query)
{
    var settings = new SceneRequestSettings { EnableStreaming = true };

    await foreach (var response in _sceneManager.ExecuteAsync(query, settings))
    {
        if (response.Status == AiResponseStatus.Streaming || response.IsStreamingComplete)
        {
            // Send each chunk to connected clients
            await Clients.Caller.SendAsync("ReceiveChunk", new
            {
                Chunk = response.StreamingChunk,
                AccumulatedText = response.Message,
                IsComplete = response.IsStreamingComplete,
                Cost = response.Cost,
                TotalCost = response.TotalCost
            });
        }
    }
}
```

### Pattern 3: Blazor Real-Time UI

```razor
@page "/ai-chat"
@inject ISceneManager SceneManager

<div class="chat-container">
    <div class="message ai-message">
        @aiMessage
        @if (isStreaming)
        {
            <span class="cursor">▊</span>
        }
    </div>
</div>

@code {
    private string aiMessage = "";
    private bool isStreaming = false;

    private async Task SendQuery(string query)
    {
        aiMessage = "";
        isStreaming = true;

        var settings = new SceneRequestSettings { EnableStreaming = true };

        await foreach (var response in SceneManager.ExecuteAsync(query, settings))
        {
            if (response.Status == AiResponseStatus.Streaming || response.IsStreamingComplete)
            {
                aiMessage = response.Message ?? "";
                isStreaming = !response.IsStreamingComplete;
                StateHasChanged(); // Update UI
            }
        }

        isStreaming = false;
        StateHasChanged();
    }
}
```

### Pattern 4: TypeScript/React Streaming

```typescript
const [streamingText, setStreamingText] = useState('');
const [isStreaming, setIsStreaming] = useState(false);

const streamQuery = async (query: string) => {
  setStreamingText('');
  setIsStreaming(true);

  // Assume API returns Server-Sent Events (SSE)
  const eventSource = new EventSource(`/api/ai/stream?query=${query}`);

  eventSource.onmessage = (event) => {
    const data = JSON.parse(event.data);
    
    if (data.status === 'Streaming' || data.isStreamingComplete) {
      setStreamingText(data.message);
      
      if (data.isStreamingComplete) {
        setIsStreaming(false);
        eventSource.close();
        console.log(`Total cost: $${data.totalCost}`);
      }
    }
  };
};
```

## 🎯 When Streaming Happens

### ✅ Streamed (Text Responses)
- **Final text responses** in scenes
- **Final answer generation** after tool execution
- **Summary responses** (if summarization enabled)

### ❌ NOT Streamed (Structured Responses)
- **Scene selection** (function call)
- **Tool calling** (function calls with JSON arguments)
- **Planning phase** (structured execution plan)

This is because function calls require **complete JSON** to parse correctly.

## 📋 AiSceneResponse Properties

### Streaming-Specific Properties

```csharp
public sealed class AiSceneResponse
{
    /// <summary>
    /// The new piece of text in this streaming event.
    /// </summary>
    public string? StreamingChunk { get; set; }

    /// <summary>
    /// The complete accumulated message so far.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// True if this is the last chunk.
    /// </summary>
    public bool IsStreamingComplete { get; set; }

    /// <summary>
    /// Status: Streaming (intermediate) or Running (complete).
    /// </summary>
    public AiResponseStatus Status { get; set; }

    // ... other properties (Cost, TotalCost, etc.)
}
```

### Example Responses

**Intermediate chunk:**
```json
{
  "status": "Streaming",
  "streamingChunk": " robot",
  "message": "C'era una volta un robot",
  "isStreamingComplete": false,
  "totalCost": 0.0
}
```

**Final chunk:**
```json
{
  "status": "Running",
  "streamingChunk": ".",
  "message": "C'era una volta un robot di nome R2.",
  "isStreamingComplete": true,
  "cost": 0.045,
  "totalCost": 0.123,
  "inputTokens": 250,
  "outputTokens": 500
}
```

## ⚙️ Configuration

### Enable/Disable Streaming

```csharp
// Enable streaming
var settings = new SceneRequestSettings
{
    EnableStreaming = true
};

// Disable streaming (default)
var settings = new SceneRequestSettings
{
    EnableStreaming = false // Or omit (default is false)
};
```

### Streaming with Other Features

```csharp
// Streaming + Budget Limit
var settings = new SceneRequestSettings
{
    EnableStreaming = true,
    MaxBudget = 1.00m // Stop if cost exceeds $1
};

// Streaming + Planning
var settings = new SceneRequestSettings
{
    EnableStreaming = true,
    EnablePlanning = true // Plan execution, then stream final response
};

// Streaming + Caching
var settings = new SceneRequestSettings
{
    EnableStreaming = true,
    CacheBehavior = CacheBehavior.Preferred,
    CacheKey = "user-123-query-456"
};
```

## 🧮 Cost Tracking with Streaming

### Challenge
In streaming mode, token usage is often **not available** until the very end (or not at all with some LLM providers).

### Solution
**Cost tracking happens on the final chunk** when `IsStreamingComplete = true`:

```csharp
await foreach (var response in sceneManager.ExecuteAsync(query, settings))
{
    if (response.IsStreamingComplete)
    {
        // Cost information available here
        Console.WriteLine($"Input tokens: {response.InputTokens}");
        Console.WriteLine($"Output tokens: {response.OutputTokens}");
        Console.WriteLine($"Cost: ${response.Cost:F6}");
        Console.WriteLine($"Total cost: ${response.TotalCost:F6}");
    }
}
```

### Note on Microsoft.Extensions.AI
The `ChatResponseUpdate` type (used for streaming) **does not include `Usage`** property in `Microsoft.Extensions.AI`. This means:
- ✅ Costs **can be tracked** with complete `GetResponseAsync()` calls
- ⚠️ Costs **cannot be tracked** during `GetStreamingResponseAsync()` (no usage data)

For accurate cost tracking with streaming, consider:
1. Using non-streaming mode for critical operations where cost precision matters
2. Estimating costs based on approximate token counts
3. Tracking costs at the end of the conversation (aggregate all calls)

## 🎭 Real-World Example

### Scenario: AI-Powered Customer Support

```csharp
public class CustomerSupportService
{
    private readonly ISceneManager _sceneManager;
    private readonly IHubContext<ChatHub> _hubContext;

    public async Task HandleCustomerQuery(string userId, string query)
    {
        var settings = new SceneRequestSettings
        {
            EnableStreaming = true,
            MaxBudget = 0.50m, // Limit cost per query
            CacheKey = $"support-{userId}"
        };

        var accumulatedText = "";

        await foreach (var response in _sceneManager.ExecuteAsync(query, settings))
        {
            switch (response.Status)
            {
                case AiResponseStatus.Initializing:
                    await SendToUser(userId, "🤖 AI Agent is thinking...");
                    break;

                case AiResponseStatus.Streaming:
                    // Send progressive chunks
                    accumulatedText = response.Message ?? "";
                    await SendStreamingUpdate(userId, response.StreamingChunk);
                    break;

                case AiResponseStatus.Running when response.IsStreamingComplete:
                    // Streaming complete
                    await SendFinalMessage(userId, response.Message, response.Cost);
                    break;

                case AiResponseStatus.FunctionRequest:
                    await SendToUser(userId, $"🔧 Executing: {response.FunctionName}...");
                    break;

                case AiResponseStatus.BudgetExceeded:
                    await SendToUser(userId, "⚠️ Query too complex - please simplify.");
                    break;

                case AiResponseStatus.Error:
                    await SendToUser(userId, $"❌ Error: {response.ErrorMessage}");
                    break;
            }
        }
    }

    private async Task SendStreamingUpdate(string userId, string? chunk)
    {
        await _hubContext.Clients.User(userId).SendAsync("StreamChunk", chunk);
    }

    private async Task SendFinalMessage(string userId, string? message, decimal? cost)
    {
        await _hubContext.Clients.User(userId).SendAsync("MessageComplete", new
        {
            Message = message,
            Cost = cost,
            Timestamp = DateTime.UtcNow
        });
    }

    private async Task SendToUser(string userId, string message)
    {
        await _hubContext.Clients.User(userId).SendAsync("Notification", message);
    }
}
```

## 🧪 Testing

### Unit Test Example

```csharp
[Fact]
public async Task ExecuteAsync_WithStreaming_ReturnsProgressiveChunks()
{
    // Arrange
    var settings = new SceneRequestSettings
    {
        EnableStreaming = true
    };

    // Act
    var responses = new List<AiSceneResponse>();
    await foreach (var response in sceneManager.ExecuteAsync("Tell me a story", settings))
    {
        responses.Add(response);
    }

    // Assert
    var streamingChunks = responses.Where(r => r.Status == AiResponseStatus.Streaming).ToList();
    Assert.NotEmpty(streamingChunks); // Should have intermediate chunks

    // Verify accumulation
    for (int i = 1; i < streamingChunks.Count; i++)
    {
        var current = streamingChunks[i].Message?.Length ?? 0;
        var previous = streamingChunks[i - 1].Message?.Length ?? 0;
        Assert.True(current >= previous, "Message should accumulate");
    }

    // Verify final chunk
    var finalChunk = responses.FirstOrDefault(r => r.IsStreamingComplete);
    Assert.NotNull(finalChunk);
    Assert.Equal(AiResponseStatus.Running, finalChunk!.Status);
}
```

## 🛡️ Best Practices

### 1. Use Streaming for Long Responses
```csharp
// ✅ Good: Long text responses
var settings = new SceneRequestSettings
{
    EnableStreaming = true
};
await sceneManager.ExecuteAsync("Write a detailed product description", settings);

// ❌ Avoid: Short, quick responses (overhead not worth it)
await sceneManager.ExecuteAsync("Add 2 + 2", settings);
```

### 2. Handle Network Issues

```csharp
try
{
    await foreach (var response in sceneManager.ExecuteAsync(query, settings))
    {
        if (response.Status == AiResponseStatus.Streaming)
        {
            await SendChunkAsync(response.StreamingChunk);
        }
    }
}
catch (OperationCanceledException)
{
    // User canceled - clean up
    await SendCancellationMessage();
}
catch (Exception ex)
{
    // Network error - retry or fail gracefully
    await SendErrorMessage($"Connection lost: {ex.Message}");
}
```

### 3. Provide Visual Feedback

```typescript
// React example
{isStreaming && (
  <div className="typing-indicator">
    <span className="cursor">▊</span>
  </div>
)}
```

### 4. Allow Cancellation

```csharp
var cts = new CancellationTokenSource();

// User clicks "Stop" button
stopButton.Clicked += (s, e) => cts.Cancel();

await foreach (var response in sceneManager.ExecuteAsync(query, settings, cts.Token))
{
    // Process streaming...
}
```

## 📊 Performance Considerations

### Streaming vs Non-Streaming

| Aspect | Non-Streaming | Streaming |
|--------|---------------|-----------|
| **Time to First Token** | ❌ High (wait for complete response) | ✅ Low (immediate feedback) |
| **Memory Usage** | ✅ Lower (single response) | ⚠️ Higher (accumulating chunks) |
| **Network Overhead** | ✅ Lower (one response) | ⚠️ Higher (multiple chunks) |
| **User Experience** | ❌ Feels slow | ✅ Feels responsive |
| **Cost Tracking** | ✅ Accurate (full usage data) | ⚠️ Limited (usage not always available) |

### When to Use Each

**Use Streaming:**
- Interactive chat interfaces
- Long-form content generation
- Real-time user feedback desired
- Web applications with good connectivity

**Use Non-Streaming:**
- Batch processing
- API integrations (machine-to-machine)
- Cost tracking precision required
- Short, quick responses

## 🔗 Related Features

- **Cost Tracking**: [COST_TRACKING.md](./COST_TRACKING.md) - Track costs (limited in streaming)
- **Budget Limit**: [BUDGET_LIMIT.md](./BUDGET_LIMIT.md) - Stop execution when cost exceeds budget
- **Caching**: Improve performance by caching results (works with streaming)
- **Tool Calling**: [TOOL_CALLING.md](./TOOL_CALLING.md) - Function calls (not streamed)

## 🎯 Summary

Streaming Support provides:
- ✅ **Real-time feedback**: Users see responses as they're generated
- ✅ **Better UX**: Reduced perceived latency
- ✅ **Selective streaming**: Text responses stream, function calls don't
- ✅ **Accumulative messages**: Each chunk includes full text so far
- ✅ **Budget compatible**: Works with cost tracking and budget limits

Perfect for interactive applications where user experience is paramount! 🚀
