import React, { useState } from 'react';
import { usePlayFramework, PlayFrameworkServices, PlayFrameworkRequest } from './rystem/src';
import './App.css';

// Configure PlayFramework client
PlayFrameworkServices.configure("default", "https://localhost:5001/api/ai", settings => {
    // Add Authorization header (example)
    settings.addHeadersEnricher(async (url, method, headers, body) => {
        return {
            ...headers,
            // "Authorization": `Bearer ${yourToken}`
        };
    });
});

function App() {
    const client = usePlayFramework("default");
    const [messages, setMessages] = useState<string[]>([]);
    const [prompt, setPrompt] = useState<string>("");
    const [loading, setLoading] = useState<boolean>(false);

    const handleSendStepByStep = async () => {
        if (!prompt.trim()) return;

        setLoading(true);
        setMessages([]);

        const newMessages: string[] = [];

        try {
            const request: PlayFrameworkRequest = {
                prompt: prompt,
                sceneName: "Chat"
            };

            // Step-by-step streaming
            for await (const step of client.executeStepByStep(request)) {
                if (step.message) {
                    newMessages.push(`[${step.status}] ${step.message}`);
                    setMessages([...newMessages]);
                }
            }
        } catch (error) {
            console.error("Error:", error);
            setMessages([...newMessages, `❌ Error: ${error}`]);
        } finally {
            setLoading(false);
        }
    };

    const handleSendTokenStreaming = async () => {
        if (!prompt.trim()) return;

        setLoading(true);
        setMessages([]);

        let fullText = "";

        try {
            const request: PlayFrameworkRequest = {
                prompt: prompt,
                sceneName: "Chat"
            };

            // Token-level streaming
            for await (const chunk of client.executeTokenStreaming(request)) {
                if (chunk.message) {
                    fullText += chunk.message;
                    setMessages([fullText]);
                }
            }
        } catch (error) {
            console.error("Error:", error);
            setMessages([fullText, `❌ Error: ${error}`]);
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="App">
            <header className="App-header">
                <h1>Rystem PlayFramework Client - Test App</h1>

                <div style={{ marginBottom: '20px', width: '80%' }}>
                    <input
                        type="text"
                        value={prompt}
                        onChange={(e) => setPrompt(e.target.value)}
                        placeholder="Enter your prompt..."
                        style={{
                            width: '100%',
                            padding: '10px',
                            fontSize: '16px',
                            borderRadius: '5px',
                            border: '1px solid #ccc'
                        }}
                        disabled={loading}
                    />
                </div>

                <div style={{ display: 'flex', gap: '10px', marginBottom: '20px' }}>
                    <button
                        onClick={handleSendStepByStep}
                        disabled={loading}
                        style={{
                            padding: '10px 20px',
                            fontSize: '16px',
                            borderRadius: '5px',
                            border: 'none',
                            backgroundColor: '#61dafb',
                            color: '#282c34',
                            cursor: loading ? 'not-allowed' : 'pointer'
                        }}
                    >
                        {loading ? "Loading..." : "Send (Step-by-Step)"}
                    </button>

                    <button
                        onClick={handleSendTokenStreaming}
                        disabled={loading}
                        style={{
                            padding: '10px 20px',
                            fontSize: '16px',
                            borderRadius: '5px',
                            border: 'none',
                            backgroundColor: '#ff6b6b',
                            color: 'white',
                            cursor: loading ? 'not-allowed' : 'pointer'
                        }}
                    >
                        {loading ? "Loading..." : "Send (Token Streaming)"}
                    </button>
                </div>

                <div style={{
                    width: '80%',
                    maxHeight: '400px',
                    overflowY: 'auto',
                    backgroundColor: '#1e1e1e',
                    padding: '20px',
                    borderRadius: '10px',
                    textAlign: 'left'
                }}>
                    {messages.length === 0 && !loading && (
                        <p style={{ color: '#666' }}>No messages yet. Enter a prompt and click Send.</p>
                    )}
                    {messages.map((msg, i) => (
                        <div key={i} style={{ marginBottom: '10px', color: '#61dafb' }}>
                            {msg}
                        </div>
                    ))}
                </div>
            </header>
        </div>
    );
}

export default App;
