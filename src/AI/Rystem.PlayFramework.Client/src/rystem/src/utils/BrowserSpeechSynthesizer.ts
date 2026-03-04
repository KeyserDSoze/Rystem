/**
 * Options for BrowserSpeechSynthesizer.
 */
export interface BrowserSpeechSynthesizerOptions {
    /** BCP-47 language code (e.g. 'it-IT', 'en-US'). Default: browser default. */
    lang?: string;
    /** Speech rate (0.1 - 10). Default: `1`. */
    rate?: number;
    /** Pitch (0 - 2). Default: `1`. */
    pitch?: number;
    /** Volume (0 - 1). Default: `1`. */
    volume?: number;
    /** Preferred voice name (e.g. 'Google italiano'). If not found, uses first matching lang voice. */
    voiceName?: string;
    /**
     * Sentence delimiters used to split streaming text into speakable chunks.
     * Default: `['.', '!', '?', '\n']`.
     */
    sentenceDelimiters?: string[];
    /**
     * Minimum character count before flushing a sentence for speech.
     * Prevents speaking very short fragments. Default: `20`.
     */
    minCharsBeforeSpeak?: number;
}

/**
 * Callbacks emitted by BrowserSpeechSynthesizer.
 */
export interface BrowserSpeechSynthesizerCallbacks {
    /** Called when a sentence starts being spoken. */
    onSpeakStart?: (text: string) => void;
    /** Called when a sentence finishes being spoken. */
    onSpeakEnd?: (text: string) => void;
    /** Called when all queued speech has been spoken. */
    onQueueEmpty?: () => void;
    /** Called on speech error. */
    onError?: (error: Error) => void;
}

/**
 * Wraps the browser Web Speech API `SpeechSynthesis` for text-to-speech,
 * with built-in sentence accumulation for streaming scenarios.
 *
 * @example
 * ```ts
 * const synth = new BrowserSpeechSynthesizer({ lang: 'it-IT', rate: 1.1 });
 *
 * // Simple usage
 * await synth.speak("Ciao, come stai?");
 *
 * // Streaming usage — feed text chunks as they arrive
 * synth.feedChunk("Ciao, ");
 * synth.feedChunk("come stai? ");
 * synth.feedChunk("Tutto bene.");
 * synth.flush(); // speak any remaining text
 * ```
 */
export class BrowserSpeechSynthesizer {
    private options: Required<BrowserSpeechSynthesizerOptions>;
    private callbacks: BrowserSpeechSynthesizerCallbacks = {};
    private buffer = '';
    private queue: string[] = [];
    private _isSpeaking = false;
    private _isProcessingQueue = false;
    private resolvedVoice: SpeechSynthesisVoice | null = null;
    private voiceResolved = false;
    private warmedUp = false;

    constructor(options?: BrowserSpeechSynthesizerOptions, callbacks?: BrowserSpeechSynthesizerCallbacks) {
        this.options = {
            lang: options?.lang ?? '',
            rate: options?.rate ?? 1,
            pitch: options?.pitch ?? 1,
            volume: options?.volume ?? 1,
            voiceName: options?.voiceName ?? '',
            sentenceDelimiters: options?.sentenceDelimiters ?? ['.', '!', '?', '\n'],
            minCharsBeforeSpeak: options?.minCharsBeforeSpeak ?? 20,
        };
        if (callbacks) this.callbacks = callbacks;

        // Pre-load voices (Chrome loads them asynchronously)
        if (BrowserSpeechSynthesizer.isSupported()) {
            window.speechSynthesis.getVoices(); // trigger async load
            window.speechSynthesis.addEventListener?.('voiceschanged', () => {
                // Only re-resolve if we haven't successfully found a voice yet.
                // Chrome fires this event multiple times as network voices load
                // incrementally; if we already have a match, keep it stable to
                // avoid language flipping mid-stream.
                if (!this.resolvedVoice) {
                    this.voiceResolved = false;
                }
            });
        }
    }

    /** Whether speech is currently being synthesized. */
    get isSpeaking(): boolean {
        return this._isSpeaking;
    }

    /** Returns `true` if the browser supports the Web Speech Synthesis API. */
    static isSupported(): boolean {
        return typeof window !== 'undefined' && 'speechSynthesis' in window;
    }

    /** Set event callbacks. */
    setCallbacks(callbacks: BrowserSpeechSynthesizerCallbacks): void {
        this.callbacks = callbacks;
    }

