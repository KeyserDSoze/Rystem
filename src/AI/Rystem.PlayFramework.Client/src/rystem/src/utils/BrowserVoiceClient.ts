import { PlayFrameworkClient } from "../engine/PlayFrameworkClient";
import { AiSceneResponse } from "../models/AiSceneResponse";
import { PlayFrameworkRequest } from "../models/PlayFrameworkRequest";
import { BrowserSpeechRecognizer, BrowserSpeechRecognizerOptions } from "./BrowserSpeechRecognizer";
import { BrowserSpeechSynthesizer, BrowserSpeechSynthesizerOptions, BrowserSpeechSynthesizerCallbacks } from "./BrowserSpeechSynthesizer";

/**
 * Streaming mode used for the text/LLM interaction.
 */
export type BrowserVoiceStreamingMode = 'stepByStep' | 'tokenStreaming';

/**
 * Options for executeWithBrowserVoice.
 */
export interface BrowserVoiceOptions {
    /**
     * If provided, skip speech recognition and use this text directly as the user message.
     * Useful when the caller already has the transcript (e.g. from an external recognizer).
     */
    text?: string;

    /**
     * Streaming mode for the LLM interaction.
     * - `'stepByStep'` — yields complete step responses (planning, actor actions, final message).
     * - `'tokenStreaming'` — yields individual text tokens as they arrive.
     * Default: `'stepByStep'`.
     */
    streamingMode?: BrowserVoiceStreamingMode;

    /**
     * Base PlayFramework request properties (settings, metadata, conversationKey, etc.).
     * The `message` field will be overwritten by the recognised speech text.
     */
    request?: Partial<PlayFrameworkRequest>;

    /** Timeout in ms for speech recognition (default: 15000). */
    recognitionTimeoutMs?: number;

    /** AbortSignal to cancel the entire voice flow. */
    signal?: AbortSignal;

    /**
     * If `true`, the browser TTS will speak the LLM response as it arrives.
     * Default: `true`.
     */
    speakResponse?: boolean;

    /**
     * If `false`, disables the server-side voice-style system instruction
     * that tells the LLM to respond conversationally (no tables, no markdown).
     * Useful when you want voice I/O but still want rich formatted responses.
     * Default: `true`.
     */
    useVoiceStyle?: boolean;
}

/**
 * Extra status information reported alongside `AiSceneResponse` events.
 */
export type BrowserVoiceStatus =
    | 'recognizing'     // STT is listening
    | 'recognized'      // STT transcript obtained
    | 'speaking'        // TTS is speaking a sentence
    | 'speechComplete'; // TTS finished all speech

/**
 * Event emitted by BrowserVoiceClient.executeWithBrowserVoice.
 */
export interface BrowserVoiceEvent {
    /** The original AiSceneResponse from PlayFramework (null for voice-only status events). */
    response: AiSceneResponse | null;

    /** Voice pipeline status. */
    voiceStatus?: BrowserVoiceStatus;

    /** Recognised speech text (set when voiceStatus === 'recognized'). */
    transcript?: string;
}

/**
 * Orchestrates browser-native voice I/O with the PlayFrameworkClient.
 *
 * Flow:
 * 1. **STT** — `BrowserSpeechRecognizer` captures user speech → text.
 * 2. **LLM** — Text is sent to PlayFramework via step-by-step or token streaming.
 * 3. **TTS** — `BrowserSpeechSynthesizer` speaks the LLM response in real-time.
 *
 * This gives a fully client-side voice experience (no server-side Whisper / TTS-1 needed).
 *
 * @example
 * ```ts
 * const client = new PlayFrameworkClient(settings);
 * const voice = new BrowserVoiceClient(client, { lang: 'it-IT' });
 *
 * for await (const event of voice.executeWithBrowserVoice()) {
 *     if (event.voiceStatus === 'recognized') {
 *         console.log('User said:', event.transcript);
 *     }
 *     if (event.response?.message) {
 *         console.log('AI:', event.response.message);
 *     }
 * }
 * ```
 */
export class BrowserVoiceClient {
    private readonly client: PlayFrameworkClient;
    private readonly recognizer: BrowserSpeechRecognizer;
    private readonly synthesizer: BrowserSpeechSynthesizer;

    /**
     * Internal AbortController created for each `executeWithBrowserVoice()` invocation.
     * Used by `cancelStream()` and `cancelAll()` to programmatically abort the HTTP/SSE stream.
     * Set to `null` when no voice flow is in progress.
     */
    private currentAbortController: AbortController | null = null;

    constructor(
        client: PlayFrameworkClient,
        recognizerOptions?: BrowserSpeechRecognizerOptions,
        synthesizerOptions?: BrowserSpeechSynthesizerOptions,
        synthesizerCallbacks?: BrowserSpeechSynthesizerCallbacks,
    ) {
        this.client = client;
        this.recognizer = new BrowserSpeechRecognizer(recognizerOptions);
        this.synthesizer = new BrowserSpeechSynthesizer(synthesizerOptions, synthesizerCallbacks);
    }

    /** Returns `true` if the browser supports both Speech Recognition and Speech Synthesis. */
    static isSupported(): boolean {
        return BrowserSpeechRecognizer.isSupported() && BrowserSpeechSynthesizer.isSupported();
    }

    /** Access the underlying recognizer for advanced configuration. */
    getRecognizer(): BrowserSpeechRecognizer {
        return this.recognizer;
    }

