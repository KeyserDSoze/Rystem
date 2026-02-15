/**
 * Response status from PlayFramework execution.
 */
export type AiResponseStatus = "Planning" | "Running" | "Error" | "Completed" | "AwaitingClient";

/**
 * Single step response from PlayFramework (used in step-by-step streaming).
 */
export interface AiSceneResponse {
    /**
     * Execution status.
     */
    status: AiResponseStatus;

    /**
     * Response message/text.
     */
    message?: string;

    /**
     * Error message (if status==="Error").
     */
    errorMessage?: string;

    /**
     * Total tokens used so far.
     */
    totalTokens?: number;

    /**
     * Total cost so far.
     */
    totalCost?: number;

    /**
     * Metadata from this step.
     */
    metadata?: Record<string, any>;

    /**
     * Tool calls executed in this step.
     */
    toolCalls?: any[];

    /**
     * Conversation key for resuming execution.
     */
    conversationKey?: string;

    /**
     * Continuation token for resuming after client interaction.
     * Present when status === "AwaitingClient".
     */
    continuationToken?: string;

    /**
     * Client interaction request when status === "AwaitingClient".
     * Contains tool name, arguments, and schema for client execution.
     */
    clientInteractionRequest?: {
        interactionId: string;
        toolName: string;
        description?: string;
        arguments?: Record<string, any>;
        argumentsSchema?: string;
        timeoutSeconds: number;
    };
}

/**
 * Completion marker for SSE streaming.
 */
export interface CompletionMarker {
    status: "completed";
}

/**
 * Error marker for SSE streaming.
 */
export interface ErrorMarker {
    status: "error";
    errorMessage: string;
}

/**
 * SSE event types.
 */
export type SSEEvent = AiSceneResponse | CompletionMarker | ErrorMarker;
