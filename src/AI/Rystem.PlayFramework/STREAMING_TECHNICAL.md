# Streaming Technical Architecture

## 🎯 Overview

PlayFramework ora implementa lo **streaming nativo ottimistico** che fornisce streaming token-by-token direttamente dal provider LLM. 

**Strategia**: Inizia sempre con streaming nativo, rileva function calls progressivamente, e passa automaticamente ad accumulo silenzioso quando necessario.

## 🏗️ Architettura Optimistic Streaming

### What Changed

**❌ BEFORE (Simulated Streaming)**:
```csharp
// 1. Get complete response first
var response = await GetResponseAsync(...);

// 2. Check for function calls
if (no function calls && EnableStreaming)
{
    // 3. Simulate streaming by splitting words
    await foreach (var word in SplitWords(response.Text))
    {
        yield return word;
    }
}
```

**✅ NOW (Native Optimistic Streaming)**:
```csharp
// 1. Start streaming immediately
await foreach (var chunk in GetStreamingResponseAsync(...))
{
    // 2. Detect function calls progressively
    if (chunk contains function call)
    {
        // Switch to silent accumulation
        hasDetectedFunctionCall = true;
        accumulatedFunctionCalls.Add(chunk.FunctionCall);
    }
    
    // 3. Stream to user ONLY if no function call detected yet
    if (!hasDetectedFunctionCall && chunk.Text != null)
    {
        yield return chunk.Text;  // ✅ Real token from LLM!
    }
}
```

### Flow Diagram - Optimistic Streaming

```
┌─────────────────────────────────────────────────────────┐
│                    User Request                         │
│               "Write a story about..."                  │
└────────────────────┬────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│           ExecuteSceneAsync - Tool Loop                 │
│       ✅ OPTIMISTIC NATIVE STREAMING                    │
│                                                          │
│   settings.EnableStreaming == true ?                    │
│   ├─ YES → GetStreamingResponseAsync()                  │
│   │                                                      │
│   │   ┌─────────────────────────────────────┐           │
│   │   │  Chunk 1: "Once"                    │           │
│   │   │  → Check: Contains function call?   │           │
│   │   │     NO → ✅ STREAM TO USER!         │           │
│   │   │  → hasDetectedFunctionCall = false  │           │
│   │   └─────────────────────────────────────┘           │
│   │                                                      │
│   │   ┌─────────────────────────────────────┐           │
│   │   │  Chunk 2: " upon"                   │           │
│   │   │  → Check: Contains function call?   │           │
│   │   │     NO → ✅ STREAM TO USER!         │           │
│   │   └─────────────────────────────────────┘           │
│   │                                                      │
│   │   ┌─────────────────────────────────────┐           │
│   │   │  Chunk 3: " a time"                 │           │
│   │   │  → Check: Contains function call?   │           │
│   │   │     NO → ✅ STREAM TO USER!         │           │
│   │   └─────────────────────────────────────┘           │
│   │                                                      │
│   │   ... (continues streaming) ...                     │
│   │                                                      │
│   │   ┌─────────────────────────────────────┐           │
│   │   │  Chunk N: (IsComplete=true)         │           │
│   │   │  → Finalize with costs              │           │
│   │   └─────────────────────────────────────┘           │
│   │                                                      │
│   └─ NO  → GetResponseAsync() (fallback)                │
│            Returns: "Once upon a time..."               │
└─────────────────────────────────────────────────────────┘

Alternative Flow: Function Call Detected During Streaming
┌─────────────────────────────────────────────────────────┐
│                    User Request                         │
│         "Calculate 15 + 27 and explain it"              │
└────────────────────┬────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│           ExecuteSceneAsync - Tool Loop                 │
│       ✅ OPTIMISTIC NATIVE STREAMING                    │
│                                                          │
│   GetStreamingResponseAsync()                           │
│                                                          │
│   ┌─────────────────────────────────────────┐           │
│   │  Chunk 1: "Let"                         │           │
│   │  → Check: Contains function call? NO    │           │
│   │  → ✅ STREAM TO USER!                   │           │
│   │  accumulatedText = "Let"                │           │
│   └─────────────────────────────────────────┘           │
│                                                          │
│   ┌─────────────────────────────────────────┐           │
│   │  Chunk 2: " me"                         │           │
│   │  → Check: Contains function call? NO    │           │
│   │  → ✅ STREAM TO USER!                   │           │
│   │  accumulatedText = "Let me"             │           │
│   └─────────────────────────────────────────┘           │
│                                                          │
│   ┌─────────────────────────────────────────┐           │
│   │  Chunk 3: FunctionCall("Add", {a:15})   │           │
│   │  → Check: Contains function call? YES!  │           │
│   │  → ⚠️ SWITCH TO SILENT MODE             │           │
│   │  → hasDetectedFunctionCall = true       │           │
│   │  → ❌ DO NOT stream to user anymore     │           │
│   │  accumulatedFunctionCalls.Add(...)      │           │
│   └─────────────────────────────────────────┘           │
│                                                          │
│   ┌─────────────────────────────────────────┐           │
│   │  Chunk 4: more function call args...    │           │
│   │  → hasDetectedFunctionCall == true      │           │
│   │  → 📦 Accumulate silently               │           │
│   └─────────────────────────────────────────┘           │
│                                                          │
│   → Stream complete → Execute tools                     │
│   → Result: 42                                          │
│   → Add tool result to conversation                     │
│   → Loop again (may stream final explanation)           │
└─────────────────────────────────────────────────────────┘
```

