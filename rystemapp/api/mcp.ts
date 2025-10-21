import type { VercelRequest, VercelResponse } from '@vercel/node';
import { McpServer, ResourceTemplate } from '@modelcontextprotocol/sdk/server/mcp.js';
import { StreamableHTTPServerTransport } from '@modelcontextprotocol/sdk/server/streamableHttp.js';
import { readFile } from 'fs/promises';
import { join } from 'path';
import { z } from 'zod';

// Global MCP server instance (reused across requests)
let mcpServer: McpServer | null = null;

interface DynamicToolDocument {
    filename: string;
    id: string;
    value: string;
    metadata?: {
        title?: string;
        description?: string;
    };
}

interface DynamicTool {
    name: string;
    title: string;
    description: string;
    inputSchema: Record<string, {
        type: string;
        description: string;
        required: boolean;
    }>;
    documents: DynamicToolDocument[];
}

interface McpManifest {
    tools: Array<{ name: string; path: string; title?: string; description?: string }>;
    resources: Array<{ name: string; path: string; title?: string; description?: string }>;
    prompts: Array<{ name: string; path: string; title?: string; description?: string; arguments?: any[] }>;
    dynamicTools: DynamicTool[];
}

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
    const manifest: McpManifest = JSON.parse(manifestContent);

    // 🆕 Register dynamic tools
    for (const dynamicTool of manifest.dynamicTools || []) {
        // Build Zod schema from inputSchema
        const zodSchema: Record<string, z.ZodString | z.ZodOptional<z.ZodString>> = {};
        for (const [key, config] of Object.entries(dynamicTool.inputSchema)) {
            zodSchema[key] = config.required 
                ? z.string().describe(config.description)
                : z.string().optional().describe(config.description);
        }

        // Build mapping: { id: { value: filename } }
        const mapping: Record<string, Record<string, string>> = {};
        const categoryInfo: Record<string, Array<{ value: string; title?: string }>> = {};
        
        for (const doc of dynamicTool.documents) {
            if (!mapping[doc.id]) {
                mapping[doc.id] = {};
                categoryInfo[doc.id] = [];
            }
            mapping[doc.id][doc.value] = doc.filename;
            categoryInfo[doc.id].push({
                value: doc.value,
                title: doc.metadata?.title
            });
        }

        // Helper: Progressive keyword disambiguation
        const findBestMatch = (query: string): { id: string; value: string; title?: string } | null => {
            const keywords = query.toLowerCase().split(/\s+/).filter(k => k.length > 2);
            if (keywords.length === 0) return null;

            // Try progressive disambiguation
            let candidates: Array<{ id: string; value: string; title?: string; score: number }> = [];

            for (const [id, topics] of Object.entries(mapping)) {
                for (const value of Object.keys(topics)) {
                    const info = categoryInfo[id].find(i => i.value === value);
                    const searchText = `${id} ${value} ${info?.title || ''}`.toLowerCase();
                    
                    let score = 0;
                    let matchedKeywords = 0;
                    
                    for (const keyword of keywords) {
                        if (searchText.includes(keyword)) {
                            matchedKeywords++;
                            score += keyword.length; // Longer keywords = higher relevance
                        }
                    }
                    
                    if (matchedKeywords > 0) {
                        candidates.push({ id, value, title: info?.title, score: matchedKeywords * 1000 + score });
                    }
                }
            }

            if (candidates.length === 0) return null;
            
            // Sort by score (highest first)
            candidates.sort((a, b) => b.score - a.score);
            
            // If top result significantly better, return it
            if (candidates.length === 1 || candidates[0].score > candidates[1].score * 1.5) {
                return candidates[0];
            }
            
            return null; // Ambiguous
        };

        // Register main tool
        server.registerTool(
            dynamicTool.name,
            {
                title: dynamicTool.title,
                description: dynamicTool.description,
                inputSchema: zodSchema
            },
            async (args: { [x: string]: string | undefined }) => {
                const id = args.id!;
                const value = args.value!;

                const filename = mapping[id]?.[value];

                if (!filename) {
                    // Try smart fuzzy search
                    const bestMatch = findBestMatch(`${id} ${value}`);
                    
                    if (bestMatch) {
                        // Found a good match! Use it
                        const matchFilename = mapping[bestMatch.id][bestMatch.value];
                        const docPath = join(process.cwd(), 'public', 'mcp', 'tools', dynamicTool.name, matchFilename);
                        
                        try {
                            const content = await readFile(docPath, 'utf-8');
                            const suggestion = `\n\n---\n💡 **Auto-corrected**: You searched for id="${id}", value="${value}"\n✅ **Showing**: id="${bestMatch.id}", value="${bestMatch.value}"${bestMatch.title ? ` (${bestMatch.title})` : ''}`;
                            
                            return {
                                content: [{ type: 'text' as const, text: content + suggestion }]
                            };
                        } catch (error) {
                            // Fall through to error handling
                        }
                    }
                    
                    const availableIds = Object.keys(mapping);
                    const availableValues = mapping[id] ? Object.keys(mapping[id]) : [];
                    
                    let errorText = `❌ Documentation not found for id="${id}", value="${value}"\n\n`;
                    
                    if (!mapping[id]) {
                        errorText += `📂 Available categories:\n${availableIds.map(cat => `  - ${cat}`).join('\n')}`;
                    } else {
                        errorText += `📄 Available topics for "${id}":\n`;
                        errorText += categoryInfo[id]
                            .map(item => `  - ${item.value}${item.title ? ` (${item.title})` : ''}`)
                            .join('\n');
                    }
                    
                    errorText += `\n\n💡 Tip: Use "${dynamicTool.name}-list()" to see all available documentation.`;

                    return {
                        content: [{ type: 'text' as const, text: errorText }],
                        isError: true
                    };
                }

                const docPath = join(process.cwd(), 'public', 'mcp', 'tools', dynamicTool.name, filename);

                try {
                    const content = await readFile(docPath, 'utf-8');
                    return {
                        content: [{ type: 'text' as const, text: content }]
                    };
                } catch (error) {
                    return {
                        content: [{
                            type: 'text' as const,
                            text: `❌ Error loading documentation: ${error}`
                        }],
                        isError: true
                    };
                }
            }
        );

        // 🆕 Register companion "list" tool
        server.registerTool(
            `${dynamicTool.name}-list`,
            {
                title: `List ${dynamicTool.title}`,
                description: `Get all available categories and topics for ${dynamicTool.name}`,
                inputSchema: {
                    id: z.string().optional().describe('Optional: filter by category')
                }
            },
            async (args: { id?: string }) => {
                if (args.id) {
                    const topics = mapping[args.id];

                    if (!topics) {
                        const availableIds = Object.keys(mapping);
                        return {
                            content: [{
                                type: 'text' as const,
                                text: `Category "${args.id}" not found.\n\nAvailable categories: ${availableIds.join(', ')}`
                            }]
                        };
                    }

                    const text = `Documentation topics for "${args.id}":\n\n` +
                        categoryInfo[args.id].map(item => 
                            `  - ${item.value}${item.title ? ` (${item.title})` : ''}`
                        ).join('\n') +
                        `\n\nUsage: ${dynamicTool.name}(id="${args.id}", value="{topic}")`;

                    return {
                        content: [{ type: 'text' as const, text }]
                    };
                }

                // List all categories with their topics
                const text = `Available ${dynamicTool.title}:\n\n` +
                    Object.entries(categoryInfo)
                        .map(([category, topics]) =>
                            `**${category}**\n${topics.map(t => `  - ${t.value}${t.title ? ` (${t.title})` : ''}`).join('\n')}`
                        )
                        .join('\n\n') +
                    `\n\nUsage: ${dynamicTool.name}(id="{category}", value="{topic}")`;

                return {
                    content: [{ type: 'text' as const, text }]
                };
            }
        );

        // 🆕 Register companion "search" tool
        server.registerTool(
            `${dynamicTool.name}-search`,
            {
                title: `Search ${dynamicTool.title}`,
                description: `Search documentation by keyword (supports multi-word queries with progressive disambiguation)`,
                inputSchema: {
                    query: z.string().describe('Search query (space-separated keywords)')
                }
            },
            async (args: { query: string }) => {
                const keywords = args.query.toLowerCase().split(/\s+/).filter(k => k.length > 2);
                
                if (keywords.length === 0) {
                    return {
                        content: [{
                            type: 'text' as const,
                            text: `⚠️ Query too short. Please provide meaningful keywords (3+ characters).`
                        }]
                    };
                }

                const matches: Array<{ id: string; value: string; title?: string; score: number; matchedKeywords: string[] }> = [];

                for (const [id, topics] of Object.entries(mapping)) {
                    for (const value of Object.keys(topics)) {
                        const info = categoryInfo[id].find(i => i.value === value);
                        const searchText = `${id} ${value} ${info?.title || ''}`.toLowerCase();
                        
                        let score = 0;
                        let matchedKeywords: string[] = [];
                        
                        for (const keyword of keywords) {
                            if (searchText.includes(keyword)) {
                                matchedKeywords.push(keyword);
                                score += keyword.length * 100; // Weight by keyword length
                            }
                        }
                        
                        if (matchedKeywords.length > 0) {
                            matches.push({ 
                                id, 
                                value, 
                                title: info?.title, 
                                score: matchedKeywords.length * 1000 + score,
                                matchedKeywords 
                            });
                        }
                    }
                }

                if (matches.length === 0) {
                    // No matches - show all available options
                    const allOptions = Object.entries(categoryInfo)
                        .map(([category, topics]) =>
                            `**${category}**\n${topics.map(t => `  - ${t.value}${t.title ? ` (${t.title})` : ''}`).join('\n')}`
                        )
                        .join('\n\n');
                    
                    return {
                        content: [{
                            type: 'text' as const,
                            text: `❌ No matches found for "${args.query}"\n\n` +
                                  `📚 **Available Documentation:**\n\n${allOptions}\n\n` +
                                  `💡 **How to use:**\n` +
                                  `- Use ${dynamicTool.name}(id="{category}", value="{topic}")\n` +
                                  `- Use ${dynamicTool.name}-list() to see all options\n` +
                                  `- Use ${dynamicTool.name}-search(query="keyword") to search\n\n` +
                                  `**Categories:** ${Object.keys(categoryInfo).join(', ')}`
                        }]
                    };
                }

                // Sort by score (highest first)
                matches.sort((a, b) => b.score - a.score);

                // Show top matches
                const topMatches = matches.slice(0, 10);
                const text = `🔍 Found ${matches.length} matches for "${args.query}"` +
                    (matches.length > 10 ? ` (showing top 10)` : '') +
                    `:\n\n` +
                    topMatches.map((m, i) =>
                        `${i + 1}. **${m.id}** → \`${m.value}\`${m.title ? ` (${m.title})` : ''}\n` +
                        `   Matched: ${m.matchedKeywords.join(', ')}\n` +
                        `   Usage: ${dynamicTool.name}(id="${m.id}", value="${m.value}")`
                    ).join('\n\n');

                return {
                    content: [{ type: 'text' as const, text }]
                };
            }
        );
    }

    // Register all tools from manifest
    for (const tool of manifest.tools || []) {
        const toolPath = join(process.cwd(), 'public', 'mcp', 'tools', `${tool.name}.md`);
        
        server.registerTool(
            tool.name,
            {
                title: tool.title || tool.name,
                description: tool.description || `Rystem Framework tool: ${tool.name}`,
                inputSchema: {}
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
            `rystem://resources/${resource.name}`,
            {
                title: resource.title || resource.name,
                description: resource.description || `Rystem Framework resource: ${resource.name}`,
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
        
        const argsSchema: Record<string, z.ZodString | z.ZodOptional<z.ZodString>> = {};
        for (const arg of prompt.arguments || []) {
            argsSchema[arg.name] = arg.required ? z.string() : z.string().optional();
        }
        
        server.registerPrompt(
            prompt.name,
            {
                title: prompt.title || prompt.name,
                description: prompt.description || `Rystem Framework prompt: ${prompt.name}`,
                argsSchema: argsSchema
            },
            async (args: Record<string, unknown>) => {
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
                                role: 'user' as const,
                                content: {
                                    type: 'text' as const,
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
