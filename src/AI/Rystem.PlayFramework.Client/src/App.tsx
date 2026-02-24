import React, { useState, useRef, useEffect, useCallback } from 'react';
import {
    PlayFrameworkServices,
    PlayFrameworkClient,
    AIContentConverter,
    ContentUrlConverter,
    CommandResultHelper,
    type PlayFrameworkRequest,
    type AiSceneResponse,
    type AiResponseStatus,
    type SceneExecutionMode,
    type StoredConversation,
    type StoredMessage,
    type AIContent,
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
}

type StreamMode = 'step' | 'token';
type ConnectionStatus = 'disconnected' | 'connecting' | 'connected' | 'error';
type ExecutionModeOption = SceneExecutionMode;

// ─── Configuration ───────────────────────────────────────────────────────

const API_BASE = 'http://localhost:5158/api/ai';
const FACTORY_NAME = 'default';

// Configure PlayFramework client (runs once at module load)
const clientReady = PlayFrameworkServices.configure(FACTORY_NAME, API_BASE, settings => {
    settings.timeout = 120_000; // 2 min for long AI responses
    settings.maxReconnectAttempts = 3;
    settings.reconnectBaseDelay = 1000;
});

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
            return CommandResultHelper.ok(`Saved "${args.key}" to local storage`);
        } catch (error: any) {
            return CommandResultHelper.fail(`Storage error: ${error.message}`);
        }
    }, { feedbackMode: 'always' });
}

// ─── Helpers ─────────────────────────────────────────────────────────────

