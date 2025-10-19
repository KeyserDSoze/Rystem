import type { VercelRequest, VercelResponse } from '@vercel/node';
import { McpServer, ResourceTemplate } from '@modelcontextprotocol/sdk/server/mcp.js';
import { StreamableHTTPServerTransport } from '@modelcontextprotocol/sdk/server/streamableHttp.js';
import { readFile } from 'fs/promises';
import { join } from 'path';
import { z } from 'zod';

// Global MCP server instance (reused across requests)
let mcpServer: McpServer | null = null;

// Initialize MCP server with all tools, resources, and prompts
async function initializeMcpServer(): Promise<McpServer> {
    if (mcpServer) {
        return mcpServer;
    }

    const server = new McpServer({
        name: 'rystem-mcp-server',
        version: '1.0.0'
    });

    // Load manifest to get all available items
    const manifestPath = join(process.cwd(), 'public', 'mcp-manifest.json');
    const manifestContent = await readFile(manifestPath, 'utf-8');
    const manifest = JSON.parse(manifestContent);

    // Register all tools from manifest
    for (const tool of manifest.tools || []) {
        const toolPath = join(process.cwd(), 'public', 'mcp', 'tools', `${tool.name}.md`);
        
        server.registerTool(
            tool.name,
            {
                title: tool.metadata?.title || tool.name,
                description: tool.metadata?.description || `Rystem Framework tool: ${tool.name}`,
                // Tools return markdown content as text
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
            async uri => {
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
    }

    // Register all prompts from manifest
    for (const prompt of manifest.prompts || []) {
        const promptPath = join(process.cwd(), 'public', 'mcp', 'prompts', `${prompt.name}.md`);
        
        server.registerPrompt(
            prompt.name,
            {
                title: prompt.metadata?.title || prompt.name,
                description: prompt.metadata?.description || `Rystem Framework prompt: ${prompt.name}`,
                argsSchema: prompt.arguments?.reduce(
                    (schema, arg) => {
                        schema[arg.name] = arg.required ? z.string() : z.string().optional();
                        return schema;
                    },
                    {} as Record<string, z.ZodString | z.ZodOptional<z.ZodString>>
                ) || {}
            },
            async args => {
                try {
                    const content = await readFile(promptPath, 'utf-8');
                    // Replace argument placeholders in the prompt
                    let processedContent = content;
                    for (const [key, value] of Object.entries(args)) {
                        processedContent = processedContent.replace(
                            new RegExp(`{{${key}}}`, 'g'),
                            String(value)
                        );
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
    }

    mcpServer = server;
    return server;
}

// Vercel serverless function handler
export default async function handler(req: VercelRequest, res: VercelResponse) {
    // Only accept POST requests for MCP protocol
    if (req.method !== 'POST') {
        res.status(405).json({
            jsonrpc: '2.0',
            error: {
                code: -32601,
                message: 'Method not allowed. Use POST for MCP requests.'
            },
            id: null
        });
        return;
    }

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
        await transport.handleRequest(
            req as any, // Vercel types are compatible
            res as any,
            req.body
        );
    } catch (error) {
        console.error('Error handling MCP request:', error);
        
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
}
