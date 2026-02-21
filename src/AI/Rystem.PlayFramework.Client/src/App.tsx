import React, { useState, useRef, useEffect, useCallback } from 'react';
import {
    PlayFrameworkServices,
    PlayFrameworkClient,
    AIContentConverter,
    type PlayFrameworkRequest,
    type AiSceneResponse,
    type AiResponseStatus,
    type SceneExecutionMode
} from './rystem/src/index';
import './App.css';

// ─── Types ───────────────────────────────────────────────────────────────

interface ChatMessage {
    role: 'user' | 'assistant' | 'system' | 'tool';
    text: string;
    status?: AiResponseStatus;
    timestamp: Date;
    toolName?: string;
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
}

// ─── Helpers ─────────────────────────────────────────────────────────────

const statusColors: Partial<Record<AiResponseStatus, string>> = {
    initializing: '#888',
    planning: '#f0ad4e',
    executingScene: '#5bc0de',
    functionRequest: '#d9534f',
    functionCompleted: '#5cb85c',
    streaming: '#61dafb',
    awaitingClient: '#ff6b6b',
    completed: '#5cb85c',
    error: '#d9534f',
    budgetExceeded: '#f0ad4e',
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
                status: 'error',
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
        <div className="App" style={{ height: '100vh', display: 'flex', flexDirection: 'column', backgroundColor: '#1e1e1e' }}>
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
    );
}

export default App;
