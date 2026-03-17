import React, { useState, useRef, useEffect, useCallback } from 'react';
import {
    PlayFrameworkServices,
    PlayFrameworkClient,
    AIContentConverter,
    ContentUrlConverter,
    CommandResultHelper,
    VoiceRecorder,
    BrowserVoiceClient,
    type VoiceRecordingMode,
    type BrowserVoiceStreamingMode,
    type PlayFrameworkRequest,
    type AiSceneResponse,
    type AiResponseStatus,
    type SceneExecutionMode,
    type StoredConversation,
    type StoredMessage,
    type AIContent,
    type ContentItem,
    type VoiceEvent,
    ConversationSortOrder
} from './rystem/src/index';
import './App.css';

// ─── Types ───────────────────────────────────────────────────────────────

interface ChatMessage {
    role: 'user' | 'assistant' | 'system' | 'tool';
    text: string;
    status?: AiResponseStatus;
    timestamp: Date;
    toolName?: string;
    contents?: AIContent[];
    inputTokens?: number;
    outputTokens?: number;
    cachedInputTokens?: number;
    totalTokens?: number;
    cost?: number;
    totalCost?: number;
}

type StreamMode = 'step' | 'token' | 'voice';
type VoiceEngine = 'server' | 'browser';
type ConnectionStatus = 'disconnected' | 'connecting' | 'connected' | 'error';
type ExecutionModeOption = SceneExecutionMode;

// ─── Configuration ───────────────────────────────────────────────────────

const API_BASE = 'http://localhost:5158/api/ai';

type FactoryName = 'default' | 'foundry';

// Configure both PlayFramework clients (runs once at module load)
const clientsReady = Promise.all([
    PlayFrameworkServices.configure('default', API_BASE, settings => {
        settings.timeout = 120_000;
        settings.maxReconnectAttempts = 3;
        settings.reconnectBaseDelay = 1000;
    }),
    PlayFrameworkServices.configure('foundry', API_BASE, settings => {
        settings.timeout = 120_000;
        settings.maxReconnectAttempts = 3;
        settings.reconnectBaseDelay = 1000;
    }),
]);

// ─── Client Tool Implementations ─────────────────────────────────────────

function registerClientTools(client: PlayFrameworkClient) {
    const registry = client.getClientRegistry();

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // STANDARD TOOLS (require response)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    // getCurrentLocation — uses browser Geolocation API
    registry.register('getCurrentLocation', async () => {
        const content = await AIContentConverter.fromGeolocation({ timeout: 10_000 });
        return [content];
    });

    // getUserConfirmation — shows a browser confirm dialog
    registry.register<{ question?: string; confirmLabel?: string; cancelLabel?: string }>(
        'getUserConfirmation',
        async (args) => {
            const question = args?.question ?? 'Do you confirm?';
            const confirmed = window.confirm(question);
            return [AIContentConverter.fromText(confirmed ? 'confirmed' : 'denied')];
        }
    );

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // COMMANDS (fire-and-forget, optional feedback)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    // Command - NEVER mode: Silent logging (no feedback)
    registry.registerCommand('logUserAction', async (args?: { action: string }) => {
        console.log(`[Command] User action: ${args?.action || 'unknown'}`);
        return CommandResultHelper.ok();
    }, { feedbackMode: 'never' });

    // Command - ON_ERROR mode (default): Track analytics, send feedback only on error
    registry.registerCommand('trackAnalytics', async (args?: { event: string }) => {
        try {
            await fetch('/analytics', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(args)
            });
            return CommandResultHelper.ok(`Event "${args?.event}" tracked`);
        } catch (error: any) {
            return CommandResultHelper.fail(`Analytics error: ${error.message}`);
        }
    }); // Default feedbackMode: 'onError'

    // Command - ALWAYS mode: Critical operation with full feedback
    registry.registerCommand('saveToLocalStorage', async (args?: { key: string; value: string }) => {
        try {
            if (!args) throw new Error('Missing arguments');
            localStorage.setItem(args.key, args.value);
            console.log(`[Command:Always] Saved "${args.key}" = "${args.value}" to localStorage`);
            return CommandResultHelper.ok(`Saved "${args.key}" to local storage`);
        } catch (error: any) {
            console.log(`[Command:Always] Failed to save: ${error.message}`);
            return CommandResultHelper.fail(`Storage error: ${error.message}`);
        }
    }, { feedbackMode: 'always' });

    // Command - ON_ERROR mode: Show notification, feedback only on failure
    registry.registerCommand('showNotification', async (args?: { title: string; message: string; type: string }) => {
        try {
            const title = args?.title ?? 'Notification';
            const message = args?.message ?? '';
            const type = args?.type ?? 'info';
            console.log(`[Command:OnError] Notification [${type}]: ${title} - ${message}`);
            window.alert(`[${type.toUpperCase()}] ${title}\n\n${message}`);
            return CommandResultHelper.ok(`Notification shown: ${title}`);
        } catch (error: any) {
            console.log(`[Command:OnError] Notification failed: ${error.message}`);
            return CommandResultHelper.fail(`Notification error: ${error.message}`);
        }
    }, { feedbackMode: 'onError' });
}

// ─── Helpers ─────────────────────────────────────────────────────────────

const statusColors: Partial<Record<AiResponseStatus, string>> = {
    // Initialization & Loading
    initializing: '#888',
    loadingCache: '#6c757d',
    executingMainActors: '#7952b3',

    // Planning & Execution
    planning: '#f0ad4e',
    executingScene: '#5bc0de',
    running: '#17a2b8',

    // Function/Tool Execution
    functionRequest: '#d9534f',
    functionCompleted: '#5cb85c',
    toolSkipped: '#6c757d',

    // Streaming & Response Generation
    streaming: '#61dafb',
    generatingFinalResponse: '#20c997',

    // Director & Summarization
    directorDecision: '#e83e8c',
    summarizing: '#fd7e14',

    // Saving & Persistence
    savingCache: '#6610f2',
    savingMemory: '#6f42c1',

    // Client Interaction
    awaitingClient: '#ff6b6b',
    commandClient: '#ff9500',

    // Final States
    completed: '#5cb85c',
    error: '#d9534f',
    budgetExceeded: '#f0ad4e',
    unauthorized: '#dc3545',
};

function statusBadge(status?: AiResponseStatus) {
    if (!status) return null;
    const bg = statusColors[status] ?? '#888';
    return (
        <span style={{
            display: 'inline-block',
            fontSize: '11px',
            padding: '2px 7px',
            borderRadius: '4px',
            backgroundColor: bg,
            color: '#fff',
            marginRight: '6px',
            fontWeight: 600,
            letterSpacing: '0.3px',
        }}>
            {status}
        </span>
    );
}

// ─── Content Viewer Components ───────────────────────────────────────────

interface ContentViewerProps {
    content: AIContent;
}

function ImageViewer({ content }: ContentViewerProps) {
    const [url, setUrl] = useState<string | null>(null);

    useEffect(() => {
        const blobUrl = ContentUrlConverter.toBlobUrl(content, `image-${Date.now()}`);
        setUrl(blobUrl);

        return () => {
            if (blobUrl) {
                ContentUrlConverter.revokeUrl(blobUrl);
            }
        };
    }, [content]);

    if (!url) return <div style={{ color: '#999', fontSize: '12px' }}>Loading image...</div>;

    return (
        <div style={{ marginTop: '8px', position: 'relative' }}>
            <img
                src={url}
                alt="Image"
                style={{
                    maxWidth: '100%',
                    maxHeight: '400px',
                    borderRadius: '8px',
                    border: '1px solid #444',
                }}
            />
            <button
                onClick={() => ContentUrlConverter.downloadAsFile(content, 'image')}
                style={{
                    position: 'absolute',
                    top: '8px',
                    right: '8px',
                    padding: '4px 10px',
                    fontSize: '11px',
                    borderRadius: '4px',
                    border: '1px solid rgba(255,255,255,0.3)',
                    backgroundColor: 'rgba(0,0,0,0.6)',
                    color: '#fff',
                    cursor: 'pointer',
                }}
            >
                Download
            </button>
        </div>
    );
}

