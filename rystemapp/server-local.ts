#!/usr/bin/env tsx
/**
 * Local development server for testing MCP API
 * Run with: npm run dev:api
 * Then test with: http://localhost:3000/mcp
 */

import express from 'express';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { StreamableHTTPServerTransport } from '@modelcontextprotocol/sdk/server/streamableHttp.js';
import { readFile } from 'fs/promises';
import { join } from 'path';
import { z } from 'zod';
import cors from 'cors';

const app = express();
const PORT = process.env.PORT || 3000;

// Enable CORS for testing
app.use(
    cors({
        origin: '*',
        exposedHeaders: ['Mcp-Session-Id'],
        allowedHeaders: ['Content-Type', 'mcp-session-id']
    })
);

app.use(express.json());

// Serve static files from public directory
app.use(express.static('public'));

// Global MCP server instance (reused across requests)
let mcpServer: McpServer | null = null;

// Initialize MCP server with all tools, resources, and prompts
async function initializeMcpServer(): Promise<McpServer> {
    if (mcpServer) {
        return mcpServer;
    }

    console.log('🔧 Initializing MCP Server...');

    const server = new McpServer({
        name: 'rystem-mcp-server',
        version: '1.0.0'
    });

    // Load manifest to get all available items
    const manifestPath = join(process.cwd(), 'public', 'mcp-manifest.json');
    const manifestContent = await readFile(manifestPath, 'utf-8');
    const manifest = JSON.parse(manifestContent);

    console.log(`📦 Found ${manifest.tools?.length || 0} tools`);
    console.log(`📦 Found ${manifest.resources?.length || 0} resources`);
    console.log(`📦 Found ${manifest.prompts?.length || 0} prompts`);
    console.log(`📦 Found ${manifest.dynamicTools?.length || 0} dynamic tools`);

    // ── Dynamic tools (get-rystem-docs, -list, -search) ──────────────────────
    for (const dynamicTool of manifest.dynamicTools || []) {
        const zodSchema: Record<string, z.ZodString | z.ZodOptional<z.ZodString>> = {};
        for (const [key, config] of Object.entries(dynamicTool.inputSchema as Record<string, { type: string; description: string; required: boolean }>)) {
            zodSchema[key] = config.required
                ? z.string().describe(config.description)
                : z.string().optional().describe(config.description);
        }

        const mapping: Record<string, Record<string, string>> = {};
        const categoryInfo: Record<string, Array<{ value: string; title?: string }>> = {};
        for (const doc of dynamicTool.documents as Array<{ id: string; value: string; filename: string; metadata?: { title?: string } }>) {
            if (!mapping[doc.id]) { mapping[doc.id] = {}; categoryInfo[doc.id] = []; }
            mapping[doc.id][doc.value] = doc.filename;
            categoryInfo[doc.id].push({ value: doc.value, title: doc.metadata?.title });
        }

        // Main tool
        server.registerTool(
            dynamicTool.name,
            { title: dynamicTool.title, description: dynamicTool.description, inputSchema: zodSchema },
            async (args: Record<string, string | undefined>) => {
                const id = args.id!; const value = args.value!;
                const filename = mapping[id]?.[value];
                if (!filename) {
                    const errorText = mapping[id]
                        ? `❌ Not found: id="${id}", value="${value}"\n\nAvailable:\n${categoryInfo[id].map(i => `  - ${i.value}`).join('\n')}`
                        : `❌ Unknown category "${id}"\n\nAvailable: ${Object.keys(mapping).join(', ')}`;
                    return { content: [{ type: 'text' as const, text: errorText }], isError: true };
                }
                try {
                    const content = await readFile(join(process.cwd(), 'public', 'mcp', 'tools', dynamicTool.name, filename), 'utf-8');
                    return { content: [{ type: 'text' as const, text: content }] };
                } catch (e) {
                    return { content: [{ type: 'text' as const, text: `❌ ${e}` }], isError: true };
                }
            }
        );

        // -list companion
        server.registerTool(
            `${dynamicTool.name}-list`,
            { title: `List ${dynamicTool.title}`, description: `List all categories/topics for ${dynamicTool.name}`, inputSchema: { id: z.string().optional().describe('Optional: filter by category') } },
            async (args: { id?: string }) => {
                if (args.id) {
                    const topics = categoryInfo[args.id];
                    if (!topics) return { content: [{ type: 'text' as const, text: `❌ Unknown category "${args.id}"\n\nAvailable: ${Object.keys(categoryInfo).join(', ')}` }], isError: true };
                    return { content: [{ type: 'text' as const, text: `Topics for "${args.id}":\n${topics.map(t => `  - ${t.value}${t.title ? ` (${t.title})` : ''}`).join('\n')}` }] };
                }
                const text = Object.entries(categoryInfo).map(([cat, topics]) =>
                    `**${cat}**\n${topics.map(t => `  - ${t.value}${t.title ? ` (${t.title})` : ''}`).join('\n')}`
                ).join('\n\n');
                return { content: [{ type: 'text' as const, text }] };
            }
        );

        // -search companion
        server.registerTool(
            `${dynamicTool.name}-search`,
            { title: `Search ${dynamicTool.title}`, description: 'Search docs by keyword', inputSchema: { query: z.string().describe('Keywords to search') } },
            async (args: { query: string }) => {
                const keywords = args.query.toLowerCase().split(/\s+/).filter(k => k.length > 2);
                const matches: Array<{ id: string; value: string; title?: string; score: number }> = [];
                for (const [id, topics] of Object.entries(mapping)) {
                    for (const value of Object.keys(topics)) {
                        const info = categoryInfo[id].find(i => i.value === value);
                        const text = `${id} ${value} ${info?.title || ''}`.toLowerCase();
                        const score = keywords.reduce((s, k) => s + (text.includes(k) ? k.length : 0), 0);
                        if (score > 0) matches.push({ id, value, title: info?.title, score });
                    }
                }
                matches.sort((a, b) => b.score - a.score);
                if (matches.length === 0) return { content: [{ type: 'text' as const, text: `❌ No matches for "${args.query}"` }] };
                const text = matches.slice(0, 10).map((m, i) =>
                    `${i + 1}. **${m.id}** → \`${m.value}\`${m.title ? ` (${m.title})` : ''}\n   Usage: ${dynamicTool.name}(id="${m.id}", value="${m.value}")`
                ).join('\n\n');
                return { content: [{ type: 'text' as const, text: `🔍 ${matches.length} matches:\n\n${text}` }] };
            }
        );

        console.log(`  ✅ Registered dynamic tool: ${dynamicTool.name} (${dynamicTool.documents.length} docs)`);
    }

    // ── Static tools from manifest ────────────────────────────────────────────
    // Skip tools already registered as dynamic tools (they share the same names)
    const dynamicToolNames = new Set<string>(
        (manifest.dynamicTools || []).flatMap((dt: { name: string }) => [dt.name, `${dt.name}-list`, `${dt.name}-search`])
    );
    for (const tool of (manifest.tools || []).filter((t: { name: string }) => !dynamicToolNames.has(t.name))) {
        const toolPath = join(process.cwd(), 'public', 'mcp', 'tools', `${tool.name}.md`);

        server.registerTool(
            tool.name,
            {
                title: tool.metadata?.title || tool.name,
                description: tool.metadata?.description || `Rystem Framework tool: ${tool.name}`,
                inputSchema: {},
                outputSchema: { content: z.string(), markdown: z.string() }
            },
            async () => {
                try {
                    const content = await readFile(toolPath, 'utf-8');
                    const output = {
                        content: content,
                        markdown: content
                    };
                    return {
                        content: [
                            {
                                type: 'text',
                                text: content
                            }
                        ],
                        structuredContent: output
                    };
                } catch (error) {
                    const errorMsg = `Error loading tool ${tool.name}: ${error}`;
                    return {
                        content: [{ type: 'text', text: errorMsg }],
                        isError: true
                    };
                }
            }
        );
        console.log(`  ✅ Registered tool: ${tool.name}`);
    }

    // Register all resources from manifest
    for (const resource of manifest.resources || []) {
        const resourcePath = join(process.cwd(), 'public', 'mcp', 'resources', `${resource.name}.md`);

        server.registerResource(
            resource.name,
            resource.uri,
            {
                title: resource.metadata?.title || resource.name,
                description: resource.metadata?.description || `Rystem Framework resource: ${resource.name}`,
                mimeType: 'text/markdown'
            },
            async (uri: { href: string }) => {
                try {
                    const content = await readFile(resourcePath, 'utf-8');
                    return {
                        contents: [
                            {
                                uri: uri.href,
                                text: content,
                                mimeType: 'text/markdown'
                            }
                        ]
                    };
                } catch (error) {
                    throw new Error(`Error loading resource ${resource.name}: ${error}`);
                }
            }
        );
        console.log(`  ✅ Registered resource: ${resource.name}`);
    }

    // Register all prompts from manifest
    for (const prompt of manifest.prompts || []) {
        const promptPath = join(process.cwd(), 'public', 'mcp', 'prompts', `${prompt.name}.md`);

        server.registerPrompt(
            prompt.name,
            {
                title: prompt.metadata?.title || prompt.name,
                description: prompt.metadata?.description || `Rystem Framework prompt: ${prompt.name}`,
                argsSchema:
                    prompt.arguments?.reduce(
                        (schema: Record<string, z.ZodString | z.ZodOptional<z.ZodString>>, arg: { name: string; required: boolean }) => {
                            schema[arg.name] = arg.required ? z.string() : z.string().optional();
                            return schema;
                        },
                        {} as Record<string, z.ZodString | z.ZodOptional<z.ZodString>>
                    ) || {}
            },
            // @ts-ignore - Type mismatch with SDK but functionally correct
            async (args: Record<string, unknown>) => {
                try {
                    const content = await readFile(promptPath, 'utf-8');
                    // Replace argument placeholders in the prompt
                    let processedContent = content;
                    for (const [key, value] of Object.entries(args)) {
                        processedContent = processedContent.replace(new RegExp(`{{${key}}}`, 'g'), String(value));
                    }

                    return {
                        messages: [
                            {
                                role: 'user',
                                content: {
                                    type: 'text',
                                    text: processedContent
                                }
                            }
                        ]
                    };
                } catch (error) {
                    throw new Error(`Error loading prompt ${prompt.name}: ${error}`);
                }
            }
        );
        console.log(`  ✅ Registered prompt: ${prompt.name}`);
    }

    mcpServer = server;
    console.log('✨ MCP Server initialized successfully!\n');
    return server;
}

