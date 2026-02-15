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
}
