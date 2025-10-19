#!/usr/bin/env tsx
/**
 * Simple test script for MCP server
 * Run with: tsx test-mcp-local.ts
 */

const BASE_URL = 'http://localhost:3000';

interface JsonRpcRequest {
    jsonrpc: '2.0';
    method: string;
    params?: unknown;
    id: number;
}

interface JsonRpcResponse {
    jsonrpc: '2.0';
    result?: unknown;
    error?: {
        code: number;
        message: string;
        data?: unknown;
    };
    id: number;
}

async function sendMcpRequest(method: string, params?: unknown): Promise<JsonRpcResponse> {
    const request: JsonRpcRequest = {
        jsonrpc: '2.0',
        method,
        params,
        id: Date.now()
    };

    console.log(`\n📤 Sending: ${method}`);

    const response = await fetch(`${BASE_URL}/mcp`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(request)
    });

    const data = (await response.json()) as JsonRpcResponse;

    if (data.error) {
        console.log(`❌ Error: ${data.error.message}`);
    } else {
        console.log(`✅ Success`);
    }

    return data;
}

async function runTests() {
    console.log('🧪 Testing Rystem MCP Server\n');
    console.log('═'.repeat(60));

    try {
        // Test 1: Health check
        console.log('\n1️⃣  Health Check');
        const health = await fetch(`${BASE_URL}/health`);
        const healthData = await health.json();
        console.log(`✅ Health: ${healthData.status}, MCP: ${healthData.mcp}`);

        // Test 2: Initialize
        console.log('\n2️⃣  Initialize MCP Connection');
        const initResult = await sendMcpRequest('initialize', {
            protocolVersion: '2024-11-05',
            capabilities: {},
            clientInfo: {
                name: 'test-client',
                version: '1.0.0'
            }
        });

        if (initResult.result) {
            const result = initResult.result as { serverInfo?: { name: string; version: string } };
            console.log(`   Server: ${result.serverInfo?.name} v${result.serverInfo?.version}`);
        }

        // Test 3: List Tools
        console.log('\n3️⃣  List Tools');
        const toolsResult = await sendMcpRequest('tools/list');

        if (toolsResult.result) {
            const result = toolsResult.result as { tools?: Array<{ name: string }> };
            console.log(`   Found ${result.tools?.length || 0} tools:`);
            result.tools?.forEach(tool => {
                console.log(`   - ${tool.name}`);
            });
        }

        // Test 4: List Resources
        console.log('\n4️⃣  List Resources');
        const resourcesResult = await sendMcpRequest('resources/list');

        if (resourcesResult.result) {
            const result = resourcesResult.result as { resources?: Array<{ name: string; uri: string }> };
            console.log(`   Found ${result.resources?.length || 0} resources:`);
            result.resources?.forEach(resource => {
                console.log(`   - ${resource.name} (${resource.uri})`);
            });
        }

        // Test 5: List Prompts
        console.log('\n5️⃣  List Prompts');
        const promptsResult = await sendMcpRequest('prompts/list');

        if (promptsResult.result) {
            const result = promptsResult.result as { prompts?: Array<{ name: string }> };
            console.log(`   Found ${result.prompts?.length || 0} prompts:`);
            result.prompts?.forEach(prompt => {
                console.log(`   - ${prompt.name}`);
            });
        }

        // Test 6: Call a tool
        console.log('\n6️⃣  Call Tool: ddd');
        const toolResult = await sendMcpRequest('tools/call', {
            name: 'ddd',
            arguments: {}
        });

        if (toolResult.result) {
            const result = toolResult.result as { content?: Array<{ type: string; text?: string }> };
            const content = result.content?.[0]?.text;
            if (content) {
                const preview = content.substring(0, 100);
                console.log(`   Content preview: ${preview}...`);
            }
        }

        // Test 7: Read a resource
        console.log('\n7️⃣  Read Resource: background-jobs');
        const resourceResult = await sendMcpRequest('resources/read', {
            uri: 'rystem://resources/background-jobs'
        });

        if (resourceResult.result) {
            const result = resourceResult.result as { contents?: Array<{ text?: string }> };
            const text = result.contents?.[0]?.text;
            if (text) {
                const preview = text.substring(0, 100);
                console.log(`   Content preview: ${preview}...`);
            }
        }

        console.log('\n' + '═'.repeat(60));
        console.log('\n✅ All tests completed successfully!\n');
    } catch (error) {
        console.error('\n❌ Test failed:', error);
        process.exit(1);
    }
}

// Run tests
runTests();