## 🔧 Implementation Details

### 1. Entry Point: `ExecuteSceneAsync`

```csharp
// File: Services/SceneManager.cs
// Line: ~827

private async IAsyncEnumerable<AiSceneResponse> ExecuteSceneAsync(...)
{
    // ... scene actors, MCP tools, conversation setup

    while (iteration < MaxToolCallIterations)
    {
        // 🎯 OPTIMISTIC STREAMING PIPELINE
        
        if (settings.EnableStreaming)
        {
            // ✅ ALWAYS START WITH NATIVE STREAMING
            var accumulatedText = new StringBuilder();
            var accumulatedFunctionCalls = new List<FunctionCallContent>();
            var hasDetectedFunctionCall = false;
            var streamedToUser = false;
            
            decimal? totalCost = null;
            int? totalInputTokens = null;
            int? totalOutputTokens = null;
            int? totalCachedInputTokens = null;

            // 🚀 START STREAMING IMMEDIATELY
            await foreach (var streamUpdateWithCost in context.ChatClientManager
                .GetStreamingResponseAsync(
                    conversationMessages,
                    chatOptions,
                    cancellationToken))
            {
                var chunk = streamUpdateWithCost.Update;

                // 🔍 PROGRESSIVE FUNCTION CALL DETECTION
                var chunkFunctionCalls = chunk.Contents?
                    .OfType<FunctionCallContent>()
                    .ToList() ?? [];

                if (chunkFunctionCalls.Any())
                {
                    // ⚠️ FUNCTION CALL DETECTED - SWITCH TO SILENT MODE
                    hasDetectedFunctionCall = true;
                    accumulatedFunctionCalls.AddRange(chunkFunctionCalls);
                }

                // 📝 ACCUMULATE TEXT
                if (chunk.Text != null)
                {
                    accumulatedText.Append(chunk.Text);
                }

                // 🎬 STREAM TO USER (ONLY IF NO FUNCTION CALL DETECTED YET)
                if (!hasDetectedFunctionCall && chunk.Text != null)
                {
                    streamedToUser = true;
                    yield return new AiSceneResponse
                    {
                        Status = AiResponseStatus.Streaming,
                        StreamingChunk = chunk.Text,  // ✅ Real token from LLM!
                        Message = accumulatedText.ToString(),
                        IsStreamingComplete = false
                    };
                }

                // 💰 TRACK COSTS
                if (streamUpdateWithCost.Cost.HasValue)
                {
                    totalCost = (totalCost ?? 0) + streamUpdateWithCost.Cost.Value;
                }
                if (chunk.Contents != null)
                {
                    foreach (var content in chunk.Contents)
                    {
                        if (content.AdditionalProperties != null)
                        {
                            totalInputTokens = (totalInputTokens ?? 0) + 
                                GetTokenCount(content.AdditionalProperties, "inputTokens");
                            totalOutputTokens = (totalOutputTokens ?? 0) + 
                                GetTokenCount(content.AdditionalProperties, "outputTokens");
                            totalCachedInputTokens = (totalCachedInputTokens ?? 0) + 
                                GetTokenCount(content.AdditionalProperties, "cachedInputTokens");
                        }
                    }
                }
            }

            // 🏁 STREAMING COMPLETE
            
            // Build final message for conversation history
            var finalMessage = new ChatMessage(ChatRole.Assistant, accumulatedText.ToString());
            if (accumulatedFunctionCalls.Any())
            {
                foreach (var functionCall in accumulatedFunctionCalls)
                {
                    finalMessage.Contents.Add(functionCall);
                }
            }
            conversationMessages.Add(finalMessage);

            if (accumulatedFunctionCalls.Count == 0)
            {
                // ✅ PURE TEXT RESPONSE - FINALIZE
                if (streamedToUser)
                {
                    // Already streamed, just send completion marker
                    yield return YieldAndTrack(context, new AiSceneResponse
                    {
                        Status = AiResponseStatus.Running,
                        StreamingChunk = string.Empty,
                        IsStreamingComplete = true,
                        Cost = totalCost,
                        TotalCost = context.AddCost(totalCost ?? 0)
                    });
                }
                yield break;  // Exit tool loop
            }
            else
            {
                // ⚙️ EXECUTE TOOLS (same logic as before)
                foreach (var functionCall in accumulatedFunctionCalls)
                {
                    // ... execute tool and yield results
                }
            }
        }
        else
        {
            // 📦 NON-STREAMING FALLBACK (backward compatible)
            var responseWithCost = await context.ChatClientManager.GetResponseAsync(
                conversationMessages,
                chatOptions,
                cancellationToken);
            // ... handle complete response as before
        }
    }
}
```
### 2. Key Decision Points