// MCP endpoint (POST only)
app.post('/mcp', async (req, res) => {
    console.log(`📥 Received MCP request: ${req.body.method || 'unknown'}`);

    try {
        // Initialize server (cached after first request)
        const server = await initializeMcpServer();

        // Create a new transport for each request (prevents request ID collisions)
        const transport = new StreamableHTTPServerTransport({
            sessionIdGenerator: undefined, // Stateless mode
            enableJsonResponse: true
        });

        // Clean up transport when response closes
        res.on('close', () => {
            transport.close();
        });

        // Connect server to transport
        await server.connect(transport);

        // Handle the MCP request
        await transport.handleRequest(req as any, res as any, req.body);

        console.log(`✅ Request handled successfully\n`);
    } catch (error) {
        console.error('❌ Error handling MCP request:', error);

        if (!res.headersSent) {
            res.status(500).json({
                jsonrpc: '2.0',
                error: {
                    code: -32603,
                    message: 'Internal server error',
                    data: error instanceof Error ? error.message : String(error)
                },
                id: null
            });
        }
    }
});

// Health check endpoint
app.get('/health', (req, res) => {
    res.json({
        status: 'ok',
        mcp: mcpServer ? 'initialized' : 'not-initialized',
        timestamp: new Date().toISOString()
    });
});

