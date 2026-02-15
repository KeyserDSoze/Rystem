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
     */
    timeoutSeconds: number;
}