    /** Update language at runtime. Resets the resolved voice cache. */
    setLang(lang: string): void {
        this.options.lang = lang;
        this.voiceResolved = false;
        this.resolvedVoice = null;
    }

    /**
     * Get available voices. Due to browser async loading, you may need to call this
     * after the `voiceschanged` event fires, or retry after a short delay.
     */
    static getVoices(): SpeechSynthesisVoice[] {
        return typeof window !== 'undefined' && 'speechSynthesis' in window
            ? window.speechSynthesis.getVoices()
            : [];
    }

    /**
     * Speak a complete text. Returns a Promise that resolves when the utterance finishes.
     */
    speak(text: string): Promise<void> {
        return new Promise<void>((resolve, reject) => {
            if (!BrowserSpeechSynthesizer.isSupported()) {
                reject(new Error('SpeechSynthesis API is not supported in this browser.'));
                return;
            }

            const trimmed = text.trim();
            if (!trimmed) {
                resolve();
                return;
            }

            // Chrome workaround: cancel any stale/paused state before first speak
            if (!this.warmedUp) {
                window.speechSynthesis.cancel();
                this.warmedUp = true;
            }

            const utterance = this.createUtterance(trimmed);

            utterance.onstart = () => {
                this._isSpeaking = true;
                this.callbacks.onSpeakStart?.(trimmed);
            };

            utterance.onend = () => {
                this._isSpeaking = false;
                this.callbacks.onSpeakEnd?.(trimmed);
                resolve();
            };

            utterance.onerror = (event: SpeechSynthesisErrorEvent) => {
                this._isSpeaking = false;
                const err = new Error(`SpeechSynthesis error: ${event.error}`);
                this.callbacks.onError?.(err);
                reject(err);
            };

            window.speechSynthesis.speak(utterance);
        });
    }

    /**
     * Feed a streaming text chunk. Text is accumulated until a sentence boundary is found,
     * then the complete sentence is queued for speaking.
     */
    feedChunk(chunk: string): void {
        this.buffer += chunk;
        this.extractAndQueueSentences();
    }

    /**
     * Flush any remaining buffered text (e.g. at end of stream) and queue it for speaking.
     */
    flush(): void {
        const remaining = this.buffer.trim();
        if (remaining) {
            this.buffer = '';
            this.enqueue(remaining);
        }
    }

    /**
     * Flush and wait for all queued sentences to finish speaking.
     * Returns a Promise that resolves when the queue is empty and speech is done.
     */
    async flushAndWait(): Promise<void> {
        this.flush();
        if (this.queue.length === 0 && !this._isSpeaking) return;

        return new Promise<void>((resolve) => {
            const check = () => {
                if (this.queue.length === 0 && !this._isSpeaking && !this._isProcessingQueue) {
                    resolve();
                } else {
                    setTimeout(check, 100);
                }
            };
            check();
        });
    }

    /** Cancel all speech and clear the queue. */
    cancel(): void {
        window.speechSynthesis.cancel();
        this.queue = [];
        this.buffer = '';
        this._isSpeaking = false;
        this._isProcessingQueue = false;
        this.warmedUp = false; // reset so next speak re-cancels
    }

    /** Pause speech. */
    pause(): void {
        window.speechSynthesis.pause();
    }

    /** Resume speech. */
    resume(): void {
        window.speechSynthesis.resume();
    }

    // ── Private ────────────────────────────────────────────────

    private extractAndQueueSentences(): void {
        const delimiters = this.options.sentenceDelimiters;
        let lastSplit = 0;

        for (let i = 0; i < this.buffer.length; i++) {
            if (delimiters.includes(this.buffer[i])) {
                const sentence = this.buffer.substring(lastSplit, i + 1).trim();
                if (sentence.length >= this.options.minCharsBeforeSpeak) {
                    this.enqueue(sentence);
                    lastSplit = i + 1;
                }
            }
        }

        // Keep un-split remainder in the buffer
        if (lastSplit > 0) {
            this.buffer = this.buffer.substring(lastSplit);
        }
    }

    private enqueue(text: string): void {
        this.queue.push(text);
        this.processQueue();
    }

    private async processQueue(): Promise<void> {
        if (this._isProcessingQueue) return;
        this._isProcessingQueue = true;

        try {
            while (this.queue.length > 0) {
                const text = this.queue.shift()!;
                await this.speak(text);
            }
            this.callbacks.onQueueEmpty?.();
        } finally {
            this._isProcessingQueue = false;
        }
    }

