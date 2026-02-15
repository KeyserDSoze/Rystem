# Client-Side Tool Execution with Continuation Tokens

This guide explains how to use PlayFramework's client-side tool execution feature, which allows LLMs to request browser/mobile-specific operations (camera, geolocation, file picker, etc.) and resume execution with the results.

## Architecture Overview

1. **Server** (C#) registers client-side tools via `OnClient()` builder
2. **LLM** requests a tool (e.g., "Take a photo")
3. **Server** yields `AwaitingClient` status with continuation token
4. **Client** (TypeScript) executes the tool and resumes with results
5. **Server** continues execution with tool results

## Server Setup (C#)

```csharp
services.AddPlayFramework(builder =>
{
    builder.AddScene("vision-analysis", "Analyze images from camera", scene =>
    {
        scene.OnClient(client =>
        {
            client.AddTool<CameraOptions>("capturePhoto", "Capture photo from user's camera");
            client.AddTool("getLocation", "Get user's GPS coordinates");
        })
        .WithCacheExpiration(TimeSpan.FromMinutes(5)); // Continuation token TTL

        scene.WithService<VisionService>(s => s.AddTool(x => x.AnalyzeImage));
    });
});

// CameraOptions class for JSON Schema generation
public class CameraOptions
{
    [Description("Camera facing mode")]
    public string? FacingMode { get; set; } // "user" or "environment"

    [Range(320, 1920)]
    public int Width { get; set; } = 640;

    [Range(240, 1080)]
    public int Height { get; set; } = 480;
}
```

**Important**: Scenes using `OnClient()` require distributed cache (in-memory or Redis).

## Client Setup (TypeScript/React)

### 1. Register Client Tools

```typescript
import { 
    PlayFrameworkClient, 
    ClientInteractionRegistry,
    AIContentConverter 
} from "@rystem/playframework-client";

// Create registry
const registry = new ClientInteractionRegistry();

// Register camera tool
registry.register("capturePhoto", async (args?: any) => {
    const width = args?.width || 640;
    const height = args?.height || 480;
    const facingMode = args?.facingMode || "user";

    const content = await AIContentConverter.fromCamera(
        { video: { facingMode } },
        width,
        height
    );

    return [content];
});

// Register geolocation tool
registry.register("getLocation", async () => {
    const content = await AIContentConverter.fromGeolocation();
    return [content];
});

// Create PlayFramework client with registry
const client = new PlayFrameworkClient(settings, registry);
```

### 2. Execute with Automatic Continuation

The client automatically handles `AwaitingClient` responses:

```typescript
const request: PlayFrameworkRequest = {
    prompt: "Take a photo and tell me what you see",
    sceneName: "vision-analysis"
};

for await (const response of client.executeStepByStep(request)) {
    console.log(`Status: ${response.status}`);
    console.log(`Message: ${response.message}`);

    // Client automatically executes tools when status === "AwaitingClient"
    // You can still yield these responses to show "Accessing camera..." UI
}
```

### 3. Custom Client Tool Implementation

```typescript
// File picker tool
registry.register("selectFiles", async (args?: { accept?: string, multiple?: boolean }) => {
    return new Promise((resolve) => {
        const input = document.createElement("input");
        input.type = "file";
        input.accept = args?.accept || "*/*";
        input.multiple = args?.multiple || false;

        input.onchange = async () => {
            if (input.files) {
                const contents = await AIContentConverter.fromMultipleFiles(input.files);
                resolve(contents);
            }
        };

        input.click();
    });
});

// Microphone tool
registry.register("recordAudio", async (args?: { durationMs?: number }) => {
    const durationMs = args?.durationMs || 5000;
    const content = await AIContentConverter.fromMicrophone(durationMs);
    return [content];
});

// Custom data tool (e.g., fetch from API)
registry.register("getUserProfile", async () => {
    const response = await fetch("/api/user/profile");
    const profile = await response.json();
    return [AIContentConverter.fromText(JSON.stringify(profile))];
});
```

## AIContentConverter Helpers

### Camera
```typescript
const content = await AIContentConverter.fromCamera(
    { video: { facingMode: "environment" } }, // MediaStreamConstraints
    1920, // width
    1080  // height
);
// Returns: { type: "data", data: "base64...", mediaType: "image/jpeg" }
```

### Geolocation
```typescript
const content = await AIContentConverter.fromGeolocation();
// Returns: { type: "text", text: '{"latitude":45.1234,"longitude":7.5678,...}' }
```

### File Upload
```typescript
const file = fileInput.files[0];
const content = await AIContentConverter.fromFile(file);
// Returns: { type: "data", data: "base64...", mediaType: "image/png" }
```

### Multiple Files
```typescript
const contents = await AIContentConverter.fromMultipleFiles(fileInput.files);
// Returns: AIContent[]
```

### Microphone
```typescript
const content = await AIContentConverter.fromMicrophone(3000, "audio/webm");
// Returns: { type: "data", data: "base64...", mediaType: "audio/webm" }
```

### Plain Text
```typescript
const content = AIContentConverter.fromText("Hello world");
// Returns: { type: "text", text: "Hello world" }
```

## React Hook Example

```tsx
import { usePlayFramework } from "@rystem/playframework-client";
import { useState } from "react";

function VisionAnalysisComponent() {
    const client = usePlayFramework("default");
    const [responses, setResponses] = useState<AiSceneResponse[]>([]);

    // Register tools once
    useEffect(() => {
        const registry = client.getClientRegistry();

        registry.register("capturePhoto", async () => {
            const content = await AIContentConverter.fromCamera();
            return [content];
        });
    }, [client]);

    const analyze = async () => {
        const request = {
            prompt: "Take a photo and describe what you see",
            sceneName: "vision-analysis"
        };

        for await (const response of client.executeStepByStep(request)) {
            setResponses(prev => [...prev, response]);

            // Show UI feedback for AwaitingClient
            if (response.status === "AwaitingClient") {
                console.log("Accessing camera...");
            }
        }
    };

    return (
        <div>
            <button onClick={analyze}>Analyze with Camera</button>
            {responses.map((r, i) => (
                <div key={i}>{r.message}</div>
            ))}
        </div>
    );
}
```

## Advanced: Manual Continuation Token Handling

If you want manual control over continuation tokens (not using the automatic loop):

```typescript
let continuationToken: string | undefined;
let conversationKey: string | undefined;

for await (const response of client.executeStepByStep(request)) {
    if (response.status === "AwaitingClient" && response.clientInteractionRequest) {
        // Save token
        continuationToken = response.continuationToken;
        conversationKey = response.conversationKey;

        // Execute tool manually
        const result = await registry.execute(response.clientInteractionRequest);

        // Manually resume with new POST
        const resumeRequest: PlayFrameworkRequest = {
            continuationToken,
            clientInteractionResults: [result]
        };

        // Continue with new request
        for await (const resumeResponse of client.executeStepByStep(resumeRequest)) {
            console.log(resumeResponse);
        }

        break;
    }
}
```

## Security Considerations

1. **Timeout**: Server enforces `TimeoutSeconds` per tool (default from server config)
2. **Cache TTL**: Continuation tokens expire after `CacheExpiration` (default 5 minutes)
3. **Single Use**: Continuation tokens are deleted after use (cannot be replayed)
4. **Validation**: Server validates tool results have valid contents and no errors

## Error Handling

```typescript
registry.register("riskyTool", async () => {
    try {
        // Your tool logic
        return [content];
    } catch (error) {
        // Registry automatically wraps errors in ClientInteractionResult
        throw error; // Will be sent to server as result.error
    }
});
```

## TypeScript Types

```typescript
// Client interaction request from server
interface ClientInteractionRequest {
    interactionId: string;
    toolName: string;
    description?: string;
    arguments?: Record<string, any>;
    argumentsSchema?: string; // JSON Schema from C# type
    timeoutSeconds: number;
}

// Client interaction result to server
interface ClientInteractionResult {
    interactionId: string;
    contents: AIContent[];
    error?: string;
    executedAt: string; // ISO 8601
}

// AIContent union type
type AIContent = 
    | { type: "text"; text: string }
    | { type: "data"; data: string; mediaType: string };
```

## Best Practices

1. **Register tools early**: Call `registry.register()` during component mount
2. **Use descriptive tool names**: Match server `OnClient().AddTool("name")`
3. **Handle permissions**: Request camera/mic permissions gracefully
4. **Show feedback**: Display "Accessing camera..." during `AwaitingClient` status
5. **Test locally**: Server requires IDistributedCache (use in-memory for dev)

## Example: Complete Flow

**Server** (C#):
```csharp
scene.OnClient(c => c.AddTool("capturePhoto", "Take photo"))
     .WithService<ImageService>(s => s.AddTool(x => x.Analyze));
```

**Client** (TypeScript):
```typescript
registry.register("capturePhoto", async () => {
    const content = await AIContentConverter.fromCamera();
    return [content];
});

const request = { prompt: "Take photo and describe", sceneName: "vision" };

for await (const response of client.executeStepByStep(request)) {
    console.log(response.status); 
    // Output: "Running" → "AwaitingClient" → (camera opens) → "Running" → "Completed"
}
```

## Troubleshooting

**Error: "Tool 'X' not found in registry"**
- Ensure `registry.register("X", ...)` matches server `AddTool("X")`

**Error: "Continuation token not found or expired"**
- Check `WithCacheExpiration()` TTL (default 5 min)
- Ensure IDistributedCache is configured

**Camera not working**
- Check browser permissions (navigator.mediaDevices)
- Test in HTTPS context (required for camera/mic)

---

For more examples, see [examples/client-tools/](../examples/client-tools/)