function AudioPlayer({ content }: ContentViewerProps) {
    const [url, setUrl] = useState<string | null>(null);

    useEffect(() => {
        const blobUrl = ContentUrlConverter.toBlobUrl(content, `audio-${Date.now()}`);
        setUrl(blobUrl);

        return () => {
            if (blobUrl) {
                ContentUrlConverter.revokeUrl(blobUrl);
            }
        };
    }, [content]);

    if (!url) return <div style={{ color: '#999', fontSize: '12px' }}>Loading audio...</div>;

    return (
        <div style={{ marginTop: '8px' }}>
            <audio controls style={{ width: '100%', maxWidth: '500px' }}>
                <source src={url} type={content.mediaType || 'audio/mpeg'} />
                Your browser does not support audio playback.
            </audio>
            <button
                onClick={() => ContentUrlConverter.downloadAsFile(content, 'audio')}
                style={{
                    marginTop: '6px',
                    padding: '4px 10px',
                    fontSize: '11px',
                    borderRadius: '4px',
                    border: '1px solid #444',
                    backgroundColor: '#333',
                    color: '#aaa',
                    cursor: 'pointer',
                }}
            >
                Download Audio
            </button>
        </div>
    );
}

function VideoPlayer({ content }: ContentViewerProps) {
    const [url, setUrl] = useState<string | null>(null);

    useEffect(() => {
        const blobUrl = ContentUrlConverter.toBlobUrl(content, `video-${Date.now()}`);
        setUrl(blobUrl);

        return () => {
            if (blobUrl) {
                ContentUrlConverter.revokeUrl(blobUrl);
            }
        };
    }, [content]);

    if (!url) return <div style={{ color: '#999', fontSize: '12px' }}>Loading video...</div>;

    return (
        <div style={{ marginTop: '8px' }}>
            <video
                controls
                style={{
                    width: '100%',
                    maxWidth: '600px',
                    maxHeight: '400px',
                    borderRadius: '8px',
                    border: '1px solid #444',
                }}
            >
                <source src={url} type={content.mediaType || 'video/mp4'} />
                Your browser does not support video playback.
            </video>
            <button
                onClick={() => ContentUrlConverter.downloadAsFile(content, 'video')}
                style={{
                    marginTop: '6px',
                    padding: '4px 10px',
                    fontSize: '11px',
                    borderRadius: '4px',
                    border: '1px solid #444',
                    backgroundColor: '#333',
                    color: '#aaa',
                    cursor: 'pointer',
                }}
            >
                Download Video
            </button>
        </div>
    );
}

function PDFViewer({ content }: ContentViewerProps) {
    const [url, setUrl] = useState<string | null>(null);

    useEffect(() => {
        const blobUrl = ContentUrlConverter.toBlobUrl(content, `pdf-${Date.now()}`);
        setUrl(blobUrl);

        return () => {
            if (blobUrl) {
                ContentUrlConverter.revokeUrl(blobUrl);
            }
        };
    }, [content]);

    if (!url) return <div style={{ color: '#999', fontSize: '12px' }}>Loading PDF...</div>;

    return (
        <div style={{ marginTop: '8px' }}>
            <iframe
                src={url}
                title="PDF Document"
                style={{
                    width: '100%',
                    height: '500px',
                    border: '1px solid #444',
                    borderRadius: '8px',
                }}
            />
            <button
                onClick={() => ContentUrlConverter.downloadAsFile(content, 'document.pdf')}
                style={{
                    marginTop: '6px',
                    padding: '4px 10px',
                    fontSize: '11px',
                    borderRadius: '4px',
                    border: '1px solid #444',
                    backgroundColor: '#333',
                    color: '#aaa',
                    cursor: 'pointer',
                }}
            >
                Download PDF
            </button>
        </div>
    );
}

function ContentViewer({ content }: ContentViewerProps) {
    // Skip text content - it's already displayed in msg.text
    if (content.type === 'text') {
        return null;
    }

    // Handle multimedia content (images, audio, video, PDFs, files)
    if (content.type === 'data') {
        const mediaType = content.mediaType?.toLowerCase() || '';

        if (mediaType.startsWith('image/')) {
            return <ImageViewer content={content} />;
        }

        if (mediaType.startsWith('audio/')) {
            return <AudioPlayer content={content} />;
        }

        if (mediaType.startsWith('video/')) {
            return <VideoPlayer content={content} />;
        }

        if (mediaType === 'application/pdf') {
            return <PDFViewer content={content} />;
        }

        // Fallback for unknown binary types
        return (
            <div style={{
                marginTop: '8px',
                padding: '10px',
                backgroundColor: '#2d2d2d',
                borderRadius: '6px',
                border: '1px solid #444',
            }}>
                <div style={{ fontSize: '12px', color: '#aaa', marginBottom: '6px' }}>
                    Attachment ({mediaType || 'unknown type'})
                </div>
                <button
                    onClick={() => ContentUrlConverter.downloadAsFile(content, 'file')}
                    style={{
                        padding: '4px 10px',
                        fontSize: '11px',
                        borderRadius: '4px',
                        border: '1px solid #444',
                        backgroundColor: '#333',
                        color: '#aaa',
                        cursor: 'pointer',
                    }}
                >
                    Download File
                </button>
            </div>
        );
    }

    // Unknown content type
    return null;
}

// ─── App Component ───────────────────────────────────────────────────────

