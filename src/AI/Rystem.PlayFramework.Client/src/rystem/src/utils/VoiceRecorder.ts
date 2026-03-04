/**
 * Recording mode for VoiceRecorder.
 * - `vad` — Voice Activity Detection: automatically stops when silence is detected.
 * - `pushToTalk` — Press once to start, press again to stop & send.
 * - `holdToTalk` — Hold button to record, release to stop & send.
 */
export type VoiceRecordingMode = 'vad' | 'pushToTalk' | 'holdToTalk';

/**
 * Options for VoiceRecorder.
 */
export interface VoiceRecorderOptions {
    /** Recording mode. Default: `'vad'`. */
    mode?: VoiceRecordingMode;
    /** Audio MIME type. Default: `'audio/webm'`. */
    mimeType?: string;
    /** Maximum recording duration in ms (safety cap). Default: `30000` (30s). */
    maxDurationMs?: number;

    // ── VAD options ──────────────────────────────────────────────
    /** Silence threshold (0-1). Audio level below this is "silence". Default: `0.01`. */
    silenceThreshold?: number;
    /** How long (ms) silence must last before auto-stopping. Default: `1500`. */
    silenceDurationMs?: number;
    /** Minimum recording duration in ms before VAD can trigger a stop. Default: `500`. */
    minRecordingMs?: number;
}

/**
 * Callbacks emitted by VoiceRecorder.
 */
export interface VoiceRecorderCallbacks {
    /** Called when recording starts. */
    onStart?: () => void;
    /** Called every second with the elapsed seconds. */
    onTick?: (seconds: number) => void;
    /** Called with the recorded audio Blob when recording ends successfully. */
    onRecorded?: (blob: Blob) => void;
    /** Called when an error occurs. */
    onError?: (error: Error) => void;
    /** Called when recording stops (success or error). Always fires. */
    onStop?: () => void;
}

/**
 * Cross-browser voice recorder with support for VAD, push-to-talk, and hold-to-talk modes.
 *
 * @example
 * ```ts
 * const recorder = new VoiceRecorder({ mode: 'vad', silenceDurationMs: 2000 });
 *
 * // Start recording — in VAD mode it stops automatically on silence
 * recorder.start({
 *     onStart: () => console.log('Recording...'),
 *     onTick: (s) => console.log(`${s}s`),
 *     onRecorded: (blob) => sendToServer(blob),
 *     onStop: () => console.log('Done'),
 * });
 *
 * // For pushToTalk: call recorder.stop() when user presses the button again.
 * // For holdToTalk: call recorder.stop() on pointerup / mouseup.
 * ```
 */
export class VoiceRecorder {
    private _options: Required<VoiceRecorderOptions>;
    private _mediaRecorder: MediaRecorder | null = null;
    private _stream: MediaStream | null = null;
    private _audioContext: AudioContext | null = null;
    private _analyser: AnalyserNode | null = null;
    private _vadIntervalId: ReturnType<typeof setInterval> | null = null;
    private _tickIntervalId: ReturnType<typeof setInterval> | null = null;
    private _maxTimeoutId: ReturnType<typeof setTimeout> | null = null;
    private _recording = false;
    private _startTime = 0;
    private _callbacks: VoiceRecorderCallbacks = {};

    constructor(options?: VoiceRecorderOptions) {
        this._options = {
            mode: options?.mode ?? 'vad',
            mimeType: options?.mimeType ?? 'audio/webm',
            maxDurationMs: options?.maxDurationMs ?? 30_000,
            silenceThreshold: options?.silenceThreshold ?? 0.01,
            silenceDurationMs: options?.silenceDurationMs ?? 1500,
            minRecordingMs: options?.minRecordingMs ?? 500,
        };
    }

    /** Whether the recorder is currently active. */
    get isRecording(): boolean {
        return this._recording;
    }

    /** Current recording mode. */
    get mode(): VoiceRecordingMode {
        return this._options.mode;
    }

    set mode(value: VoiceRecordingMode) {
        if (this._recording) throw new Error('Cannot change mode while recording');
        this._options.mode = value;
    }

