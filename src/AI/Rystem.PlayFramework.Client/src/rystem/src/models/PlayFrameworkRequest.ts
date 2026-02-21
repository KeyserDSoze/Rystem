import { ContentItem } from "./ContentItem";
import { ClientInteractionResult } from "./ClientInteractionResult";

/**
 * Execution mode for the scene.
 */
export type SceneExecutionMode = "Direct" | "Planning" | "DynamicChaining" | "Scene";

/**
 * Cache behavior for the request.
 */
export type CacheBehavior = "Default" | "Avoidable" | "Forever";

/**
 * Request-specific settings matching C# SceneRequestSettings.
 */
export interface SceneRequestSettings {
    /** Maximum recursion depth for planning (default: 5). */
    maxRecursionDepth?: number;
    /** Execution mode: Direct, Planning, DynamicChaining, Scene. */
    executionMode?: SceneExecutionMode;
    /** Whether to enable summarization. */
    enableSummarization?: boolean;
    /** Whether to enable director for multi-scene orchestration. */
    enableDirector?: boolean;
    /** Model to use for this request (overrides default). */
    modelId?: string;
    /** Temperature for LLM calls (0.0 - 2.0). */
    temperature?: number;
    /** Maximum tokens to generate. */
    maxTokens?: number;
    /** Cache behavior for this request. */
    cacheBehavior?: CacheBehavior;
    /** Maximum budget for this request. Null for unlimited. */
    maxBudget?: number;
    /** Enable streaming for text responses. */
    enableStreaming?: boolean;
    /** Maximum number of scenes in dynamic chaining mode (default: 5). */
    maxDynamicScenes?: number;
    /** Unique key for this conversation (used by cache and memory). */
    conversationKey?: string;
    /** Name of the scene to execute directly (used with SceneExecutionMode "Scene"). */
    sceneName?: string;
    /** Results from client-side tool executions. */
    clientInteractionResults?: ClientInteractionResult[];
}

/**
 * Request model for PlayFramework HTTP API.
 * Matches C# PlayFrameworkRequest contract.
 */
export interface PlayFrameworkRequest {
    /**
     * User message (text).
     */
    message?: string;

    /**
     * Multi-modal content items (images, audio, video, files, URIs).
     */
    contents?: ContentItem[];

    /**
     * Custom metadata (userId, sessionId, etc.).
     */
    metadata?: Record<string, any>;

    /**
     * Request-specific settings (override defaults).
     */
    settings?: SceneRequestSettings;

    /**
     * Conversation key for resuming execution after client interaction.
     * When present, server loads conversation state from cache.
     */
    conversationKey?: string;

    /**
     * Results from client-side tool executions.
     * Used to resume execution after AwaitingClient status.
     */
    clientInteractionResults?: ClientInteractionResult[];
}