// Landing page redirect
app.get('/mcp', (req, res) => {
    res.redirect('/mcp-server.html');
});

// Start server
app.listen(PORT, () => {
    console.log(`
╔════════════════════════════════════════════════════════╗
║                                                        ║
║   🚀 Rystem MCP Server (Local Development)           ║
║                                                        ║
║   📍 Server:     http://localhost:${PORT}                 ║
║   📍 MCP API:    http://localhost:${PORT}/mcp             ║
║   📍 Health:     http://localhost:${PORT}/health          ║
║   📍 Docs:       http://localhost:${PORT}/                ║
║                                                        ║
║   🧪 Test with MCP Inspector:                         ║
║      npx @modelcontextprotocol/inspector \\            ║
║          http://localhost:${PORT}/mcp                     ║
║                                                        ║
╚════════════════════════════════════════════════════════╝
    `);
}).on('error', (error: NodeJS.ErrnoException) => {
    if (error.code === 'EADDRINUSE') {
        console.error(`❌ Port ${PORT} is already in use. Please stop the other server first.`);
    } else {
        console.error('❌ Server error:', error);
    }
    process.exit(1);
});

// Graceful shutdown
process.on('SIGTERM', () => {
    console.log('⏹️  Shutting down gracefully...');
    process.exit(0);
});