const statusColors: Partial<Record<AiResponseStatus, string>> = {
    // Initialization & Loading
    Initializing: '#888',
    LoadingCache: '#6c757d',
    ExecutingMainActors: '#7952b3',

    // Planning & Execution
    Planning: '#f0ad4e',
    ExecutingScene: '#5bc0de',
    Running: '#17a2b8',

    // Function/Tool Execution
    FunctionRequest: '#d9534f',
    FunctionCompleted: '#5cb85c',
    ToolSkipped: '#6c757d',

    // Streaming & Response Generation
    Streaming: '#61dafb',
    GeneratingFinalResponse: '#20c997',

    // Director & Summarization
    DirectorDecision: '#e83e8c',
    Summarizing: '#fd7e14',

    // Saving & Persistence
    SavingCache: '#6610f2',
    SavingMemory: '#6f42c1',

    // Client Interaction
    AwaitingClient: '#ff6b6b',

    // Final States
    Completed: '#5cb85c',
    Error: '#d9534f',
    BudgetExceeded: '#f0ad4e',
    Unauthorized: '#dc3545',
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
    // Handle function calls (tool invocations)
    if (content.type === 'functionCall') {
        const args = content.argumentsJson ? JSON.parse(content.argumentsJson) : {};
        return (
            <div style={{
                marginTop: '8px',
                padding: '10px',
                backgroundColor: '#2d3a4d',
                borderRadius: '6px',
                border: '1px solid #4a5a6d',
            }}>
                <div style={{ fontSize: '12px', color: '#61dafb', marginBottom: '6px', fontWeight: 600 }}>
                    🔧 Tool Call: {content.name}
                </div>
                {Object.keys(args).length > 0 && (
                    <pre style={{
                        fontSize: '11px',
                        color: '#aaa',
                        margin: 0,
                        whiteSpace: 'pre-wrap',
                        wordBreak: 'break-word',
                    }}>
                        {JSON.stringify(args, null, 2)}
                    </pre>
                )}
            </div>
        );
    }

    // Handle function results (tool responses)
    if (content.type === 'functionResult') {
        let result = content.resultJson;
        try {
            result = JSON.parse(content.resultJson || 'null');
            if (typeof result === 'string') {
                // If parsed result is still a string, use it directly
                result = result;
            } else if (typeof result === 'number') {
                result = result.toString();
            } else {
                // For objects/arrays, stringify with formatting
                result = JSON.stringify(result, null, 2);
            }
        } catch {
            // If parsing fails, use raw string
            result = content.resultJson || 'null';
        }

        return (
            <div style={{
                marginTop: '8px',
                padding: '10px',
                backgroundColor: '#2d4d2d',
                borderRadius: '6px',
                border: '1px solid #4a6d4a',
            }}>
                <div style={{ fontSize: '12px', color: '#5cb85c', marginBottom: '6px', fontWeight: 600 }}>
                    ✓ Tool Result
                </div>
                <pre style={{
                    fontSize: '11px',
                    color: '#ddd',
                    margin: 0,
                    whiteSpace: 'pre-wrap',
                    wordBreak: 'break-word',
                }}>
                    {result}
                </pre>
            </div>
        );
    }

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
                    📎 Attachment ({mediaType || 'unknown type'})
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

    // ─── Conversation Management State ───────────────────────────────────
    const [conversations, setConversations] = useState<StoredConversation[]>([]);
    const [searchText, setSearchText] = useState('');
    const [showPublic, setShowPublic] = useState(true);
    const [showPrivate, setShowPrivate] = useState(true);
    const [showSidebar, setShowSidebar] = useState(false);
    const [loadingConversations, setLoadingConversations] = useState(false);

    // Initialize client
    useEffect(() => {
        clientReady.then(() => {
            const client = PlayFrameworkServices.resolve(FACTORY_NAME);
            registerClientTools(client);
            clientRef.current = client;
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
        if (!text || loading || !clientRef.current) return;

        setInput('');
        setLoading(true);

        // Add user message
        addMessage({ role: 'user', text, timestamp: new Date() });

        // Generate conversationKey if not already set (first request)
        const key = conversationKey ?? crypto.randomUUID();
        if (!conversationKey) {
            setConversationKey(key);
        }

        const request: PlayFrameworkRequest = {
            message: text,
            conversationKey: key,
            settings: {
                executionMode,
                enableStreaming: mode === 'token',
            }
        };

        const client = clientRef.current;

        try {
            setConnection('connecting');

            if (mode === 'step') {
                // Step-by-step streaming
                addMessage({ role: 'assistant', text: '', timestamp: new Date() });

                for await (const step of client.executeStepByStep(request)) {
                    handleStep(step);
                }
            } else {
                // Token-level streaming
                addMessage({ role: 'assistant', text: '', timestamp: new Date() });

                for await (const chunk of client.executeTokenStreaming(request)) {
                    handleStep(chunk);
                }
            }

            setConnection('connected');
        } catch (error: any) {
            console.error('Execution error:', error);
            addMessage({
                role: 'system',
                text: `Error: ${error.message ?? error}`,
                status: 'Error',
                timestamp: new Date()
            });
            setConnection('error');
        } finally {
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
        if (step.status === 'AwaitingClient' && step.clientInteractionRequest) {
            addMessage({
                role: 'tool',
                text: `Executing client tool: ${step.clientInteractionRequest.toolName}...`,
                status: 'AwaitingClient',
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
        if (step.status === 'FunctionRequest' || step.status === 'FunctionCompleted') {
            const label = step.status === 'FunctionRequest' ? 'Calling' : 'Completed';
            addMessage({
                role: 'system',
                text: `${label}: ${step.functionName ?? 'unknown'}`,
                status: step.status,
                timestamp: new Date()
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
                        <p>Send a message to start chatting.</p>
                        <p style={{ fontSize: '12px', color: '#444' }}>
                            Try: "Where am I right now?" (triggers <code>getCurrentLocation</code>)
                            <br />
                            or: "Delete my account" (triggers <code>getUserConfirmation</code>)
                        </p>
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
                    </div>
                ))}
                    <div ref={messagesEndRef} />
                </div>

            {/* Input bar */}
            <div style={{
                padding: '12px 20px',
                backgroundColor: '#282c34',
                borderTop: '1px solid #3a3a3a',
                display: 'flex',
                gap: '10px',
                alignItems: 'center',
                flexShrink: 0,
            }}>
                <input
                    ref={inputRef}
                    type="text"
                    value={input}
                    onChange={e => setInput(e.target.value)}
                    onKeyDown={handleKey}
                    placeholder={loading ? 'Waiting for response...' : 'Type a message...'}
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
                <button
                    onClick={handleSend}
                    disabled={loading || !input.trim() || connection === 'error'}
                    style={{
                        padding: '10px 20px',
                        fontSize: '14px',
                        borderRadius: '8px',
                        border: 'none',
                        backgroundColor: loading ? '#444' : '#61dafb',
                        color: loading ? '#888' : '#1e1e1e',
                        cursor: loading ? 'not-allowed' : 'pointer',
                        fontWeight: 600,
                    }}
                >
                    {loading ? 'Sending...' : 'Send'}
                </button>
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
                <span>API: {API_BASE}/{FACTORY_NAME}</span>
                <span>
                    Mode: {mode === 'step' ? 'Step-by-Step' : 'Token Streaming'}
                    {conversationKey && ` | Conv: ${conversationKey.substring(0, 8)}...`}
                    </span>
                </div>
            </div>
        </div>
    );
}

export default App;
