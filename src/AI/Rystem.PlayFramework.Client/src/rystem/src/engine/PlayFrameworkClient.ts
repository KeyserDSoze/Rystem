import { PlayFrameworkRequest } from "../models/PlayFrameworkRequest";
import { AiSceneResponse, SSEEvent, ErrorMarker } from "../models/AiSceneResponse";
import { PlayFrameworkSettings } from "../servicecollection/PlayFrameworkSettings";
import { ClientInteractionRegistry } from "./ClientInteractionRegistry";
import { ClientInteractionResult } from "../models/ClientInteractionResult";

/**
 * PlayFramework HTTP client with step-by-step and token-level streaming support.
 * Supports client-side tool execution with continuation tokens.
 */
export class PlayFrameworkClient {
    private settings: PlayFrameworkSettings;
    private clientRegistry: ClientInteractionRegistry;

    constructor(settings: PlayFrameworkSettings, clientRegistry?: ClientInteractionRegistry) {
        this.settings = settings;
        this.clientRegistry = clientRegistry || new ClientInteractionRegistry();
    }

    /**
     * Gets the client interaction registry for registering client-side tools.
     */
    public getClientRegistry(): ClientInteractionRegistry {
        return this.clientRegistry;
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
     * Creates an AbortSignal combining user signal and timeout.
     * Uses AbortController to combine signals for broad compatibility.
     */
    private createTimeoutSignal(signal?: AbortSignal): AbortSignal | undefined {
        const hasTimeout = this.settings.timeout > 0;

        if (signal && hasTimeout) {
            // Combine user signal + timeout via AbortController
            const controller = new AbortController();
            const timer = setTimeout(() => controller.abort(new DOMException('The operation was aborted due to timeout', 'TimeoutError')), this.settings.timeout);

            signal.addEventListener('abort', () => {
                clearTimeout(timer);
                controller.abort(signal.reason);
            }, { once: true });

            // Clean up timer if our combined signal is aborted
            controller.signal.addEventListener('abort', () => clearTimeout(timer), { once: true });

            return controller.signal;
        }
        if (signal) return signal;
        if (hasTimeout) return AbortSignal.timeout(this.settings.timeout);
        return undefined;
    }

    /**
     * Internal SSE streaming implementation with continuation token support
     * and automatic reconnection on network failures.
     */
    private async *streamSSE(
        url: string,
        request: PlayFrameworkRequest,
        signal?: AbortSignal
    ): AsyncIterableIterator<AiSceneResponse> {
        let currentRequest = { ...request };
        let shouldContinue = true;
        const effectiveSignal = this.createTimeoutSignal(signal);

        while (shouldContinue) {
            const headers = await this.settings.enrichHeaders(url, "POST", undefined, currentRequest);
            let reconnectAttempts = 0;

            let shouldRetry = true;
            while (shouldRetry) {
                try {
                    const response = await fetch(url, {
                        method: "POST",
                        headers,
                        body: JSON.stringify(currentRequest),
                        signal: effectiveSignal || undefined
                    });

                    if (!response.ok) {
                        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
                    }

                    if (!response.body) {
                        throw new Error("Response body is null");
                    }

                    // Reset reconnect counter on successful connection
                    reconnectAttempts = 0;

                    const reader = response.body.getReader();
                    const decoder = new TextDecoder();
                    let buffer = "";

                    let awaitingClientResponse: AiSceneResponse | null = null;

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

                                    let event: SSEEvent;
                                    try {
                                        event = JSON.parse(data);
                                    } catch {
                                        console.error("Failed to parse SSE event:", data);
                                        continue;
                                    }

                                    // Check for completion/error markers
                                    if (event.status === "completed") {
                                        shouldContinue = false;
                                        return; // End streaming
                                    }

                                    if (event.status === "error") {
                                        shouldContinue = false;
                                        throw new Error((event as ErrorMarker).errorMessage || "Unknown error");
                                    }

                                    // Check for AwaitingClient status
                                    if (event.status === "AwaitingClient") {
                                        awaitingClientResponse = event as AiSceneResponse;
                                        yield awaitingClientResponse; // Yield to user
                                        break; // Exit SSE loop to execute client tool
                                    }

                                    // Yield valid AiSceneResponse
                                    yield event as AiSceneResponse;
                                }
                            }

                            // Break out of read loop if AwaitingClient
                            if (awaitingClientResponse) {
                                break;
                            }
                        }
                    } finally {
                        reader.releaseLock();
                    }

                    shouldRetry = false; // Success, no retry

                    // Handle client interaction if AwaitingClient
                    if (awaitingClientResponse?.clientInteractionRequest) {
                        const clientRequest = awaitingClientResponse.clientInteractionRequest;

                        // Execute client tool
                        const result = await this.clientRegistry.execute(clientRequest);

                        // Prepare new request with client interaction results
                        // The server uses conversationKey + cache to restore context
                        currentRequest = {
                            ...currentRequest,
                            clientInteractionResults: [result]
                        };

                        // Continue loop to resume execution
                        shouldContinue = true;
                    } else {
                        shouldContinue = false; // Normal completion
                    }
                } catch (error) {
                    // Check if this is a network/connection error (not abort, not server error marker)
                    const isAbort = error instanceof DOMException && error.name === "AbortError";
                    const isServerError = error instanceof Error && (
                        error.message.startsWith("HTTP ") || error.message === "Unknown error"
                    );

                    if (!isAbort && !isServerError && reconnectAttempts < this.settings.maxReconnectAttempts) {
                        reconnectAttempts++;
                        const delay = this.settings.reconnectBaseDelay * Math.pow(2, reconnectAttempts - 1);
                        console.warn(
                            `SSE connection lost. Reconnecting (attempt ${reconnectAttempts}/${this.settings.maxReconnectAttempts}) in ${delay}ms...`
                        );
                        await new Promise(resolve => setTimeout(resolve, delay));
                        continue; // retry the fetch
                    }

                    // Delegate to error handlers for custom retry logic
                    shouldRetry = await this.settings.manageError(url, "POST", headers, currentRequest, error);

                    if (!shouldRetry) {
                        shouldContinue = false;
                        throw error; // Propagate error if no retry
                    }
                }
            }
        }
    }
}