**Why Optimistic Streaming?**

1. **Performance**: ~200-300ms first token vs 2-3 seconds for complete response
2. **User Experience**: Smooth, progressive token streaming (not word-by-word simulation)
3. **Provider Compatibility**: Modern LLMs (GPT-4, Claude 3.5, Gemini) support streaming WITH function calls
4. **Resource Efficiency**: Single streaming request, no additional overhead
5. **Graceful Degradation**: Automatically switches to silent accumulation when function calls appear

**Detection Strategy**:
- Check each streaming chunk for `FunctionCallContent`
- Once detected, stop visible streaming but continue accumulation
- Build complete response with both text and function calls for conversation history
- Execute tools normally (same logic as before)

**Backward Compatibility**:
- Non-streaming mode (`EnableStreaming = false`) still uses `GetResponseAsync`
- Existing code works identically
- No breaking changes
            cancellationToken: cancellationToken);

        yield return new AiSceneResponse
        {
            Status = AiResponseStatus.Running,
            Message = responseWithCost.Response.Messages?.FirstOrDefault()?.Text,
            // ... costs
        };
    }
}
```

### 4. Core Streaming Processor: `ProcessStreamingChunkAsync`

```csharp
// File: Services/SceneManager.cs
// Line: ~1500

private async IAsyncEnumerable<AiSceneResponse> ProcessStreamingChunkAsync(
    ChatResponseUpdate streamChunk,
    string? sceneName,
    SceneContext context,
    SceneRequestSettings settings,
    CancellationToken cancellationToken)
{
    // 📝 Accumulate complete message (stored in context for tracking)
    var contextKey = $"streaming_message_{sceneName ?? "final"}";
    if (!context.Properties.TryGetValue(contextKey, out var accumulatedObj))
    {
        accumulatedObj = new StringBuilder();
        context.Properties[contextKey] = accumulatedObj;
    }
    var accumulated = (StringBuilder)accumulatedObj;

    // 📦 Get the text from this chunk
    var chunkText = streamChunk.Text ?? string.Empty;
    accumulated.Append(chunkText);

    // ✅ Check if this is the final chunk (has completion reason)
    var isComplete = streamChunk.FinishReason != null;

    var streamResponse = new AiSceneResponse
    {
        Status = isComplete ? AiResponseStatus.Running : AiResponseStatus.Streaming,
        SceneName = sceneName,
        StreamingChunk = chunkText,            // 📦 Current chunk
        Message = accumulated.ToString(),       // 📚 Full message so far
        IsStreamingComplete = isComplete
    };

    // On the last chunk, clean up and finalize
    if (isComplete)
    {
        streamResponse.TotalCost = context.TotalCost;
        yield return YieldAndTrack(context, streamResponse);

        // Clean up accumulated text
        context.Properties.Remove(contextKey);
    }
    else
    {
        streamResponse.TotalCost = context.TotalCost;
        yield return streamResponse;
    }

    await Task.CompletedTask; // Satisfy async requirement
}
```

## 📊 Data Flow

### Streaming Response Object: `AiSceneResponse`

```csharp
public sealed class AiSceneResponse
{
    /// <summary>
    /// Status: Streaming (intermediate) or Running (complete)
    /// </summary>
    public AiResponseStatus Status { get; set; }