    private createUtterance(text: string): SpeechSynthesisUtterance {
        const cleaned = BrowserSpeechSynthesizer.stripMarkdown(text);
        const voice = this.resolveVoice();

        const utterance = new SpeechSynthesisUtterance(cleaned);

        // Set voice BEFORE lang — Chrome requires this order
        if (voice) {
            utterance.voice = voice;
            utterance.lang = voice.lang; // use the voice's own lang tag for consistency
        } else if (this.options.lang) {
            utterance.lang = this.options.lang;
        }

        utterance.rate = this.options.rate;
        utterance.pitch = this.options.pitch;
        utterance.volume = this.options.volume;

        return utterance;
    }

    /**
     * Normalize a BCP-47 lang code: lowercase and replace underscores with hyphens.
     */
    private static normalizeLang(lang: string): string {
        return lang.toLowerCase().replace(/_/g, '-');
    }

    private resolveVoice(): SpeechSynthesisVoice | null {
        if (this.voiceResolved) return this.resolvedVoice;

        const voices = BrowserSpeechSynthesizer.getVoices();
        if (voices.length === 0) return null; // voices not loaded yet — do NOT cache, retry next time

        // Try by exact name
        if (this.options.voiceName) {
            const byName = voices.find(v =>
                v.name.toLowerCase().includes(this.options.voiceName.toLowerCase())
            );
            if (byName) {
                this.voiceResolved = true;
                this.resolvedVoice = byName;
                return byName;
            }
        }

        // Try by language (exact match, then base language match)
        if (this.options.lang) {
            const targetLang = BrowserSpeechSynthesizer.normalizeLang(this.options.lang);
            const targetBase = targetLang.split('-')[0];

            // Exact match: it-it === it-it
            const exact = voices.find(v =>
                BrowserSpeechSynthesizer.normalizeLang(v.lang) === targetLang
            );
            if (exact) {
                this.voiceResolved = true;
                this.resolvedVoice = exact;
                return exact;
            }

            // Base language match: it-it starts with it, it-ch starts with it
            const byBase = voices.find(v =>
                BrowserSpeechSynthesizer.normalizeLang(v.lang).startsWith(targetBase + '-')
            ) ?? voices.find(v =>
                BrowserSpeechSynthesizer.normalizeLang(v.lang) === targetBase
            );
            if (byBase) {
                this.voiceResolved = true;
                this.resolvedVoice = byBase;
                return byBase;
            }
        }

        // Don't cache failure — voices may load later or lang may change
        return null;
    }

    /**
     * Strip common Markdown formatting so TTS reads only the plain text.
     * Handles: bold, italic, strikethrough, inline code, code blocks,
     * headers, links, images, blockquotes, list markers, horizontal rules.
     */
    static stripMarkdown(text: string): string {
        return text
            // Code blocks (```...```)
            .replace(/```[\s\S]*?```/g, '')
            // Inline code (`...`)
            .replace(/`([^`]+)`/g, '$1')
            // Images ![alt](url)
            .replace(/!\[([^\]]*)\]\([^)]*\)/g, '$1')
            // Links [text](url)
            .replace(/\[([^\]]*)\]\([^)]*\)/g, '$1')
            // Bold+Italic ***text*** or ___text___
            .replace(/(\*{3}|_{3})(.+?)\1/g, '$2')
            // Bold **text** or __text__
            .replace(/(\*{2}|_{2})(.+?)\1/g, '$2')
            // Italic *text* or _text_
            .replace(/(\*|_)(.+?)\1/g, '$2')
            // Strikethrough ~~text~~
            .replace(/~~(.+?)~~/g, '$1')
            // Headers # ... ######
            .replace(/^#{1,6}\s+/gm, '')
            // Blockquotes > text
            .replace(/^>+\s?/gm, '')
            // Unordered list markers (- * +)
            .replace(/^[\s]*[-*+]\s+/gm, '')
            // Ordered list markers (1. 2. etc.)
            .replace(/^[\s]*\d+\.\s+/gm, '')
            // Horizontal rules (---, ***, ___)
            .replace(/^[-*_]{3,}\s*$/gm, '')
            // HTML tags
            .replace(/<[^>]+>/g, '')
            // Collapse multiple newlines
            .replace(/\n{3,}/g, '\n\n')
            .trim();
    }
}