    /** Access the underlying synthesizer for advanced configuration. */
    getSynthesizer(): BrowserSpeechSynthesizer {
        return this.synthesizer;
    }

    /**
     * Execute a complete voice interaction:
     *   Speech → Text → PlayFramework (streaming) → Text → Speech
     *
     * Yields `BrowserVoiceEvent` objects with both voice status and LLM responses.
     */
    async *executeWithBrowserVoice(
        options?: BrowserVoiceOptions,
    ): AsyncIterableIterator<BrowserVoiceEvent> {
        const {
            text,
            streamingMode = 'stepByStep',
            request = {},
            recognitionTimeoutMs = 15000,
            signal,
            speakResponse = true,
            useVoiceStyle = true,
        } = options ?? {};

        // ── 0. Create internal AbortController for this invocation ─────
        //    Links to the user-provided signal so external abort propagates.
        const controller = new AbortController();
        this.currentAbortController = controller;

        if (signal) {
            if (signal.aborted) {
                controller.abort(signal.reason);
            } else {
                signal.addEventListener('abort', () => controller.abort(signal.reason), { once: true });
            }
        }

        const effectiveSignal = controller.signal;

        try {
            // ── 1. Speech Recognition (if no text provided) ────────────────
            let transcript: string;

            if (text != null) {
                transcript = text;
            } else {
                yield { response: null, voiceStatus: 'recognizing' };

                try {
                    transcript = await this.recognizer.listen(recognitionTimeoutMs);
                } catch (err) {
                    throw new Error(`Speech recognition failed: ${err instanceof Error ? err.message : String(err)}`);
                }

                if (!transcript.trim()) {
                    // No speech detected
                    return;
                }
            }

            yield { response: null, voiceStatus: 'recognized', transcript };

            // ── 2. Send text to PlayFramework via streaming ────────────────
            const fullRequest: PlayFrameworkRequest = {
                ...request,
                message: transcript,
                settings: {
                    ...request.settings,
                    isVoiceMode: useVoiceStyle,
                },
            };

            const stream = streamingMode === 'tokenStreaming'
                ? this.client.executeTokenStreaming(fullRequest, effectiveSignal)
                : this.client.executeStepByStep(fullRequest, effectiveSignal);

            // ── 3. Stream responses and optionally speak them ──────────────
            for await (const response of stream) {
                // Forward the raw LLM event
                yield { response };

                if (!speakResponse) continue;

                // For token streaming, feed chunks incrementally
                if (streamingMode === 'tokenStreaming' && response.streamingChunk) {
                    this.synthesizer.feedChunk(response.streamingChunk);
                }

                // For step-by-step, speak complete messages
                if (streamingMode === 'stepByStep' && response.message) {
                    // Speak any status that carries meaningful text; skip system/noise statuses
                    const nonSpeakableStatuses = [
                        'initializing', 'loadingCache', 'savingCache', 'savingMemory',
                        'executingMainActors', 'executingScene', 'planning',
                        'functionRequest', 'functionCompleted', 'toolSkipped',
                        'awaitingClient', 'commandClient',
                        'directorDecision', 'summarizing', 'generatingFinalResponse',
                        'error', 'budgetExceeded', 'unauthorized',
                    ];
                    if (!nonSpeakableStatuses.includes(response.status)) {
                        this.synthesizer.feedChunk(response.message);
                    }
                }
            }

            // ── 4. Flush remaining buffered text and wait for TTS to finish ─
            if (speakResponse) {
                yield { response: null, voiceStatus: 'speaking' };
                await this.synthesizer.flushAndWait();
                yield { response: null, voiceStatus: 'speechComplete' };
            }
        } finally {
            this.currentAbortController = null;
        }
    }

    /**
     * Execute a voice interaction using only browser TTS (no STT).
     * Sends the provided text via PlayFramework and speaks the response.
     *
     * Shorthand for `executeWithBrowserVoice({ text, speakResponse: true, ... })`.
     */
    async *speakResponse(
        text: string,
        streamingMode: BrowserVoiceStreamingMode = 'stepByStep',
        request?: Partial<PlayFrameworkRequest>,
        signal?: AbortSignal,
    ): AsyncIterableIterator<BrowserVoiceEvent> {
        yield* this.executeWithBrowserVoice({
            text,
            streamingMode,
            request,
            signal,
            speakResponse: true,
        });
    }

    /**
     * Cancel only the audio (TTS). The HTTP/SSE text stream continues
     * so you can still read the LLM response in your `for await` loop.
     */
    cancelSpeech(): void {
        this.synthesizer.cancel();
    }

    /** Stop recognition if active. */
    stopRecognition(): void {
        this.recognizer.stop();
    }

    /**
     * Cancel the HTTP/SSE stream (abort the API call).
     * The .NET server receives a cancelled `CancellationToken` and stops processing.
     * TTS is also cancelled since no more text will arrive.
     */
    cancelStream(): void {
        if (this.currentAbortController) {
            this.currentAbortController.abort(new DOMException('The voice stream was cancelled', 'AbortError'));
        }
        this.synthesizer.cancel();
    }

    /**
     * Cancel everything: recognition + HTTP stream + audio.
     * Equivalent to calling `stopRecognition()`, `cancelStream()`, and `cancelSpeech()` together.
     */
    cancelAll(): void {
        this.recognizer.abort();
        this.cancelStream();
    }

    /**
     * Whether a voice flow is currently in progress (stream is active).
     */
    get isStreaming(): boolean {
        return this.currentAbortController !== null;
    }
}
