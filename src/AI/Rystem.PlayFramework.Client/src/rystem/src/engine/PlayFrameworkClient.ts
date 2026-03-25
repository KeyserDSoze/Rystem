import { PlayFrameworkRequest } from "../models/PlayFrameworkRequest";
import { AiSceneResponse, SSEEvent, ErrorMarker } from "../models/AiSceneResponse";
import { PlayFrameworkSettings } from "../servicecollection/PlayFrameworkSettings";
import { ClientInteractionRegistry } from "./ClientInteractionRegistry";
import { ClientInteractionResult } from "../models/ClientInteractionResult";
import type { StoredConversation, ConversationQueryParameters, UpdateConversationVisibilityRequest } from "../models/StoredConversation";
import type { VoiceEvent, VoiceRequestOptions } from "../models/VoiceResponse";
import type { PlayFrameworkDiscoveryResponse } from "../models/PlayFrameworkDiscovery";

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
            // Collect ALL client interaction responses (server may send multiple in a single batch)
            const pendingInteractions: AiSceneResponse[] = [];
            let capturedConversationKey: string | undefined;

            let shouldRetry = true;
            while (shouldRetry) {
                try {
                    const response = await this.settings.getFetch()(url, {
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
                                        // Don't set shouldContinue = false yet - check if client interactions needed first
                                        shouldRetry = false; // Don't retry, stream closed successfully
                                        break; // Exit read loop to handle client interactions if needed
                                    }

                                    if (event.status === "error") {
                                        shouldContinue = false;
                                        shouldRetry = false; // Don't retry on error
                                        throw new Error((event as ErrorMarker).errorMessage || "Unknown error");
                                    }

                                    // Check for AwaitingClient or CommandClient status (camelCase from JsonStringEnumConverter)
                                    if (event.status === "awaitingClient" || event.status === "commandClient") {
                                        const interactionResponse = event as AiSceneResponse;
                                        const isCommand = event.status === "commandClient";
                                        console.log(isCommand ? '[PlayFrameworkClient] Received CommandClient (fire-and-forget)' : '[PlayFrameworkClient] Received AwaitingClient', ', tool:', interactionResponse.clientInteractionRequest?.toolName);

                                        // Capture conversationKey from the response (server sets it for resume)
                                        if (interactionResponse.conversationKey) {
                                            capturedConversationKey = interactionResponse.conversationKey;
                                        }

                                        // Collect ALL interactions (don't overwrite — the server may send multiple)
                                        pendingInteractions.push(interactionResponse);

                                        yield interactionResponse; // Yield to user
                                        // Don't break - continue reading until stream closes
                                        continue; // Skip the second yield below to avoid duplication
                                    }

                                    // Yield valid AiSceneResponse
                                    yield event as AiSceneResponse;
                                }
                            }

                            // Continue reading stream until "completed" event - don't exit early
                            // even if AwaitingClient was received (stream must close properly)
                        }
                    } finally {
                        reader.releaseLock();
                    }

                    shouldRetry = false; // Success, no retry

                    // Handle client interactions if any were received during this stream
                    if (shouldContinue && pendingInteractions.length > 0) {
                        console.log(`[PlayFrameworkClient] Executing ${pendingInteractions.length} client interaction(s)`);

                        // Execute ALL pending interactions in parallel
                        const allResults: ClientInteractionResult[] = [];
                        let hasNonCommandInteraction = false;

                        for (const interaction of pendingInteractions) {
                            const clientRequest = interaction.clientInteractionRequest;
                            if (!clientRequest) continue;

                            console.log(`[PlayFrameworkClient] Executing: ${clientRequest.toolName} (${clientRequest.isCommand ? 'command' : 'tool'})`);

                            const result = await this.clientRegistry.execute(clientRequest);

                            if (clientRequest.isCommand === true) {
                                // Command: check feedbackMode to decide whether to send result
                                const feedbackMode = clientRequest.feedbackMode ?? 'onError';

                                if (feedbackMode === 'always') {
                                    allResults.push(result);
                                } else if (feedbackMode === 'onError' && result.error) {
                                    allResults.push(result);
                                }
                                // feedbackMode === 'never': don't send result
                            } else {
                                // Tool (AwaitingClient): always send result
                                hasNonCommandInteraction = true;
                                allResults.push(result);
                            }
                        }

                        if (hasNonCommandInteraction || allResults.length > 0) {
                            console.log(`[PlayFrameworkClient] ${allResults.length} result(s) to send back, resuming`);

                            // Prepare resume request with ALL applicable results
                            // Use captured conversationKey so the server finds the batch in cache
                            currentRequest = {
                                ...currentRequest,
                                clientInteractionResults: allResults,
                                conversationKey: capturedConversationKey ?? currentRequest.conversationKey
                            };

                            console.log('[PlayFrameworkClient] Sending resume request with clientInteractionResults');
                            shouldContinue = true;
                        } else {
                            console.log('[PlayFrameworkClient] All interactions were fire-and-forget commands. No resume needed.');
                            shouldContinue = false;
                        }
                    } else {
                        console.log('[PlayFrameworkClient] Stream completed normally (no client interaction)');
                        shouldContinue = false; // Normal completion or already closed by completed/error
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

    // ─── Conversation Management Methods ────────────────────────────────────────

    /**
     * Returns scenes, tools, services, and MCP sources exposed by the selected factory.
     */
    public async getDiscovery(signal?: AbortSignal): Promise<PlayFrameworkDiscoveryResponse> {
        const url = `${this.settings.baseUrl}/${this.settings.factoryName}/discovery`;
        const headers = await this.settings.enrichHeaders(url, "GET", undefined, undefined);
        const effectiveSignal = this.createTimeoutSignal(signal);

        const response = await this.settings.getFetch()(url, {
            method: "GET",
            headers,
            signal: effectiveSignal
        });

        if (!response.ok) {
            throw new Error(`Failed to get discovery metadata: ${response.statusText}`);
        }

        return await response.json();
    }

    /**
     * Lists conversations with filtering, sorting, and pagination.
     * 
     * @param params - Query parameters for filtering and pagination.
     * @param signal - AbortSignal for cancellation.
     * @returns Promise<StoredConversation[]>
     */
    public async listConversations(
        params?: ConversationQueryParameters,
        signal?: AbortSignal
    ): Promise<StoredConversation[]> {
        const queryString = this.buildQueryString(params || {});
        const url = `${this.settings.baseUrl}/${this.settings.factoryName}/conversations${queryString}`;
        const headers = await this.settings.enrichHeaders(url, "GET", undefined, undefined);
        const effectiveSignal = this.createTimeoutSignal(signal);

        const response = await this.settings.getFetch()(url, {
            method: "GET",
            headers,
            signal: effectiveSignal
        });

        if (!response.ok) {
            throw new Error(`Failed to list conversations: ${response.statusText}`);
        }

        return await response.json();
    }

    /**
     * Gets a single conversation by key.
     * 
     * @param conversationKey - The conversation key to retrieve.
     * @param includeContents - Whether to include message contents (images, audio, PDFs). Default: false.
     * @param signal - AbortSignal for cancellation.
     * @returns Promise<StoredConversation | null>
     */
    public async getConversation(
        conversationKey: string,
        includeContents: boolean = false,
        signal?: AbortSignal
    ): Promise<StoredConversation | null> {
        const queryString = includeContents ? '?includeContents=true' : '';
        const url = `${this.settings.baseUrl}/${this.settings.factoryName}/conversations/${conversationKey}${queryString}`;
        const headers = await this.settings.enrichHeaders(url, "GET", undefined, undefined);
        const effectiveSignal = this.createTimeoutSignal(signal);

        const response = await this.settings.getFetch()(url, {
            method: "GET",
            headers,
            signal: effectiveSignal
        });

        if (response.status === 404) {
            return null;
        }

        if (!response.ok) {
            throw new Error(`Failed to get conversation: ${response.statusText}`);
        }

        return await response.json();
    }

    /**
     * Deletes a conversation (owner only).
     * 
     * @param conversationKey - The conversation key to delete.
     * @param signal - AbortSignal for cancellation.
     * @returns Promise<void>
     */
    public async deleteConversation(
        conversationKey: string,
        signal?: AbortSignal
    ): Promise<void> {
        const url = `${this.settings.baseUrl}/${this.settings.factoryName}/conversations/${conversationKey}`;
        const headers = await this.settings.enrichHeaders(url, "DELETE", undefined, undefined);
        const effectiveSignal = this.createTimeoutSignal(signal);

        const response = await this.settings.getFetch()(url, {
            method: "DELETE",
            headers,
            signal: effectiveSignal
        });

        if (!response.ok) {
            throw new Error(`Failed to delete conversation: ${response.statusText}`);
        }
    }

    /**
     * Updates conversation visibility (public/private).
     * 
     * @param conversationKey - The conversation key to update.
     * @param isPublic - Whether the conversation should be public.
     * @param signal - AbortSignal for cancellation.
     * @returns Promise<StoredConversation>
     */
    public async updateConversationVisibility(
        conversationKey: string,
        isPublic: boolean,
        signal?: AbortSignal
    ): Promise<StoredConversation> {
        const url = `${this.settings.baseUrl}/${this.settings.factoryName}/conversations/${conversationKey}/visibility`;
        const body: UpdateConversationVisibilityRequest = { isPublic };
        const headers = await this.settings.enrichHeaders(url, "PATCH", undefined, body);
        const effectiveSignal = this.createTimeoutSignal(signal);

        const response = await this.settings.getFetch()(url, {
            method: "PATCH",
            headers,
            body: JSON.stringify(body),
            signal: effectiveSignal
        });

        if (!response.ok) {
            throw new Error(`Failed to update conversation visibility: ${response.statusText}`);
        }

        return await response.json();
    }

    /**
     * Helper to build query string from parameters.
     */
    private buildQueryString(params: ConversationQueryParameters): string {
        const queryParams: string[] = [];

        if (params.searchText) {
            queryParams.push(`searchText=${encodeURIComponent(params.searchText)}`);
        }

        // OrderBy - default to TimestampDescending (0) if not provided
        const orderBy = params.orderBy !== undefined ? params.orderBy : 0;
        queryParams.push(`orderBy=${orderBy}`);

        // IncludePublic - default to true if not provided
        const includePublic = params.includePublic !== undefined ? params.includePublic : true;
        queryParams.push(`includePublic=${includePublic}`);

        // IncludePrivate - default to true if not provided
        const includePrivate = params.includePrivate !== undefined ? params.includePrivate : true;
        queryParams.push(`includePrivate=${includePrivate}`);

        // Skip - default to 0 if not provided (REQUIRED by backend)
        const skip = params.skip !== undefined ? params.skip : 0;
        queryParams.push(`skip=${skip}`);

        // Take - default to 50 if not provided
        const take = params.take !== undefined ? params.take : 50;
        queryParams.push(`take=${take}`);

        // IncludeContents - default to false if not provided
        const includeContents = params.includeContents !== undefined ? params.includeContents : false;
        queryParams.push(`includeContents=${includeContents}`);

        return queryParams.length > 0 ? `?${queryParams.join('&')}` : '';
    }

    // ─── Voice Pipeline Methods ──────────────────────────────────────────────────

    /**
     * Execute the voice pipeline: audio → STT → PlayFramework → TTS → audio streaming.
     * Sends audio as multipart/form-data, receives SSE stream of VoiceEvent.
     *
     * @param options - Voice request options (audio blob, conversationKey, metadata).
     * @returns AsyncIterableIterator of VoiceEvent (transcription, audio chunks, scene events, completion).
     *
     * @example
     * ```ts
     * const audioBlob = await recordAudio();
     * for await (const event of client.executeVoice({ audio: audioBlob })) {
     *   if (event.type === 'transcription') console.log('User said:', event.text);
     *   if (event.type === 'audio') playAudioBase64(event.audio!);
     *   if (event.type === 'scene') console.log('Scene:', event.status, event.message);
     * }
     * ```
     */
    public async *executeVoice(
        options: VoiceRequestOptions
    ): AsyncIterableIterator<VoiceEvent> {
        const url = `${this.settings.baseUrl}/${this.settings.factoryName}/voice`;
        const effectiveSignal = this.createTimeoutSignal(options.signal);

        // Build multipart/form-data
        const formData = new FormData();
        const audioFile = options.audio instanceof File
            ? options.audio
            : new File([options.audio], 'recording.webm', { type: options.audio.type || 'audio/webm' });
        formData.append('audio', audioFile);

        if (options.conversationKey) {
            formData.append('conversationKey', options.conversationKey);
        }

        if (options.metadata) {
            formData.append('metadata', JSON.stringify(options.metadata));
        }

        // Build headers WITHOUT Content-Type (browser sets multipart boundary automatically)
        const baseHeaders = await this.settings.enrichHeaders(url, "POST", undefined, undefined);
        const headers = new Headers(baseHeaders);
        headers.delete('Content-Type'); // Let browser set multipart boundary

        const response = await this.settings.getFetch()(url, {
            method: 'POST',
            headers,
            body: formData,
            signal: effectiveSignal || undefined
        });

        if (!response.ok) {
            const errorText = await response.text().catch(() => response.statusText);
            throw new Error(`Voice pipeline error (${response.status}): ${errorText}`);
        }

        if (!response.body) {
            throw new Error('Response body is null');
        }

        // Parse SSE stream
        const reader = response.body.getReader();
        const decoder = new TextDecoder();
        let buffer = '';

        try {
            while (true) {
                const { done, value } = await reader.read();
                if (done) break;

                buffer += decoder.decode(value, { stream: true });
                const lines = buffer.split('\n');
                buffer = lines.pop() || '';

                for (const line of lines) {
                    if (line.startsWith('data: ')) {
                        const data = line.substring(6).trim();
                        if (!data) continue;

                        let event: VoiceEvent;
                        try {
                            event = JSON.parse(data);
                        } catch {
                            console.error('Failed to parse voice SSE event:', data);
                            continue;
                        }

                        yield event;

                        // Stop on completed or error
                        if (event.type === 'completed' || event.type === 'error') {
                            return;
                        }
                    }
                }
            }
        } finally {
            reader.releaseLock();
        }
    }
}
