import { ContentItem } from "./ContentItem";

/**
 * Request model for PlayFramework HTTP API.
 */
export interface PlayFrameworkRequest {
    /**
     * User prompt/message.
     */
    prompt?: string;

    /**
     * Scene name to execute.
     */
    sceneName?: string;

    /**
     * Multi-modal content items (images, audio, video, files, URIs).
     */
    contents?: ContentItem[];

    /**
     * Custom metadata (userId, sessionId, etc.).
     */
    metadata?: Record<string, any>;

    /**
     * Override PlayFramework settings for this request.
     */
    settings?: {
        temperature?: number;
        maxTokens?: number;
        [key: string]: any;
    };

    /**
     * Continuation token for resuming execution after client interaction.
     * Send this when client has executed tool and wants to resume.
     */
    continuationToken?: string;

    /**
     * Results from client-side tool executions.
     * Required when resuming with continuationToken.
     */
    clientInteractionResults?: Array<{
        interactionId: string;
        contents: Array<{
            type: "text" | "data";
            text?: string;
            data?: string; // Base64
            mediaType?: string;
        }>;
        error?: string;
        executedAt: string; // ISO 8601
    }>;
}
