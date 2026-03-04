/**
 * Options for BrowserSpeechRecognizer.
 */
export interface BrowserSpeechRecognizerOptions {
    /** BCP-47 language code (e.g. 'it-IT', 'en-US'). If omitted, the browser's default is used. */
    lang?: string;
    /** Whether to return interim (partial) results. Default: `true`. */
    interimResults?: boolean;
    /** Whether recognition restarts automatically on end. Default: `false`. */
    continuous?: boolean;
    /** Maximum alternatives per result. Default: `1`. */
    maxAlternatives?: number;
}

/**
 * Result from a speech recognition session.
 */
export interface SpeechRecognitionResult {
    /** The final transcribed text. */
    transcript: string;
    /** Confidence score (0-1). */
    confidence: number;
    /** Whether this is a final (not interim) result. */
    isFinal: boolean;
}

/**
 * Callbacks emitted by BrowserSpeechRecognizer.
 */
export interface BrowserSpeechRecognizerCallbacks {
    /** Called with each interim or final result. */
    onResult?: (result: SpeechRecognitionResult) => void;
    /** Called when recognition ends (user stopped or no more speech). */
    onEnd?: () => void;
    /** Called on recognition error. */
    onError?: (error: Error) => void;
    /** Called when recognition starts. */
    onStart?: () => void;
}

// Type declarations for Web Speech API (not always in TS lib)
interface SpeechRecognitionEvent extends Event {
    results: SpeechRecognitionResultList;
    resultIndex: number;
}

interface SpeechRecognitionErrorEvent extends Event {
    error: string;
    message: string;
}

interface SpeechRecognitionResultList {
    length: number;
    item(index: number): SpeechRecognitionResultItem;
    [index: number]: SpeechRecognitionResultItem;
}

interface SpeechRecognitionResultItem {
    isFinal: boolean;
    length: number;
    item(index: number): SpeechRecognitionAlternative;
    [index: number]: SpeechRecognitionAlternative;
}

interface SpeechRecognitionAlternative {
    transcript: string;
    confidence: number;
}

interface SpeechRecognitionAPI extends EventTarget {
    lang: string;
    continuous: boolean;
    interimResults: boolean;
    maxAlternatives: number;
    start(): void;
    stop(): void;
    abort(): void;
    onresult: ((event: SpeechRecognitionEvent) => void) | null;
    onend: (() => void) | null;
    onerror: ((event: SpeechRecognitionErrorEvent) => void) | null;
    onstart: (() => void) | null;
}

declare global {
    interface Window {
        SpeechRecognition?: new () => SpeechRecognitionAPI;
        webkitSpeechRecognition?: new () => SpeechRecognitionAPI;
    }
}

/**
 * Wraps the browser Web Speech API `SpeechRecognition` for speech-to-text.
 *
 * @example
 * ```ts
 * const recognizer = new BrowserSpeechRecognizer({ lang: 'it-IT' });
 *
 * // One-shot usage — returns final transcript as a Promise
 * const text = await recognizer.listen();
 *
 * // Event-based usage
 * recognizer.start({
 *     onResult: (r) => console.log(r.transcript, r.isFinal),
 *     onEnd: () => console.log('done'),
 * });
 * // later...
 * recognizer.stop();
 * ```
 */
export class BrowserSpeechRecognizer {
    private options: Required<BrowserSpeechRecognizerOptions>;
    private recognition: SpeechRecognitionAPI | null = null;
    private _isListening = false;

    constructor(options?: BrowserSpeechRecognizerOptions) {
        this.options = {
            lang: options?.lang ?? '',
            interimResults: options?.interimResults ?? true,
            continuous: options?.continuous ?? false,
            maxAlternatives: options?.maxAlternatives ?? 1,
        };
    }

    /** Whether recognition is currently active. */
    get isListening(): boolean {
        return this._isListening;
    }

    /** Returns `true` if the browser supports the Web Speech Recognition API. */
    static isSupported(): boolean {
        return typeof window !== 'undefined' &&
            !!(window.SpeechRecognition || window.webkitSpeechRecognition);
    }

    /**
     * Start recognition with event callbacks.
     * If already listening, this is a no-op.
     */
    start(callbacks?: BrowserSpeechRecognizerCallbacks): void {
        if (this._isListening) return;

        const SpeechRecognitionCtor = window.SpeechRecognition || window.webkitSpeechRecognition;
        if (!SpeechRecognitionCtor) {
            callbacks?.onError?.(new Error('SpeechRecognition API is not supported in this browser.'));
            return;
        }

        const recognition = new SpeechRecognitionCtor();
        this.recognition = recognition;

        if (this.options.lang) recognition.lang = this.options.lang;
        recognition.continuous = this.options.continuous;
        recognition.interimResults = this.options.interimResults;
        recognition.maxAlternatives = this.options.maxAlternatives;

        recognition.onstart = () => {
            this._isListening = true;
            callbacks?.onStart?.();
        };

        recognition.onresult = (event: SpeechRecognitionEvent) => {
            for (let i = event.resultIndex; i < event.results.length; i++) {
                const result = event.results[i];
                const alt = result[0];
                callbacks?.onResult?.({
                    transcript: alt.transcript,
                    confidence: alt.confidence,
                    isFinal: result.isFinal,
                });
            }
        };

        recognition.onerror = (event: SpeechRecognitionErrorEvent) => {
            // 'no-speech' and 'aborted' are common non-fatal errors
            const nonFatal = ['no-speech', 'aborted'];
            if (!nonFatal.includes(event.error)) {
                callbacks?.onError?.(new Error(`SpeechRecognition error: ${event.error} — ${event.message}`));
            }
        };

        recognition.onend = () => {
            this._isListening = false;
            this.recognition = null;
            callbacks?.onEnd?.();
        };

        recognition.start();
    }

    /** Stop recognition gracefully (delivers final result). */
    stop(): void {
        if (this.recognition) {
            this.recognition.stop();
        }
    }

    /** Abort recognition immediately (no final result). */
    abort(): void {
        if (this.recognition) {
            this.recognition.abort();
            this._isListening = false;
            this.recognition = null;
        }
    }

    /**
     * One-shot recognition: starts listening and returns a Promise that resolves
     * with the final transcript when speech ends.
     *
     * @param timeoutMs - Maximum wait time (default: 15000ms). Resolves with '' on timeout.
     */
    listen(timeoutMs: number = 15000): Promise<string> {
        return new Promise<string>((resolve, reject) => {
            let finalTranscript = '';
            let timer: ReturnType<typeof setTimeout> | null = null;

            const cleanup = () => {
                if (timer) {
                    clearTimeout(timer);
                    timer = null;
                }
            };

            if (timeoutMs > 0) {
                timer = setTimeout(() => {
                    this.stop();
                    // onEnd will fire and resolve with whatever we have
                }, timeoutMs);
            }

            this.start({
                onResult: (result) => {
                    if (result.isFinal) {
                        finalTranscript += result.transcript;
                    }
                },
                onEnd: () => {
                    cleanup();
                    resolve(finalTranscript);
                },
                onError: (err) => {
                    cleanup();
                    reject(err);
                },
            });
        });
    }

    /** Update language at runtime (takes effect on next `start()`). */
    setLang(lang: string): void {
        this.options.lang = lang;
    }
}