    /// <summary>
    /// The new piece of text in this streaming event.
    /// Only populated when Status is Streaming or IsStreamingComplete.
    /// </summary>
    public string? StreamingChunk { get; set; }

    /// <summary>
    /// The complete accumulated message so far.
    /// Updated with each chunk.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// True if this is the last chunk in a streaming response.
    /// </summary>
    public bool IsStreamingComplete { get; set; }

    // ... other properties (Cost, Tokens, etc.)
}
```

### Example Stream Progression

```csharp
// Chunk 1 (intermediate)
{
  Status: AiResponseStatus.Streaming,
  StreamingChunk: "C'",
  Message: "C'",
  IsStreamingComplete: false,
  TotalCost: 0.0
}

// Chunk 2 (intermediate)
{
  Status: AiResponseStatus.Streaming,
  StreamingChunk: "era",
  Message: "C'era",
  IsStreamingComplete: false,
  TotalCost: 0.0
}

// Chunk 3 (intermediate)
{
  Status: AiResponseStatus.Streaming,
  StreamingChunk: " una",
  Message: "C'era una",
  IsStreamingComplete: false,
  TotalCost: 0.0
}

// ... more chunks

// Final Chunk
{
  Status: AiResponseStatus.Running,
  StreamingChunk: "fine.",
  Message: "C'era una volta un robot di nome R2 che viveva in una città futuristica. La fine.",
  IsStreamingComplete: true,
  Cost: 0.045,
  TotalCost: 0.123,
  InputTokens: 250,
  OutputTokens: 500
}
```

## 🎨 IChatClientManager Integration

### Interface Definition

```csharp
// File: Abstractions/IChatClientManager.cs

public interface IChatClientManager
{
    /// <summary>
    /// Get streaming response (for text responses only)
    /// </summary>
    IAsyncEnumerable<ChatUpdateWithCost> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get complete response (for function calls or non-streaming)
    /// </summary>
    Task<ChatResponseWithCost> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default);

    // ... other properties
}
```

### Implementation (ChatClientManager)

```csharp
// File: Services/ChatClient/ChatClientManager.cs

