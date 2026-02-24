/**
 * Request for client-side tool execution.
 * Server sends this when it detects a tool that must run on client (camera, geolocation, file picker, etc.).
 */
export interface ClientInteractionRequest {
    /**
     * Unique interaction ID (GUID).
     */
    interactionId: string;

    /**
     * Tool name registered via OnClient().
     */
    toolName: string;

    /**
     * Human-readable description.
     */
    description?: string;

    /**
     * Arguments from LLM (if any).
     * Type is validated against argumentsSchema.
     */
    arguments?: Record<string, any>;

    /**
     * JSON Schema of arguments for validation.
     * Generated from C# type T via System.Text.Json introspection.
     */
    argumentsSchema?: string;

    /**
     * Maximum execution time in seconds.
     * For standard tools: timeout for client response.
     * For commands: timeout for client-side execution (protects against crashes/hangs).
     */
    timeoutSeconds: number;

    /**
     * Indicates if this is a command (fire-and-forget tool).
     * Commands don't require immediate response - auto-completed with 'true' on next user message.
     * Client can optionally send CommandResult (success + message) based on feedbackMode.
     */
    isCommand?: boolean;
}