function App() {
    const [messages, setMessages] = useState<ChatMessage[]>([]);
    const [input, setInput] = useState('');
    const [loading, setLoading] = useState(false);
    const [mode, setMode] = useState<StreamMode>('step');
    const [executionMode, setExecutionMode] = useState<ExecutionModeOption>('Direct');
    const [connection, setConnection] = useState<ConnectionStatus>('disconnected');
    const [conversationKey, setConversationKey] = useState<string | undefined>();
    const messagesEndRef = useRef<HTMLDivElement>(null);
    const inputRef = useRef<HTMLInputElement>(null);
    const clientRef = useRef<PlayFrameworkClient | null>(null);

    // ─── Factory Selection ───────────────────────────────────────────
    const [activeFactory, setActiveFactory] = useState<FactoryName>('default');
    /** Holds all initialised clients keyed by factory name. */
    const clientsMapRef = useRef<Map<FactoryName, PlayFrameworkClient>>(new Map());

    // ─── Conversation Management State ───────────────────────────────────
    const [conversations, setConversations] = useState<StoredConversation[]>([]);
    const [searchText, setSearchText] = useState('');
    const [showPublic, setShowPublic] = useState(true);
    const [showPrivate, setShowPrivate] = useState(true);
    const [showSidebar, setShowSidebar] = useState(false);
    const [loadingConversations, setLoadingConversations] = useState(false);

    // ─── Attachment State ────────────────────────────────────────────────
    const [pendingAttachments, setPendingAttachments] = useState<ContentItem[]>([]);
    const [isRecording, setIsRecording] = useState(false);
    const [recordingSeconds, setRecordingSeconds] = useState(0);
    const fileInputRef = useRef<HTMLInputElement>(null);
    const recordingTimerRef = useRef<ReturnType<typeof setInterval> | null>(null);

    // ─── Voice Mode State ────────────────────────────────────────────────
    const [isVoiceProcessing, setIsVoiceProcessing] = useState(false);
    const [voiceTranscript, setVoiceTranscript] = useState<string | null>(null);
    const [voiceRecordingMode, setVoiceRecordingMode] = useState<VoiceRecordingMode>('vad');
    const [voiceEngine, setVoiceEngine] = useState<VoiceEngine>('server');
    const [browserVoiceStreamingMode, setBrowserVoiceStreamingMode] = useState<BrowserVoiceStreamingMode>('stepByStep');
    const [browserVoiceLang, setBrowserVoiceLang] = useState(navigator.language || 'en-US');
    const audioQueueRef = useRef<string[]>([]);
    const isPlayingAudioRef = useRef(false);
    const voiceRecorderRef = useRef<VoiceRecorder>(new VoiceRecorder({ mode: 'vad' }));
    const browserVoiceClientRef = useRef<BrowserVoiceClient | null>(null);

    // ─── Cancellation ────────────────────────────────────────────────
    /** AbortController for the current text-mode (step/token) request. */
    const textAbortRef = useRef<AbortController | null>(null);
    /** AbortController for the current server-voice request. */
    const voiceAbortRef = useRef<AbortController | null>(null);

    // Initialize client
    useEffect(() => {
        clientsReady.then(() => {
            // Initialise both clients and register tools on each
            for (const name of ['default', 'foundry'] as FactoryName[]) {
                const client = PlayFrameworkServices.resolve(name);
                registerClientTools(client);
                clientsMapRef.current.set(name, client);
            }
            // Set the active client
            clientRef.current = clientsMapRef.current.get(activeFactory) ?? null;
            // Create BrowserVoiceClient from active client
            if (clientRef.current && BrowserVoiceClient.isSupported()) {
                const lang = navigator.language || 'en-US';
                browserVoiceClientRef.current = new BrowserVoiceClient(
                    clientRef.current,
                    { lang },
                    { lang },
                );
            }
            setConnection('connected');
        }).catch(err => {
            console.error('Failed to configure PlayFramework:', err);
            setConnection('error');
        });
    }, []);

    // Auto-scroll
    useEffect(() => {
        messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages]);

    // Switch active client when factory changes
    useEffect(() => {
        const client = clientsMapRef.current.get(activeFactory);
        if (client) {
            clientRef.current = client;
            // Recreate BrowserVoiceClient for the new client
            if (BrowserVoiceClient.isSupported()) {
                const lang = browserVoiceLang || navigator.language || 'en-US';
                browserVoiceClientRef.current = new BrowserVoiceClient(
                    client,
                    { lang },
                    { lang },
                );
            }
        }
    }, [activeFactory]);

    const addMessage = useCallback((msg: ChatMessage) => {
        setMessages(prev => [...prev, msg]);
    }, []);

    const updateLastAssistant = useCallback((updater: (prev: ChatMessage) => ChatMessage) => {
        setMessages(prev => {
            const idx = prev.length - 1;
            if (idx < 0 || prev[idx].role !== 'assistant') return prev;
            const updated = [...prev];
            updated[idx] = updater(updated[idx]);
            return updated;
        });
    }, []);

    // ── File / Audio / Camera helpers ─────────────────────────────────

    /** Map MIME type to ContentItem type */
    const mimeToContentType = (mime: string): ContentItem['type'] => {
        if (mime.startsWith('image/')) return 'image';
        if (mime.startsWith('audio/')) return 'audio';
        if (mime.startsWith('video/')) return 'video';
        return 'file';
    };

    /** Handle file(s) selected from <input type="file"> */
    const handleFileSelect = async (e: React.ChangeEvent<HTMLInputElement>) => {
        const files = e.target.files;
        if (!files || files.length === 0) return;

        for (const file of Array.from(files)) {
            const reader = new FileReader();
            const base64 = await new Promise<string>((resolve, reject) => {
                reader.onload = () => {
                    const result = reader.result as string;
                    resolve(result.split(',')[1]); // strip data:...;base64,
                };
                reader.onerror = () => reject(new Error('Failed to read file'));
                reader.readAsDataURL(file);
            });

            const item: ContentItem = {
                type: mimeToContentType(file.type),
                data: base64,
                mediaType: file.type || 'application/octet-stream',
                name: file.name,
            };
            setPendingAttachments(prev => [...prev, item]);
        }

        // Reset input so the same file can be re-selected
        e.target.value = '';
    };

    /** Record audio from microphone */
    const handleAudioRecord = async () => {
        if (isRecording) return; // prevent double-click

        setIsRecording(true);
        setRecordingSeconds(0);

        // Start countdown timer
        recordingTimerRef.current = setInterval(() => {
            setRecordingSeconds(s => s + 1);
        }, 1000);

        try {
            const durationMs = 10_000; // 10 seconds max
            const content = await AIContentConverter.fromMicrophone(durationMs, 'audio/webm');

            const item: ContentItem = {
                type: 'audio',
                data: content.data,
                mediaType: content.mediaType || 'audio/webm',
                name: `recording-${Date.now()}.webm`,
            };
            setPendingAttachments(prev => [...prev, item]);
        } catch (err: any) {
            console.error('Microphone error:', err);
            alert(`Microphone error: ${err.message}`);
        } finally {
            setIsRecording(false);
            setRecordingSeconds(0);
            if (recordingTimerRef.current) {
                clearInterval(recordingTimerRef.current);
                recordingTimerRef.current = null;
            }
        }
    };

    /** Capture photo from camera */
    const handleCameraCapture = async () => {
        try {
            const content = await AIContentConverter.fromCamera();
            const item: ContentItem = {
                type: 'image',
                data: content.data,
                mediaType: content.mediaType || 'image/jpeg',
                name: `photo-${Date.now()}.jpg`,
            };
            setPendingAttachments(prev => [...prev, item]);
        } catch (err: any) {
            console.error('Camera error:', err);
            alert(`Camera error: ${err.message}`);
        }
    };

    /** Remove a pending attachment by index */
    const removeAttachment = (index: number) => {
        setPendingAttachments(prev => prev.filter((_, i) => i !== index));
    };

    // ── Voice audio playback helpers ──────────────────────────────────

    /** Play queued base64 audio chunks sequentially */
    const playNextAudio = useCallback(() => {
        if (isPlayingAudioRef.current || audioQueueRef.current.length === 0) return;
        isPlayingAudioRef.current = true;
        const base64 = audioQueueRef.current.shift()!;
        try {
            const binary = atob(base64);
            const bytes = new Uint8Array(binary.length);
            for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
            const blob = new Blob([bytes], { type: 'audio/mp3' });
            const url = URL.createObjectURL(blob);
            const audio = new Audio(url);
            audio.onended = () => {
                URL.revokeObjectURL(url);
                isPlayingAudioRef.current = false;
                playNextAudio();
            };
            audio.onerror = () => {
                URL.revokeObjectURL(url);
                isPlayingAudioRef.current = false;
                playNextAudio();
            };
            audio.play().catch(() => {
                isPlayingAudioRef.current = false;
                playNextAudio();
            });
        } catch {
            isPlayingAudioRef.current = false;
            playNextAudio();
        }
    }, []);

    /** Enqueue a base64 audio chunk for playback */
    const enqueueAudio = useCallback((base64: string) => {
        audioQueueRef.current.push(base64);
        playNextAudio();
    }, [playNextAudio]);

    // ── Voice mode send ───────────────────────────────────────────────

    /** Cancel the current server-voice request (if any). */
    const cancelServerVoice = useCallback(() => {
        if (voiceAbortRef.current) {
            voiceAbortRef.current.abort();
            voiceAbortRef.current = null;
        }
        // Stop any queued audio playback
        audioQueueRef.current = [];
        isPlayingAudioRef.current = false;
    }, []);

    const handleVoiceSend = useCallback(async (audioBlob: Blob) => {
        if (!clientRef.current) return;

        // If already processing, cancel old flow first
        cancelServerVoice();

        setIsVoiceProcessing(true);
        setVoiceTranscript(null);
        audioQueueRef.current = [];
        isPlayingAudioRef.current = false;

        const controller = new AbortController();
        voiceAbortRef.current = controller;

        const key = conversationKey ?? crypto.randomUUID();
        if (!conversationKey) setConversationKey(key);

        addMessage({ role: 'user', text: '[Voice message]', timestamp: new Date() });
        addMessage({ role: 'assistant', text: '', timestamp: new Date() });

        try {
            setConnection('connecting');

            for await (const event of clientRef.current.executeVoice({
                audio: audioBlob,
                conversationKey: key,
                signal: controller.signal,
            })) {
                switch (event.type) {
                    case 'transcription':
                        // Update user message with transcribed text
                        setVoiceTranscript(event.text ?? null);
                        setMessages(prev => {
                            const updated = [...prev];
                            // Find the last user message
                            for (let i = updated.length - 1; i >= 0; i--) {
                                if (updated[i].role === 'user') {
                                    updated[i] = { ...updated[i], text: `[Voice] ${event.text}` };
                                    break;
                                }
                            }
                            return updated;
                        });
                        break;

                    case 'audio':
                        // Display the synthesized text in the assistant message
                        const audioText = event.text;
                        if (audioText) {
                            updateLastAssistant(prev => ({
                                ...prev,
                                text: prev.text ? prev.text + audioText : audioText,
                            }));
                        }
                        // Queue audio chunk for playback
                        if (event.audio) enqueueAudio(event.audio);
                        break;

                    case 'scene':
                        // Scene events use 'message' field for status text
                        break;

                    case 'completed':
                        break;

                    case 'error':
                        addMessage({
                            role: 'system',
                            text: `Voice error: ${event.message ?? 'Unknown error'}`,
                            status: 'error',
                            timestamp: new Date(),
                        });
                        break;
                }
            }

            setConnection('connected');
        } catch (error: any) {
            // Ignore abort errors (user cancelled)
            if (error instanceof DOMException && error.name === 'AbortError') {
                setConnection('connected');
            } else {
                console.error('Voice error:', error);
                addMessage({
                    role: 'system',
                    text: `Voice error: ${error.message ?? error}`,
                    status: 'error',
                    timestamp: new Date(),
                });
                setConnection('error');
            }
        } finally {
            voiceAbortRef.current = null;
            setIsVoiceProcessing(false);
        }
    }, [conversationKey, addMessage, updateLastAssistant, enqueueAudio, cancelServerVoice]);

    // ── Browser voice interaction ──────────────────────────────────────

    const handleBrowserVoiceInteraction = useCallback(async () => {
        const bvc = browserVoiceClientRef.current;
        if (!bvc) return;

        setIsVoiceProcessing(true);
        setVoiceTranscript(null);

        const key = conversationKey ?? crypto.randomUUID();
        if (!conversationKey) setConversationKey(key);

        try {
            setConnection('connecting');

            for await (const event of bvc.executeWithBrowserVoice({
                streamingMode: browserVoiceStreamingMode,
                request: {
                    conversationKey: key,
                    settings: { executionMode, enableStreaming: browserVoiceStreamingMode === 'tokenStreaming' },
                },
            })) {
                // Voice status events
                if (event.voiceStatus === 'recognized' && event.transcript) {
                    setVoiceTranscript(event.transcript);
                    addMessage({ role: 'user', text: `[Voice] ${event.transcript}`, timestamp: new Date() });
                    addMessage({ role: 'assistant', text: '', timestamp: new Date() });
                }

                // LLM response events
                if (event.response) {
                    const resp = event.response;

                    if (resp.conversationKey) setConversationKey(resp.conversationKey);

                    if (resp.streamingChunk) {
                        updateLastAssistant(prev => ({
                            ...prev,
                            text: prev.text + resp.streamingChunk,
                            status: resp.status,
                        }));
                    } else if (resp.message) {
                        updateLastAssistant(prev => ({
                            ...prev,
                            text: prev.text
                                ? prev.text + '\n' + `[${resp.status}] ${resp.message}`
                                : `[${resp.status}] ${resp.message}`,
                            status: resp.status,
                        }));
                    }
                }
            }

            setConnection('connected');
        } catch (error: any) {
            // Ignore abort errors (user cancelled to restart)
            if (error instanceof DOMException && error.name === 'AbortError') {
                setConnection('connected');
            } else {
                console.error('Browser voice error:', error);
                addMessage({
                    role: 'system',
                    text: `Browser voice error: ${error.message ?? error}`,
                    status: 'error',
                    timestamp: new Date(),
                });
                setConnection('error');
            }
        } finally {
            setIsVoiceProcessing(false);
        }
    }, [conversationKey, browserVoiceStreamingMode, executionMode, addMessage, updateLastAssistant]);

    // ── Voice-mode audio recording (record & auto-send) ───────────────

    /** Start or stop voice recording depending on mode and current state */
    const handleVoiceRecord = useCallback(() => {
        // ── Stop current flow (any engine) ──────────────────────────
        if (isVoiceProcessing) {
            if (voiceEngine === 'browser') {
                browserVoiceClientRef.current?.cancelAll();
            } else {
                cancelServerVoice();
            }
            setIsVoiceProcessing(false);
            return;
        }

        // ── Start new flow ──────────────────────────────────────────
        // Browser voice engine: BrowserVoiceClient handles STT
        if (voiceEngine === 'browser') {
            handleBrowserVoiceInteraction();
            return;
        }

        // Server voice engine: use VoiceRecorder to capture audio blob
        const recorder = voiceRecorderRef.current;

        // If already recording → stop (pushToTalk toggle, or manual stop in any mode)
        if (recorder.isRecording) {
            recorder.stop();
            return;
        }

        recorder.start({
            onStart: () => {
                setIsRecording(true);
                setRecordingSeconds(0);
            },
            onTick: (seconds) => {
                setRecordingSeconds(seconds);
            },
            onRecorded: (blob) => {
                handleVoiceSend(blob);
            },
            onError: (err) => {
                console.error('Voice recording error:', err);
                addMessage({
                    role: 'system',
                    text: `Mic error: ${err.message}`,
                    status: 'error',
                    timestamp: new Date(),
                });
            },
            onStop: () => {
                setIsRecording(false);
                setRecordingSeconds(0);
            },
        });
    }, [voiceEngine, isVoiceProcessing, handleVoiceSend, handleBrowserVoiceInteraction, addMessage, cancelServerVoice]);

    /** Stop recording on pointer-up (holdToTalk mode) */
    const handleVoicePointerUp = useCallback(() => {
        if (voiceRecordingMode === 'holdToTalk') {
            voiceRecorderRef.current.stop();
        }
    }, [voiceRecordingMode]);

    /** Keep VoiceRecorder mode in sync with UI selection */
    useEffect(() => {
        voiceRecorderRef.current.mode = voiceRecordingMode;
    }, [voiceRecordingMode]);

    /** Sync browser voice language when user changes the selector */
    useEffect(() => {
        const bvc = browserVoiceClientRef.current;
        if (bvc) {
            bvc.getRecognizer().setLang(browserVoiceLang);
            bvc.getSynthesizer().setLang(browserVoiceLang);
        }
    }, [browserVoiceLang]);

    // ── Load conversations ────────────────────────────────────────────

    const loadConversations = useCallback(async () => {
        if (!clientRef.current) return;

        setLoadingConversations(true);
        try {
            const result = await clientRef.current.listConversations({
                searchText: searchText || undefined,
                includePublic: showPublic,
                includePrivate: showPrivate,
                orderBy: ConversationSortOrder.TimestampDescending,
                take: 100
            });
            setConversations(result);
        } catch (error: any) {
            console.error('Failed to load conversations:', error);
        } finally {
            setLoadingConversations(false);
        }
    }, [searchText, showPublic, showPrivate]);

    // Load conversations when sidebar opens or filters change
    useEffect(() => {
        if (showSidebar) {
            loadConversations();
        }
    }, [showSidebar, showPublic, showPrivate, loadConversations]);

    // ── Delete conversation ───────────────────────────────────────────

    const handleDeleteConversation = async (key: string) => {
        if (!clientRef.current) return;
        if (!window.confirm('Delete this conversation?')) return;

        try {
            await clientRef.current.deleteConversation(key);
            await loadConversations(); // Reload list
            if (conversationKey === key) {
                // If deleted current conversation, clear chat
                handleClear();
            }
        } catch (error: any) {
            console.error('Failed to delete conversation:', error);
            alert(`Failed to delete: ${error.message}`);
        }
    };

    // ── Load conversation into chat ───────────────────────────────────

    const handleLoadConversation = async (key: string) => {
        if (!clientRef.current) return;

        try {
            // Request with includeContents=true to get media attachments
            const conv = await clientRef.current.getConversation(key, true);

            if (!conv) {
                alert('Conversation not found');
                return;
            }

            // Convert stored messages to ChatMessages
            const chatMsgs: ChatMessage[] = conv.messages.map(m => ({
                role: m.role as 'user' | 'assistant' | 'system' | 'tool',
                text: m.text || '',
                timestamp: new Date(conv.timestamp),
                contents: m.contents ?? undefined // Include attached media content, convert null to undefined
            }));

            setMessages(chatMsgs);
            setConversationKey(conv.conversationKey);
            setShowSidebar(false); // Close sidebar after loading
        } catch (error: any) {
            console.error('Failed to load conversation:', error);
            alert(`Failed to load: ${error.message}`);
        }
    };

    // ── Send message ──────────────────────────────────────────────────

    const handleSend = async () => {
        const text = input.trim();
        const hasAttachments = pendingAttachments.length > 0;
        if ((!text && !hasAttachments) || loading || !clientRef.current) return;

        // Snapshot attachments before clearing
        const attachments = [...pendingAttachments];
        setInput('');
        setPendingAttachments([]);
        setLoading(true);

        // Build user message with attachment info
        const attachLabel = attachments.length > 0
            ? `\n[Attachments: ${attachments.map(a => a.name || a.type).join(', ')}]`
            : '';
        addMessage({ role: 'user', text: (text || '') + attachLabel, timestamp: new Date() });

        // Generate conversationKey if not already set (first request)
        const key = conversationKey ?? crypto.randomUUID();
        if (!conversationKey) {
            setConversationKey(key);
        }

        const request: PlayFrameworkRequest = {
            message: text || undefined,
            contents: attachments.length > 0 ? attachments : undefined,
            conversationKey: key,
            settings: {
                executionMode,
                enableStreaming: mode === 'token',
            }
        };

        const client = clientRef.current;
        const controller = new AbortController();
        textAbortRef.current = controller;

        try {
            setConnection('connecting');

            if (mode === 'step') {
                // Step-by-step streaming
                addMessage({ role: 'assistant', text: '', timestamp: new Date() });

                for await (const step of client.executeStepByStep(request, controller.signal)) {
                    handleStep(step);
                }
            } else {
                // Token-level streaming
                addMessage({ role: 'assistant', text: '', timestamp: new Date() });

                for await (const chunk of client.executeTokenStreaming(request, controller.signal)) {
                    handleStep(chunk);
                }
            }

            setConnection('connected');
        } catch (error: any) {
            // Ignore abort errors (user clicked stop)
            if (error instanceof DOMException && error.name === 'AbortError') {
                setConnection('connected');
            } else {
                console.error('Execution error:', error);
                addMessage({
                    role: 'system',
                    text: `Error: ${error.message ?? error}`,
                    status: 'error',
                    timestamp: new Date()
                });
                setConnection('error');
            }
        } finally {
            textAbortRef.current = null;
            setLoading(false);
            inputRef.current?.focus();
        }
    };

    // ── Process a single SSE step/chunk ───────────────────────────────

    const handleStep = (step: AiSceneResponse) => {
        // Track conversation key
        if (step.conversationKey) {
            setConversationKey(step.conversationKey);
        }

        // AwaitingClient — add a tool message
        if (step.status === 'awaitingClient' && step.clientInteractionRequest) {
            addMessage({
                role: 'tool',
                text: `Executing client tool: ${step.clientInteractionRequest.toolName}...`,
                status: 'awaitingClient',
                toolName: step.clientInteractionRequest.toolName,
                timestamp: new Date()
            });
            return;
        }

        // CommandClient — add a tool message (fire-and-forget command)
        if (step.status === 'commandClient' && step.clientInteractionRequest) {
            addMessage({
                role: 'tool',
                text: `Executing command: ${step.clientInteractionRequest.toolName}...`,
                status: 'commandClient',
                toolName: step.clientInteractionRequest.toolName,
                timestamp: new Date()
            });
            return;
        }

        // Streaming chunk — append to last assistant message OR create new if needed
        if (step.streamingChunk) {
            setMessages(prev => {
                const lastIdx = prev.length - 1;
                const lastMsg = prev[lastIdx];

                // If last message is assistant, append chunk
                if (lastMsg && lastMsg.role === 'assistant') {
                    const updated = [...prev];
                    updated[lastIdx] = {
                        ...lastMsg,
                        text: lastMsg.text + step.streamingChunk,
                        status: step.status,
                    };
                    return updated;
                }

                // Otherwise, create new assistant message for streaming
                return [...prev, {
                    role: 'assistant',
                    text: step.streamingChunk ?? '',
                    status: step.status,
                    timestamp: new Date()
                }];
            });
            return;
        }

        // Regular step with message — append to last assistant OR create new if needed
        if (step.message) {
            setMessages(prev => {
                const lastIdx = prev.length - 1;
                const lastMsg = prev[lastIdx];

                // If last message is assistant, update it
                if (lastMsg && lastMsg.role === 'assistant') {
                    const updated = [...prev];
                    updated[lastIdx] = {
                        ...lastMsg,
                        text: lastMsg.text
                            ? lastMsg.text + '\n' + `[${step.status}] ${step.message}`
                            : `[${step.status}] ${step.message}`,
                        status: step.status,
                    };
                    return updated;
                }

                // Otherwise, create new assistant message (e.g., after tool/system message)
                return [...prev, {
                    role: 'assistant',
                    text: `[${step.status}] ${step.message}`,
                    status: step.status,
                    timestamp: new Date()
                }];
            });
        }

        // FunctionRequest/Completed — show as system info
        if (step.status === 'functionRequest' || step.status === 'functionCompleted') {
            const label = step.status === 'functionRequest' ? 'Calling' : 'Completed';
            addMessage({
                role: 'system',
                text: `${label}: ${step.functionName ?? 'unknown'}`,
                status: step.status,
                timestamp: new Date()
            });
        }

        // Cost data — attach to last assistant message when available
        if ((step.inputTokens ?? 0) > 0 || (step.cost ?? 0) > 0 || (step.totalCost ?? 0) > 0) {
            setMessages(prev => {
                const idx = [...prev].map((m, i) => ({ m, i })).reverse().find(x => x.m.role === 'assistant')?.i;
                if (idx === undefined) return prev;
                const updated = [...prev];
                updated[idx] = {
                    ...updated[idx],
                    inputTokens: step.inputTokens,
                    outputTokens: step.outputTokens,
                    cachedInputTokens: step.cachedInputTokens,
                    totalTokens: step.totalTokens,
                    cost: step.cost,
                    totalCost: step.totalCost,
                };
                return updated;
            });
        }
    };

    // ── Key handler ───────────────────────────────────────────────────

    const handleKey = (e: React.KeyboardEvent) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            handleSend();
        }
    };

    // ── Clear chat ────────────────────────────────────────────────────

    const handleClear = () => {
        setMessages([]);
        setConversationKey(undefined);
        setLoading(false);
        setConnection(clientRef.current ? 'connected' : 'disconnected');
    };

    // ── Render ────────────────────────────────────────────────────────

    return (
        <div className="App" style={{ height: '100vh', display: 'flex', flexDirection: 'row', backgroundColor: '#1e1e1e' }}>
            {/* Sidebar - Conversation List */}
            {showSidebar && (
                <div style={{
                    width: '350px',
                    backgroundColor: '#252525',
                    borderRight: '1px solid #3a3a3a',
                    display: 'flex',
                    flexDirection: 'column',
                    flexShrink: 0,
                }}>
                    {/* Sidebar Header */}
                    <div style={{
                        padding: '12px 16px',
                        borderBottom: '1px solid #3a3a3a',
                        backgroundColor: '#282c34',
                    }}>
                        <h2 style={{ margin: '0 0 12px 0', fontSize: '16px', color: '#61dafb' }}>Conversations</h2>

                        {/* Search Input */}
                        <input
                            type="text"
                            placeholder="Search messages..."
                            value={searchText}
                            onChange={e => setSearchText(e.target.value)}
                            style={{
                                width: '100%',
                                padding: '6px 10px',
                                fontSize: '12px',
                                borderRadius: '6px',
                                border: '1px solid #444',
                                backgroundColor: '#1e1e1e',
                                color: '#eee',
                                marginBottom: '8px',
                            }}
                        />

                        {/* Filter Toggles */}
                        <div style={{ display: 'flex', gap: '8px', fontSize: '12px' }}>
                            <label style={{ display: 'flex', alignItems: 'center', gap: '4px', color: '#aaa', cursor: 'pointer' }}>
                                <input
                                    type="checkbox"
                                    checked={showPublic}
                                    onChange={e => setShowPublic(e.target.checked)}
                                />
                                Public
                            </label>
                            <label style={{ display: 'flex', alignItems: 'center', gap: '4px', color: '#aaa', cursor: 'pointer' }}>
                                <input
                                    type="checkbox"
                                    checked={showPrivate}
                                    onChange={e => setShowPrivate(e.target.checked)}
                                />
                                Private
                            </label>
                            <button
                                onClick={loadConversations}
                                disabled={loadingConversations}
                                style={{
                                    marginLeft: 'auto',
                                    padding: '4px 10px',
                                    fontSize: '11px',
                                    borderRadius: '4px',
                                    border: '1px solid #444',
                                    backgroundColor: '#333',
                                    color: '#aaa',
                                    cursor: loadingConversations ? 'not-allowed' : 'pointer',
                                }}
                            >
                                {loadingConversations ? 'Loading...' : 'Refresh'}
                            </button>
                        </div>
                    </div>

                    {/* Conversation List */}
                    <div style={{
                        flex: 1,
                        overflowY: 'auto',
                        padding: '8px',
                    }}>
                        {loadingConversations && (
                            <div style={{ textAlign: 'center', color: '#777', padding: '20px', fontSize: '13px' }}>
                                Loading...
                            </div>
                        )}
                        {!loadingConversations && conversations.length === 0 && (
                            <div style={{ textAlign: 'center', color: '#555', padding: '20px', fontSize: '13px' }}>
                                No conversations found
                            </div>
                        )}
                        {!loadingConversations && conversations.map(conv => (
                            <div
                                key={conv.conversationKey}
                                style={{
                                    padding: '10px',
                                    marginBottom: '6px',
                                    borderRadius: '8px',
                                    backgroundColor: conversationKey === conv.conversationKey ? '#2d5a8a' : '#2d2d2d',
                                    border: '1px solid ' + (conversationKey === conv.conversationKey ? '#61dafb' : '#3a3a3a'),
                                    cursor: 'pointer',
                                    transition: 'background-color 0.2s',
                                }}
                                onMouseEnter={e => {
                                    if (conversationKey !== conv.conversationKey) {
                                        e.currentTarget.style.backgroundColor = '#353535';
                                    }
                                }}
                                onMouseLeave={e => {
                                    if (conversationKey !== conv.conversationKey) {
                                        e.currentTarget.style.backgroundColor = '#2d2d2d';
                                    }
                                }}
                            >
                                <div onClick={() => handleLoadConversation(conv.conversationKey)}>
                                    <div style={{
                                        display: 'flex',
                                        justifyContent: 'space-between',
                                        alignItems: 'center',
                                        marginBottom: '4px',
                                    }}>
                                        <span style={{
                                            fontSize: '11px',
                                            color: '#888',
                                        }}>
                                            {new Date(conv.timestamp).toLocaleString()}
                                        </span>
                                        <span style={{
                                            fontSize: '10px',
                                            padding: '2px 6px',
                                            borderRadius: '4px',
                                            backgroundColor: conv.isPublic ? '#2d5a2d' : '#5a2d2d',
                                            color: conv.isPublic ? '#5cb85c' : '#f0ad4e',
                                        }}>
                                            {conv.isPublic ? 'Public' : 'Private'}
                                        </span>
                                    </div>
                                    <div style={{
                                        fontSize: '13px',
                                        color: '#ddd',
                                        marginBottom: '6px',
                                        overflow: 'hidden',
                                        textOverflow: 'ellipsis',
                                        whiteSpace: 'nowrap',
                                    }}>
                                        {/* Show last user message for better preview */}
                                        {[...conv.messages].reverse().find(m => m.role === 'user')?.text 
                                            || conv.messages[0]?.text 
                                            || 'Empty conversation'}
                                    </div>
                                    <div style={{ fontSize: '11px', color: '#666' }}>
                                        {conv.messages.length} message{conv.messages.length !== 1 ? 's' : ''}
                                        {conv.userId && ` • User: ${conv.userId}`}
                                    </div>
                                </div>
                                <button
                                    onClick={(e) => {
                                        e.stopPropagation();
                                        handleDeleteConversation(conv.conversationKey);
                                    }}
                                    style={{
                                        marginTop: '8px',
                                        width: '100%',
                                        padding: '4px 8px',
                                        fontSize: '11px',
                                        borderRadius: '4px',
                                        border: '1px solid #5a2d2d',
                                        backgroundColor: 'transparent',
                                        color: '#d9534f',
                                        cursor: 'pointer',
                                    }}
                                >
                                    Delete
                                </button>
                            </div>
                        ))}
                    </div>
                </div>
            )}

            {/* Main Chat Area */}
            <div style={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
            {/* Header */}
            <header style={{
                padding: '12px 20px',
                backgroundColor: '#282c34',
                borderBottom: '1px solid #3a3a3a',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                flexShrink: 0,
            }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                    <button
                        onClick={() => setShowSidebar(!showSidebar)}
                        style={{
                            padding: '6px 12px',
                            fontSize: '12px',
                            borderRadius: '6px',
                            border: '1px solid #444',
                            backgroundColor: showSidebar ? '#61dafb' : '#333',
                            color: showSidebar ? '#1e1e1e' : '#aaa',
                            cursor: 'pointer',
                            fontWeight: 600,
                        }}
                    >
                        {showSidebar ? 'Hide' : 'Show'} Conversations
                    </button>
                    <h1 style={{ margin: 0, fontSize: '18px', color: '#61dafb' }}>
                        PlayFramework Test Chat
                    </h1>
                    <span style={{
                        fontSize: '11px',
                        padding: '2px 8px',
                        borderRadius: '10px',
                        backgroundColor: connection === 'connected' ? '#2d5a2d' : connection === 'error' ? '#5a2d2d' : '#4a4a2d',
                        color: connection === 'connected' ? '#5cb85c' : connection === 'error' ? '#d9534f' : '#f0ad4e',
                    }}>
                        {connection}
                    </span>
                </div>

                <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
                    {/* Mode toggle */}
                    <div style={{
                        display: 'flex',
                        borderRadius: '6px',
                        overflow: 'hidden',
                        border: '1px solid #444',
                    }}>
                        <button
                            onClick={() => setMode('step')}
                            style={{
                                padding: '5px 12px',
                                fontSize: '12px',
                                border: 'none',
                                cursor: 'pointer',
                                backgroundColor: mode === 'step' ? '#61dafb' : '#333',
                                color: mode === 'step' ? '#1e1e1e' : '#aaa',
                                fontWeight: mode === 'step' ? 700 : 400,
                            }}
                        >
                            Step-by-Step
                        </button>
                        <button
                            onClick={() => setMode('token')}
                            style={{
                                padding: '5px 12px',
                                fontSize: '12px',
                                border: 'none',
                                cursor: 'pointer',
                                backgroundColor: mode === 'token' ? '#ff6b6b' : '#333',
                                color: mode === 'token' ? '#fff' : '#aaa',
                                fontWeight: mode === 'token' ? 700 : 400,
                            }}
                        >
                            Token Streaming
                        </button>
                        <button
                            onClick={() => setMode('voice')}
                            style={{
                                padding: '5px 12px',
                                fontSize: '12px',
                                border: 'none',
                                cursor: 'pointer',
                                backgroundColor: mode === 'voice' ? '#51cf66' : '#333',
                                color: mode === 'voice' ? '#1e1e1e' : '#aaa',
                                fontWeight: mode === 'voice' ? 700 : 400,
                            }}
                        >
                             Voice
                        </button>
                    </div>

                    {/* Execution mode */}
                    <select
                        value={executionMode}
                        onChange={e => setExecutionMode(e.target.value as ExecutionModeOption)}
                        style={{
                            padding: '5px 8px',
                            fontSize: '12px',
                            borderRadius: '6px',
                            border: '1px solid #444',
                            backgroundColor: '#1e1e1e',
                            color: '#aaa',
                            cursor: 'pointer',
                        }}
                    >
                        <option value="Direct">Direct</option>
                        <option value="Planning">Planning</option>
                        <option value="DynamicChaining">DynamicChaining</option>
                        <option value="Scene">Scene</option>
                    </select>

                    <button onClick={handleClear} style={{
                        padding: '5px 12px',
                        fontSize: '12px',
                        borderRadius: '6px',
                        border: '1px solid #555',
                        backgroundColor: 'transparent',
                        color: '#aaa',
                        cursor: 'pointer',
                    }}>
                        Clear
                    </button>

                    {/* Factory (backend) selector */}
                    <div style={{
                        display: 'flex',
                        borderRadius: '6px',
                        overflow: 'hidden',
                        border: '1px solid #444',
                    }}>
                        <button
                            onClick={() => setActiveFactory('default')}
                            style={{
                                padding: '5px 10px',
                                fontSize: '11px',
                                border: 'none',
                                cursor: 'pointer',
                                backgroundColor: activeFactory === 'default' ? '#7952b3' : '#333',
                                color: activeFactory === 'default' ? '#fff' : '#aaa',
                                fontWeight: activeFactory === 'default' ? 700 : 400,
                            }}
                        >
                             Azure OpenAI
                        </button>
                        <button
                            onClick={() => setActiveFactory('foundry')}
                            style={{
                                padding: '5px 10px',
                                fontSize: '11px',
                                border: 'none',
                                cursor: 'pointer',
                                backgroundColor: activeFactory === 'foundry' ? '#20c997' : '#333',
                                color: activeFactory === 'foundry' ? '#1e1e1e' : '#aaa',
                                fontWeight: activeFactory === 'foundry' ? 700 : 400,
                            }}
                        >
                             Foundry Local
                        </button>
                    </div>
                    </div>
                </header>

            {/* Messages */}
            <div style={{
                flex: 1,
                overflowY: 'auto',
                padding: '16px 20px',
                display: 'flex',
                flexDirection: 'column',
                gap: '8px',
            }}>
                {messages.length === 0 && (
                    <div style={{ textAlign: 'center', color: '#555', marginTop: '40px', fontSize: '14px' }}>
                        {mode === 'voice' ? (
                            <>
                                <p style={{ fontSize: '32px', marginBottom: '8px' }}>[mic]</p>
                                <p>Voice mode active ({voiceEngine === 'server' ? 'Server' : 'Browser'} engine). Tap the mic button to talk.</p>
                                <p style={{ fontSize: '12px', color: '#444' }}>
                                    {voiceEngine === 'server'
                                        ? 'Audio is sent to server (Whisper STT → LLM → TTS-1).'
                                        : 'Speech recognition and synthesis run entirely in your browser.'}
                                </p>
                            </>
                        ) : (
                            <>
                                <p>Send a message to start chatting.</p>
                                <p style={{ fontSize: '12px', color: '#444' }}>
                                    Try: "Where am I right now?" (triggers <code>getCurrentLocation</code>)
                                    <br />
                                    or: "Delete my account" (triggers <code>getUserConfirmation</code>)
                                </p>
                            </>
                        )}
                    </div>
                )}

                {messages.map((msg, i) => (
                    <div
                        key={i}
                        style={{
                            alignSelf: msg.role === 'user' ? 'flex-end' : 'flex-start',
                            maxWidth: msg.role === 'system' || msg.role === 'tool' ? '100%' : '75%',
                            padding: msg.role === 'system' || msg.role === 'tool' ? '4px 10px' : '10px 14px',
                            borderRadius: '10px',
                            backgroundColor:
                                msg.role === 'user' ? '#2d5a8a'
                                : msg.role === 'assistant' ? '#2d2d2d'
                                : msg.role === 'tool' ? '#3d2d1d'
                                : 'transparent',
                            color:
                                msg.role === 'user' ? '#e8f0fe'
                                : msg.role === 'system' ? '#777'
                                : msg.role === 'tool' ? '#f0ad4e'
                                : '#ddd',
                            fontSize: msg.role === 'system' || msg.role === 'tool' ? '12px' : '14px',
                            fontStyle: msg.role === 'system' ? 'italic' : 'normal',
                            whiteSpace: 'pre-wrap',
                            wordBreak: 'break-word',
                            lineHeight: 1.5,
                        }}
                    >
                        {msg.role !== 'user' && statusBadge(msg.status)}
                        {msg.text || (msg.role === 'assistant' && loading ? '...' : '')}

                        {/* Display attached content (images, audio, video, PDFs) */}
                        {msg.contents && msg.contents.length > 0 && (
                            <div style={{ marginTop: '8px' }}>
                                {msg.contents.map((content, contentIdx) => (
                                    <ContentViewer key={contentIdx} content={content} />
                                ))}
                            </div>
                        )}

                        {/* Cost / token badge */}
                        {msg.role === 'assistant' && (msg.inputTokens ?? 0) > 0 && (
                            <div style={{
                                marginTop: '8px',
                                paddingTop: '6px',
                                borderTop: '1px solid #3a3a3a',
                                display: 'flex',
                                flexWrap: 'wrap',
                                gap: '6px',
                                fontSize: '11px',
                                color: '#888',
                            }}>
                                <span title="Input tokens">&#8593; {msg.inputTokens} in</span>
                                {(msg.cachedInputTokens ?? 0) > 0 && (
                                    <span title="Cached input tokens" style={{ color: '#6c9' }}>&#9830; {msg.cachedInputTokens} cached</span>
                                )}
                                <span title="Output tokens">&#8595; {msg.outputTokens} out</span>
                                <span title="Total tokens">= {msg.totalTokens} total</span>
                                {(msg.cost ?? 0) > 0 && (
                                    <span title="Request cost" style={{ color: '#fa3', marginLeft: '4px' }}>
                                        ${msg.cost!.toFixed(6)}
                                    </span>
                                )}
                                {(msg.totalCost ?? 0) > 0 && (
                                    <span title="Cumulative session cost" style={{ color: '#f88', marginLeft: '2px' }}>
                                        (total: ${msg.totalCost!.toFixed(6)})
                                    </span>
                                )}
                            </div>
                        )}
                    </div>
                ))}
                    <div ref={messagesEndRef} />
                </div>

            {/* Attachment preview strip */}
            {pendingAttachments.length > 0 && (
                <div style={{
                    padding: '8px 20px',
                    backgroundColor: '#282c34',
                    borderTop: '1px solid #3a3a3a',
                    display: 'flex',
                    gap: '8px',
                    flexWrap: 'wrap',
                    alignItems: 'center',
                }}>
                    {pendingAttachments.map((att, i) => {
                        // Build a small thumbnail or icon for each attachment
                        const isImage = att.type === 'image';
                        const thumbSrc = isImage && att.data
                            ? `data:${att.mediaType};base64,${att.data}`
                            : undefined;

                        const icon = att.type === 'audio' ? '[audio]'
                            : att.type === 'video' ? '[video]'
                            : att.type === 'image' ? '[image]'
                            : '[file]';

                        return (
                            <div key={i} style={{
                                display: 'flex',
                                alignItems: 'center',
                                gap: '6px',
                                padding: '4px 10px',
                                borderRadius: '6px',
                                backgroundColor: '#1e1e1e',
                                border: '1px solid #444',
                                fontSize: '12px',
                                color: '#ccc',
                            }}>
                                {thumbSrc ? (
                                    <img src={thumbSrc} alt="" style={{
                                        width: '28px',
                                        height: '28px',
                                        objectFit: 'cover',
                                        borderRadius: '4px',
                                    }} />
                                ) : (
                                    <span>{icon}</span>
                                )}
                                <span style={{ maxWidth: '120px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                                    {att.name || att.type}
                                </span>
                                <button
                                    onClick={() => removeAttachment(i)}
                                    style={{
                                        background: 'none',
                                        border: 'none',
                                        color: '#d9534f',
                                    cursor: 'pointer',
                                    fontSize: '14px',
                                    padding: '0 2px',
                                    lineHeight: 1,
                                }}
                                title="Remove"
                            >
                                x
                                </button>
                            </div>
                        );
                    })}
                </div>
            )}

            {/* Hidden file input */}
            <input
                ref={fileInputRef}
                type="file"
                multiple
                accept="image/*,audio/*,video/*,application/pdf,.txt,.csv,.json,.xml,.md,.doc,.docx,.xls,.xlsx"
                onChange={handleFileSelect}
                style={{ display: 'none' }}
            />

            {/* Input bar */}
            <div style={{
                padding: '12px 20px',
                backgroundColor: '#282c34',
                borderTop: pendingAttachments.length > 0 ? 'none' : '1px solid #3a3a3a',
                display: 'flex',
                gap: '8px',
                alignItems: 'center',
                flexShrink: 0,
            }}>
                {/* Attachment buttons (hidden in voice mode) */}
                {mode !== 'voice' && (
                <button
                    onClick={() => fileInputRef.current?.click()}
                    disabled={loading}
                    title="Attach file"
                    style={{
                        padding: '8px 10px',
                        fontSize: '16px',
                        borderRadius: '8px',
                        border: '1px solid #444',
                        backgroundColor: '#333',
                        color: '#aaa',
                        cursor: loading ? 'not-allowed' : 'pointer',
                        lineHeight: 1,
                    }}
                >
                    [attach]
                </button>
                )}
                <button
                    onClick={mode === 'voice' ? handleVoiceRecord : handleAudioRecord}
                    onPointerUp={mode === 'voice' ? handleVoicePointerUp : undefined}
                    disabled={loading || (mode !== 'voice' && isRecording)}
                    title={
                        mode === 'voice'
                            ? (isRecording
                                ? `Recording... ${recordingSeconds}s${voiceRecordingMode === 'vad' ? ' (auto-stop on silence)' : ''}`
                                : isVoiceProcessing
                                    ? 'Tap [mic] to stop and start over'
                                    : voiceRecordingMode === 'vad'
                                        ? 'Tap to talk (auto-stops on silence)'
                                        : voiceRecordingMode === 'pushToTalk'
                                            ? 'Tap to start/stop'
                                            : 'Hold to talk')
                            : (isRecording ? `Recording... ${recordingSeconds}s` : 'Record audio (10s)')
                    }
                    style={{
                        padding: '8px 10px',
                        fontSize: '16px',
                        borderRadius: '8px',
                        border: '1px solid ' + (
                            isRecording ? '#d9534f'
                            : mode === 'voice' ? (isVoiceProcessing ? '#ff6b6b' : '#51cf66')
                            : '#444'
                        ),
                        backgroundColor: isRecording ? '#5a2d2d'
                            : mode === 'voice' ? (isVoiceProcessing ? '#5a2d2d' : '#2d5a2d')
                            : '#333',
                        color: isRecording ? '#ff6b6b'
                            : mode === 'voice' ? (isVoiceProcessing ? '#ff6b6b' : '#51cf66')
                            : '#aaa',
                        cursor: loading ? 'not-allowed' : 'pointer',
                        lineHeight: 1,
                        minWidth: (isRecording || isVoiceProcessing) ? '70px' : undefined,
                    }}
                >
                    {isRecording ? `[rec] ${recordingSeconds}s` : isVoiceProcessing ? '[stop]' : '[mic]'}
                </button>
                {mode !== 'voice' && (
                <button
                    onClick={handleCameraCapture}
                    disabled={loading}
                    title="Capture photo"
                    style={{
                        padding: '8px 10px',
                        fontSize: '16px',
                        borderRadius: '8px',
                        border: '1px solid #444',
                        backgroundColor: '#333',
                        color: '#aaa',
                        cursor: loading ? 'not-allowed' : 'pointer',
                        lineHeight: 1,
                    }}
                >
                    [camera]
                </button>
                )}

                {mode === 'voice' ? (
                    /* Voice mode: status indicator + engine/mode selectors */
                    <>
                        <div style={{
                            flex: 1,
                            padding: '10px 14px',
                            fontSize: '14px',
                            borderRadius: '8px',
                            border: '1px solid ' + (isRecording ? '#d9534f' : isVoiceProcessing ? '#51cf66' : '#444'),
                            backgroundColor: '#1e1e1e',
                            color: isRecording ? '#ff6b6b' : isVoiceProcessing ? '#51cf66' : '#777',
                            textAlign: 'center',
                        }}>
                            {voiceEngine === 'browser'
                                ? (isVoiceProcessing ? 'Speaking... tap [mic] to stop & restart' : 'Tap [mic] — browser STT → LLM → browser TTS')
                                : (isRecording
                                    ? `Recording... ${recordingSeconds}s${voiceRecordingMode === 'vad' ? ' · listening for silence' : voiceRecordingMode === 'pushToTalk' ? ' · tap [mic] to stop' : ' · release to send'}`
                                    : isVoiceProcessing ? 'Responding... tap [mic] to stop & restart'
                                    : voiceRecordingMode === 'vad' ? 'Tap [mic] — auto-stops on silence'
                                    : voiceRecordingMode === 'pushToTalk' ? 'Tap [mic] to start, tap again to stop & send'
                                    : 'Hold [mic] to talk, release to send')}
                        </div>
                        {/* Voice engine selector */}
                        <select
                            value={voiceEngine}
                            onChange={e => setVoiceEngine(e.target.value as VoiceEngine)}
                            disabled={isRecording || isVoiceProcessing}
                            title="Voice engine"
                            style={{
                                padding: '8px 6px',
                                fontSize: '11px',
                                borderRadius: '8px',
                                border: '1px solid #444',
                                backgroundColor: '#1e1e1e',
                                color: '#aaa',
                                cursor: (isRecording || isVoiceProcessing) ? 'not-allowed' : 'pointer',
                            }}
                        >
                            <option value="server">Server</option>
                            <option value="browser"
                                disabled={!BrowserVoiceClient.isSupported()}
                            >
                                Browser{!BrowserVoiceClient.isSupported() ? ' (n/a)' : ''}
                            </option>
                        </select>
                        {/* Recording mode (server only) / streaming mode (browser only) */}
                        {voiceEngine === 'server' ? (
                            <select
                                value={voiceRecordingMode}
                                onChange={e => setVoiceRecordingMode(e.target.value as VoiceRecordingMode)}
                                disabled={isRecording || isVoiceProcessing}
                                title="Voice recording mode"
                                style={{
                                    padding: '8px 6px',
                                    fontSize: '11px',
                                    borderRadius: '8px',
                                    border: '1px solid #444',
                                    backgroundColor: '#1e1e1e',
                                    color: '#aaa',
                                    cursor: (isRecording || isVoiceProcessing) ? 'not-allowed' : 'pointer',
                                }}
                            >
                                <option value="vad">VAD (auto)</option>
                                <option value="pushToTalk">Push-to-talk</option>
                                <option value="holdToTalk">Hold-to-talk</option>
                            </select>
                        ) : (
                            <>
                                <select
                                    value={browserVoiceLang}
                                    onChange={e => setBrowserVoiceLang(e.target.value)}
                                    disabled={isVoiceProcessing}
                                    title="Speech language"
                                    style={{
                                        padding: '8px 6px',
                                        fontSize: '11px',
                                        borderRadius: '8px',
                                        border: '1px solid #444',
                                        backgroundColor: '#1e1e1e',
                                        color: '#aaa',
                                        cursor: isVoiceProcessing ? 'not-allowed' : 'pointer',
                                    }}
                                >
                                    <option value="it-IT">it-IT Italiano</option>
                                    <option value="en-US">en-US English</option>
                                    <option value="en-GB">en-GB English UK</option>
                                    <option value="es-ES">es-ES Español</option>
                                    <option value="fr-FR">fr-FR Français</option>
                                    <option value="de-DE">de-DE Deutsch</option>
                                    <option value="pt-BR">pt-BR Português</option>
                                    <option value="ja-JP">ja-JP 日本語</option>
                                    <option value="zh-CN">zh-CN 中文</option>
                                    <option value="ko-KR">ko-KR 한국어</option>
                                    <option value="ar-SA">ar-SA العربية</option>
                                    <option value="ru-RU">ru-RU Русский</option>
                                    <option value="hi-IN">hi-IN हिन्दी</option>
                                    <option value="nl-NL">nl-NL Nederlands</option>
                                    <option value="pl-PL">pl-PL Polski</option>
                                    <option value="sv-SE">sv-SE Svenska</option>
                                    <option value="tr-TR">tr-TR Türkçe</option>
                                </select>
                                <select
                                    value={browserVoiceStreamingMode}
                                    onChange={e => setBrowserVoiceStreamingMode(e.target.value as BrowserVoiceStreamingMode)}
                                    disabled={isVoiceProcessing}
                                    title="Streaming mode"
                                    style={{
                                        padding: '8px 6px',
                                        fontSize: '11px',
                                        borderRadius: '8px',
                                        border: '1px solid #444',
                                        backgroundColor: '#1e1e1e',
                                        color: '#aaa',
                                        cursor: isVoiceProcessing ? 'not-allowed' : 'pointer',
                                    }}
                                >
                                    <option value="stepByStep">Step-by-step</option>
                                    <option value="tokenStreaming">Token stream</option>
                                </select>
                            </>
                        )}
                    </>
                ) : (
                    /* Normal mode: text input + send button */
                    <>
                        <input
                            ref={inputRef}
                            type="text"
                            value={input}
                            onChange={e => setInput(e.target.value)}
                            onKeyDown={handleKey}
                            placeholder={loading ? 'Waiting for response...' : 'Type a message (or attach files)...'}
                            disabled={loading || connection === 'error'}
                            autoFocus
                            style={{
                                flex: 1,
                                padding: '10px 14px',
                                fontSize: '14px',
                                borderRadius: '8px',
                                border: '1px solid #444',
                                backgroundColor: '#1e1e1e',
                                color: '#eee',
                                outline: 'none',
                            }}
                        />
                        {loading ? (
                            <button
                                onClick={() => {
                                    if (textAbortRef.current) {
                                        textAbortRef.current.abort();
                                        textAbortRef.current = null;
                                    }
                                }}
                                style={{
                                    padding: '10px 20px',
                                    fontSize: '14px',
                                    borderRadius: '8px',
                                    border: 'none',
                                    backgroundColor: '#d9534f',
                                    color: '#fff',
                                    cursor: 'pointer',
                                    fontWeight: 600,
                                }}
                            >
                                Stop
                            </button>
                        ) : (
                            <button
                                onClick={handleSend}
                                disabled={(!input.trim() && pendingAttachments.length === 0) || connection === 'error'}
                                style={{
                                    padding: '10px 20px',
                                    fontSize: '14px',
                                    borderRadius: '8px',
                                    border: 'none',
                                    backgroundColor: '#61dafb',
                                    color: '#1e1e1e',
                                    cursor: 'pointer',
                                    fontWeight: 600,
                                }}
                            >
                                Send
                            </button>
                        )}
                    </>
                )}
            </div>

            {/* Footer info */}
            <div style={{
                padding: '4px 20px 8px',
                backgroundColor: '#282c34',
                fontSize: '11px',
                color: '#555',
                display: 'flex',
                justifyContent: 'space-between',
            }}>
                <span>API: {API_BASE}/{activeFactory}</span>
                <span>
                    Mode: {mode === 'step' ? 'Step-by-Step' : mode === 'token' ? 'Token Streaming' : `Voice (${voiceEngine === 'server' ? 'Server' : 'Browser'})`}
                    {conversationKey && ` | Conv: ${conversationKey.substring(0, 8)}...`}
                    {voiceTranscript && mode === 'voice' && ` | "${voiceTranscript.substring(0, 30)}..."`}
                    </span>
                </div>
            </div>
        </div>
    );
}

export default App;