public async IAsyncEnumerable<ChatUpdateWithCost> GetStreamingResponseAsync(
    IEnumerable<ChatMessage> chatMessages,
    ChatOptions? options = null,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    var messageList = chatMessages as IList<ChatMessage> ?? chatMessages.ToList();

    // Try primary client
    if (_primaryClient != null)
    {
        await foreach (var update in _primaryClient.GetStreamingResponseAsync(
            messageList, 
            options, 
## 📊 Data Flow: Optimistic Streaming

### Streaming Response Object: `AiSceneResponse`

```csharp
public sealed class AiSceneResponse
{
    /// <summary>
    /// Status: Streaming (intermediate) or Running (complete)
    /// </summary>
    public AiResponseStatus Status { get; set; }

    /// <summary>
    /// The new piece of text in this streaming event (real token from LLM).
    /// Only populated when Status is Streaming or IsStreamingComplete.
    /// </summary>
    public string? StreamingChunk { get; set; }

    /// <summary>
    /// The complete accumulated message so far.
    /// Updated with each chunk.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// True if this is the last chunk in a streaming response.
    /// </summary>
    public bool IsStreamingComplete { get; set; }

    // ... other properties (Cost, Tokens, etc.)
}
```

### Example Stream Progression (Pure Text Response)

```csharp
// Chunk 1 (intermediate) - ~200ms after request
{
  Status: AiResponseStatus.Streaming,
  StreamingChunk: "Once",          // ✅ Real token from GPT-4!
  Message: "Once",
  IsStreamingComplete: false,
  TotalCost: 0.0
}

// Chunk 2 (intermediate) - ~250ms
{
  Status: AiResponseStatus.Streaming,
  StreamingChunk: " upon",         // ✅ Real token
  Message: "Once upon",
  IsStreamingComplete: false,
  TotalCost: 0.0
}

// Chunk 3 (intermediate) - ~300ms
{
  Status: AiResponseStatus.Streaming,
  StreamingChunk: " a",            // ✅ Real token
  Message: "Once upon a",
  IsStreamingComplete: false,
  TotalCost: 0.0
}

// ... more chunks (~50ms between each)

// Final Chunk - ~3 seconds total
{
  Status: AiResponseStatus.Running,
  StreamingChunk: "",              // Empty on finalization
  Message: "Once upon a time, there was a robot named R2...",
  IsStreamingComplete: true,
  Cost: 0.045,
  TotalCost: 0.123,
  InputTokens: 250,
  OutputTokens: 500
}
```

### Example Stream Progression (Function Call Detected)

```csharp
// Chunk 1 (streamed to user) - ~200ms
{
  Status: AiResponseStatus.Streaming,
  StreamingChunk: "Let",
  Message: "Let",
  IsStreamingComplete: false
}

// Chunk 2 (streamed to user) - ~250ms
{
  Status: AiResponseStatus.Streaming,
  StreamingChunk: " me",
  Message: "Let me",
  IsStreamingComplete: false
}

// Chunk 3 (FUNCTION CALL DETECTED - NOT streamed to user!)
{
  // Internally accumulated, but NOT yielded to user
  // hasDetectedFunctionCall = true
  // accumulatedFunctionCalls.Add(FunctionCallContent)
}

// Chunk 4+ (accumulated silently)
// ... continue accumulating until streaming complete

// End of stream
// → Execute function calls
// → Yield FunctionRequest responses
// → Loop again for final result
```

## ⚠️ Important Notes

### 1. Function Call Detection

✅ **Function calls ARE detected during streaming**:
- Each chunk checked for `FunctionCallContent`
- When detected, streaming to user stops immediately
- Silent accumulation continues until stream complete
- Then executes tools normally

❌ **Function calls are NOT streamed visibly to user**:
- Would confuse UX to show partial JSON
- Tool execution requires complete parameters

### 2. Cost Tracking in Streaming

- **Progressive accumulation**: Costs added from each chunk's usage metadata
- **Final costs**: Included in completion marker (`IsStreamingComplete = true`)
- **Total cost**: Always available (`TotalCost` includes all previous operations)
- **Token counts**: `InputTokens`, `OutputTokens`, `CachedInputTokens` tracked progressively

### 3. Provider Compatibility

✅ **Full Support** (streaming + function calls):
- OpenAI GPT-4 / GPT-4 Turbo
- Azure OpenAI (GPT-4)
- Anthropic Claude 3.5 Sonnet
- Google Gemini ProProvider Compatibility

⚠️ **Partial Support** (may not support function calls during streaming):
- Local models via Ollama (depends on model)
- Some custom providers

**Fallback**: If streaming with function calls not supported, use non-streaming mode (`EnableStreaming = false`)

## 🎯 Key Performance Improvements

| Metric | Before (Simulated) | After (Optimistic Native) | Improvement |
|--------|-------------------|---------------------------|-------------|
| **First token latency** | 2-3 seconds | 200-300ms | **10x faster** |
| **Streaming quality** | Word-by-word (chunky) | Token-by-token (smooth) | Much better UX |
| **API calls** | 1 complete request | 1 streaming request | Same |
| **Function call detection** | After complete response | Progressive during streaming | Faster detection |
| **Overhead** | Response + simulation loop | Single streaming request | Zero overhead |

## 🔗 Related Files

- [Services/SceneManager.cs](Services/SceneManager.cs) - Core optimistic streaming implementation
- [Services/ChatClient/ChatClientManager.cs](Services/ChatClient/ChatClientManager.cs) - LLM client wrapper
- [Domain/Models/AiSceneResponse.cs](Domain/Models/AiSceneResponse.cs) - Response model with streaming properties
- [OPTIMISTIC_STREAMING_CHANGELOG.md](OPTIMISTIC_STREAMING_CHANGELOG.md) - Complete changelog and testing guide
- [STREAMING.md](STREAMING.md) - User guide with examples
- [MULTI_MODAL_PLAN.md](MULTI_MODAL_PLAN.md) - Multi-modal implementation plan

---

**Documentation Updated**: February 2025  
**Implementation**: Optimistic Native Streaming  
**Breaking Changes**: None (fully backward compatible)
