import { PlayFrameworkRequest } from "../models/PlayFrameworkRequest";
import { AiSceneResponse, SSEEvent } from "../models/AiSceneResponse";
import { PlayFrameworkSettings } from "../servicecollection/PlayFrameworkSettings";

/**
 * PlayFramework HTTP client with step-by-step and token-level streaming support.
 */
export class PlayFrameworkClient {
    private settings: PlayFrameworkSettings;

    constructor(settings: PlayFrameworkSettings) {
        this.settings = settings;
    }

    /**
     * Execute PlayFramework with step-by-step streaming (SSE).
     * Each step (Planning, Actor execution, etc.) is yielded as it happens.
     * 
     * @param request - PlayFramework request.
     * @param signal - AbortSignal for cancellation.
     * @returns AsyncIterableIterator of AiSceneResponse steps.
     */
    public async *executeStepByStep(
        request: PlayFrameworkRequest,
        signal?: AbortSignal
    ): AsyncIterableIterator<AiSceneResponse> {
        const url = `${this.settings.baseUrl}/${this.settings.factoryName}`;
        yield* this.streamSSE(url, request, signal);
    }

    /**
     * Execute PlayFramework with token-level streaming (SSE).
     * Each text chunk is yielded as it's generated (more granular than step-by-step).
     * 
     * @param request - PlayFramework request.
     * @param signal - AbortSignal for cancellation.
     * @returns AsyncIterableIterator of AiSceneResponse chunks.
     */
    public async *executeTokenStreaming(
        request: PlayFrameworkRequest,
        signal?: AbortSignal
    ): AsyncIterableIterator<AiSceneResponse> {
        const url = `${this.settings.baseUrl}/${this.settings.factoryName}/streaming`;
        yield* this.streamSSE(url, request, signal);
    }

    /**
     * Internal SSE streaming implementation.
     */
    private async *streamSSE(
        url: string,
        request: PlayFrameworkRequest,
        signal?: AbortSignal
    ): AsyncIterableIterator<AiSceneResponse> {
        const headers = await this.settings.enrichHeaders(url, "POST", undefined, request);

        let shouldRetry = true;
        while (shouldRetry) {
            try {
                const response = await fetch(url, {
                    method: "POST",
                    headers,
                    body: JSON.stringify(request),
                    signal
                });

                if (!response.ok) {
                    throw new Error(`HTTP ${response.status}: ${response.statusText}`);
                }

                if (!response.body) {
                    throw new Error("Response body is null");
                }

                const reader = response.body.getReader();
                const decoder = new TextDecoder();
                let buffer = "";

                try {
                    while (true) {
                        const { done, value } = await reader.read();

                        if (done) {
                            break;
                        }

                        buffer += decoder.decode(value, { stream: true });

                        // Process complete SSE messages
                        const lines = buffer.split("\n");
                        buffer = lines.pop() || ""; // Keep incomplete line in buffer

                        for (const line of lines) {
                            if (line.startsWith("data: ")) {
                                const data = line.substring(6).trim();

                                if (!data) continue;

                                try {
                                    const event: SSEEvent = JSON.parse(data);

                                    // Check for completion/error markers
                                    if (event.status === "completed") {
                                        return; // End streaming
                                    }

                                    if (event.status === "error") {
                                        throw new Error((event as any).errorMessage || "Unknown error");
                                    }

                                    // Yield valid AiSceneResponse
                                    yield event as AiSceneResponse;
                                } catch (parseError) {
                                    console.error("Failed to parse SSE event:", data, parseError);
                                }
                            }
                        }
                    }
                } finally {
                    reader.releaseLock();
                }

                shouldRetry = false; // Success, no retry
            } catch (error) {
                shouldRetry = await this.settings.manageError(url, "POST", headers, request, error);

                if (!shouldRetry) {
                    throw error; // Propagate error if no retry
                }
            }
        }
    }
}
