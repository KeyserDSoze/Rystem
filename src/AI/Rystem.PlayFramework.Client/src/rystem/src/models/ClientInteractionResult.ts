/**
 * AIContent types that can be sent to server.
 * Simplified to 2 types: DataContent (Base64) and TextContent.
 */
export interface AIContent {
    type: "text" | "data";
    text?: string;
    data?: string; // Base64 encoded
    mediaType?: string; // e.g., "image/jpeg", "audio/webm", "application/pdf"
}

/**
 * Result from client-side tool execution.
 * Client sends this back to server to resume execution.
 */
export interface ClientInteractionResult {
    /**
     * Unique interaction ID matching the request.
     */
    interactionId: string;

    /**
     * Multi-modal contents returned by tool.
     * Can include images (Base64), audio, video, files, text.
     */
    contents: AIContent[];

    /**
     * Error message if execution failed.
     */
    error?: string;

    /**
     * Timestamp when tool was executed (ISO 8601).
     */
    executedAt: string;
}
