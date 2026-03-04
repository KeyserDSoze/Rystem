import { AiSceneResponse } from "./AiSceneResponse";

/**
 * Type of voice pipeline response event.
 */
export type VoiceResponseType = "transcription" | "audio" | "scene" | "completed" | "error";

/**
 * Single event from the voice pipeline SSE stream.
 * The server sends these as `data: {...}\n\n` lines.
 */
export interface VoiceEvent {
    /** Event type discriminator. */
    type: VoiceResponseType;

    /** Text content (transcript for "transcription", synthesized sentence for "audio"). */
    text?: string;

    /** Base64-encoded audio chunk (only for type === "audio"). */
    audio?: string;

    /** PlayFramework scene status (only for type === "scene"). */
    status?: string;

    /** Full scene response (only for type === "scene"). */
    sceneResponse?: AiSceneResponse;

    /** Scene message (only for type === "scene"). */
    message?: string;

    /** Error message (only for type === "error"). */
    errorMessage?: string;
}

/**
 * Options for the voice pipeline request.
 */
export interface VoiceRequestOptions {
    /** Raw audio data as Blob or File. */
    audio: Blob | File;

    /** Optional conversation key for multi-turn voice conversations. */
    conversationKey?: string;

    /** Optional metadata (userId, tenantId, etc.). */
    metadata?: Record<string, any>;

    /** Optional abort signal. */
    signal?: AbortSignal;
}