    /**
     * Start recording. In `vad` mode the recording stops automatically on silence.
     * In `pushToTalk` and `holdToTalk` you must call `stop()` manually.
     */
    async start(callbacks?: VoiceRecorderCallbacks): Promise<void> {
        if (this._recording) return;

        this._callbacks = callbacks ?? {};
        this._recording = true;
        this._startTime = Date.now();

        try {
            this._stream = await navigator.mediaDevices.getUserMedia({ audio: true });

            const recorder = new MediaRecorder(this._stream, { mimeType: this._options.mimeType });
            this._mediaRecorder = recorder;
            const chunks: Blob[] = [];

            recorder.ondataavailable = (e) => {
                if (e.data.size > 0) chunks.push(e.data);
            };

            recorder.onstop = () => {
                const blob = new Blob(chunks, { type: this._options.mimeType });
                this._cleanup();
                this._callbacks.onRecorded?.(blob);
                this._callbacks.onStop?.();
            };

            recorder.onerror = () => {
                this._cleanup();
                this._callbacks.onError?.(new Error('MediaRecorder error'));
                this._callbacks.onStop?.();
            };

            recorder.start();
            this._callbacks.onStart?.();

            // Tick timer (1s intervals)
            let seconds = 0;
            this._tickIntervalId = setInterval(() => {
                seconds++;
                this._callbacks.onTick?.(seconds);
            }, 1000);

            // Max duration safety cap
            this._maxTimeoutId = setTimeout(() => {
                this.stop();
            }, this._options.maxDurationMs);

            // VAD: set up silence detection
            if (this._options.mode === 'vad') {
                this._setupVAD();
            }
        } catch (err: any) {
            this._recording = false;
            this._cleanup();
            const error = err instanceof Error ? err : new Error(String(err));
            this._callbacks.onError?.(error);
            this._callbacks.onStop?.();
        }
    }

    /**
     * Stop recording manually. Required for `pushToTalk` and `holdToTalk`,
     * also works for `vad` (e.g. user wants to stop early).
     */
    stop(): void {
        if (!this._recording) return;
        this._recording = false;

        if (this._mediaRecorder && this._mediaRecorder.state !== 'inactive') {
            this._mediaRecorder.stop(); // triggers onstop -> onRecorded -> onStop
        } else {
            this._cleanup();
            this._callbacks.onStop?.();
        }
    }

    // ── Private ──────────────────────────────────────────────────────

    private _setupVAD(): void {
        if (!this._stream) return;

        this._audioContext = new AudioContext();
        const source = this._audioContext.createMediaStreamSource(this._stream);
        this._analyser = this._audioContext.createAnalyser();
        this._analyser.fftSize = 2048;
        source.connect(this._analyser);

        const dataArray = new Uint8Array(this._analyser.fftSize);
        let silenceStart: number | null = null;

        this._vadIntervalId = setInterval(() => {
            if (!this._analyser || !this._recording) return;

            this._analyser.getByteTimeDomainData(dataArray);

            // Compute RMS (root mean square) level
            let sum = 0;
            for (let i = 0; i < dataArray.length; i++) {
                const normalized = (dataArray[i] - 128) / 128; // -1..1
                sum += normalized * normalized;
            }
            const rms = Math.sqrt(sum / dataArray.length);

            const elapsed = Date.now() - this._startTime;

            if (rms < this._options.silenceThreshold) {
                // Below threshold — start or continue silence timer
                if (silenceStart === null) {
                    silenceStart = Date.now();
                } else if (
                    elapsed >= this._options.minRecordingMs &&
                    Date.now() - silenceStart >= this._options.silenceDurationMs
                ) {
                    // Enough silence after minimum recording time → stop
                    this.stop();
                }
            } else {
                // Audio detected — reset silence timer
                silenceStart = null;
            }
        }, 100); // Check every 100ms
    }

    private _cleanup(): void {
        this._recording = false;

        if (this._vadIntervalId !== null) {
            clearInterval(this._vadIntervalId);
            this._vadIntervalId = null;
        }
        if (this._tickIntervalId !== null) {
            clearInterval(this._tickIntervalId);
            this._tickIntervalId = null;
        }
        if (this._maxTimeoutId !== null) {
            clearTimeout(this._maxTimeoutId);
            this._maxTimeoutId = null;
        }
        if (this._audioContext) {
            this._audioContext.close().catch(() => {});
            this._audioContext = null;
            this._analyser = null;
        }
        if (this._stream) {
            this._stream.getTracks().forEach(t => t.stop());
            this._stream = null;
        }
        this._mediaRecorder = null;
    }
}
