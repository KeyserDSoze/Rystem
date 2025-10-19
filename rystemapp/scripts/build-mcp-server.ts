import { readFileSync, writeFileSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const MANIFEST_PATH = join(__dirname, '..', 'public', 'mcp-manifest.json');
const OUTPUT_DIR = join(__dirname, '..', 'public');

interface McpItem {
  name: string;
  path: string;
  title?: string;
  description?: string;
}

interface McpManifest {
  name: string;
  version: string;
  description: string;
  tools: McpItem[];
  resources: McpItem[];
  prompts: McpItem[];
}

interface McpServerResponse {
  jsonrpc: string;
  result: {
    protocolVersion: string;
    capabilities: {
      tools?: Record<string, unknown>;
      resources?: Record<string, unknown>;
      prompts?: Record<string, unknown>;
    };
    serverInfo: {
      name: string;
      version: string;
    };
  };
  id: number;
}

function main() {
  console.log('🔧 Building static MCP server responses...\n');

  // Read existing manifest
  const manifest: McpManifest = JSON.parse(readFileSync(MANIFEST_PATH, 'utf-8'));

  // 1. Generate Initialize response (mcp-server.json)
  const initializeResponse: McpServerResponse = {
    jsonrpc: '2.0',
    result: {
      protocolVersion: '2024-11-05',
      capabilities: {
        tools: manifest.tools.length > 0 ? {} : undefined,
        resources: manifest.resources.length > 0 ? {} : undefined,
        prompts: manifest.prompts.length > 0 ? {} : undefined,
      },
      serverInfo: {
        name: manifest.name,
        version: manifest.version,
      },
    },
    id: 1,
  };

  writeFileSync(
    join(OUTPUT_DIR, 'mcp-server.json'),
    JSON.stringify(initializeResponse, null, 2),
    'utf-8'
  );
  console.log('✓ Generated mcp-server.json (Initialize response)');

  // 2. Generate Tools List response (mcp-tools-list.json)
  const toolsListResponse = {
    jsonrpc: '2.0',
    result: {
      tools: manifest.tools.map(tool => ({
        name: tool.name,
        description: tool.description || tool.title || '',
        inputSchema: {
          type: 'object',
          properties: {},
          required: [],
        },
      })),
    },
    id: 1,
  };

  writeFileSync(
    join(OUTPUT_DIR, 'mcp-tools-list.json'),
    JSON.stringify(toolsListResponse, null, 2),
    'utf-8'
  );
  console.log('✓ Generated mcp-tools-list.json (Tools list)');

  // 3. Generate Resources List response (mcp-resources-list.json)
  const resourcesListResponse = {
    jsonrpc: '2.0',
    result: {
      resources: manifest.resources.map(resource => ({
        uri: `https://rystem.net${resource.path}`,
        name: resource.name,
        description: resource.description || resource.title || '',
        mimeType: 'text/markdown',
      })),
    },
    id: 1,
  };

  writeFileSync(
    join(OUTPUT_DIR, 'mcp-resources-list.json'),
    JSON.stringify(resourcesListResponse, null, 2),
    'utf-8'
  );
  console.log('✓ Generated mcp-resources-list.json (Resources list)');

  // 4. Generate Prompts List response (mcp-prompts-list.json)
  const promptsListResponse = {
    jsonrpc: '2.0',
    result: {
      prompts: manifest.prompts.map(prompt => ({
        name: prompt.name,
        description: prompt.description || prompt.title || '',
        arguments: [],
      })),
    },
    id: 1,
  };

  writeFileSync(
    join(OUTPUT_DIR, 'mcp-prompts-list.json'),
    JSON.stringify(promptsListResponse, null, 2),
    'utf-8'
  );
  console.log('✓ Generated mcp-prompts-list.json (Prompts list)');

  // 5. Generate README for MCP integration
  const readmeContent = `# Rystem MCP Server

Model Context Protocol (MCP) integration for Rystem Framework documentation and tools.

## 🌐 Domain Architecture

Rystem uses two domains for different purposes:

- **📖 Documentation Site**: \`https://rystem.net\` (GitHub Pages)
  - Static documentation, guides, and API references
  - Static MCP JSON files (legacy support)
  
- **⚡ MCP Server**: \`https://rystem.cloud/mcp\` (Vercel)
  - Live Model Context Protocol server
  - Dynamic tools, resources, and prompts
  - JSON-RPC 2.0 over HTTP

## � Quick Start - MCP Server

**Use this endpoint for all AI tools:**

\`\`\`
https://rystem.cloud/mcp
\`\`\`

### Connect to Your AI Tool

**Claude Desktop** - Add to \`claude_desktop_config.json\`:
\`\`\`json
{
  "mcpServers": {
    "rystem": {
      "url": "https://rystem.cloud/mcp",
      "transport": {
        "type": "streamable-http"
      }
    }
  }
}
\`\`\`

**Cursor** - Click this deeplink:
\`\`\`
cursor://anysphere.cursor-deeplink/mcp/install?name=rystem&config=eyJ1cmwiOiJodHRwczovL3J5c3RlbS5jbG91ZC9tY3AifQ==
\`\`\`

**VS Code** - Run this command:
\`\`\`bash
code --add-mcp '{"name":"rystem","type":"http","url":"https://rystem.cloud/mcp"}'
\`\`\`

**GitHub Copilot** - The MCP server provides context automatically when configured

**MCP Inspector** - Test the server interactively:
\`\`\`bash
npx @modelcontextprotocol/inspector https://rystem.cloud/mcp
\`\`\`

## 🔧 Technical Details

- **Protocol**: JSON-RPC 2.0
- **Transport**: Streamable HTTP
- **Version**: ${manifest.version} (MCP Protocol 2024-11-05)
- **Runtime**: Node.js 20.x on Vercel Edge Functions
- **Region**: iad1 (US East - Washington D.C.)

## � Available Content

### Tools (${manifest.tools.length})
${manifest.tools.map(t => `- **${t.title || t.name}**: ${t.description || 'Description'}`).join('\n')}

### Resources (${manifest.resources.length})
${manifest.resources.map(r => `- **${r.title || r.name}**: ${r.description || 'Overview'}`).join('\n')}

### Prompts (${manifest.prompts.length})
${manifest.prompts.map(p => `- **${p.title || p.name}**: ${p.description || 'Context'}`).join('\n')}

## 📁 Static JSON Files (Legacy)

For clients that don't support full MCP protocol, static JSON files are available at \`https://rystem.net/\`:

- **Server Info**: \`mcp-server.json\`
- **Tools List**: \`mcp-tools-list.json\`
- **Resources List**: \`mcp-resources-list.json\`
- **Prompts List**: \`mcp-prompts-list.json\`

### Content Files

Individual content files:
- Tools: \`https://rystem.net/mcp/tools/{name}.md\`
- Resources: \`https://rystem.net/mcp/resources/{name}.md\`
- Prompts: \`https://rystem.net/mcp/prompts/{name}.md\`

## 🧪 Testing

### Test with cURL

\`\`\`bash
# Initialize connection
curl -X POST https://rystem.cloud/mcp \\
  -H "Content-Type: application/json" \\
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "initialize",
    "params": {
      "protocolVersion": "2024-11-05",
      "capabilities": {},
      "clientInfo": {
        "name": "test-client",
        "version": "1.0.0"
      }
    }
  }'

# List available tools
curl -X POST https://rystem.cloud/mcp \\
  -H "Content-Type: application/json" \\
  -d '{
    "jsonrpc": "2.0",
    "id": 2,
    "method": "tools/list",
    "params": {}
  }'
\`\`\`

### Local Development

Run the MCP server locally:

\`\`\`bash
cd rystemapp
npm run dev:api
\`\`\`

Server will start on \`http://localhost:3000/mcp\`

## 🔄 Updates

This documentation is automatically generated during build:

\`\`\`bash
npm run build-mcp
\`\`\`

The MCP server content is dynamically loaded from markdown files in \`/mcp/{tools,resources,prompts}/\`.

---

**Last Updated**: ${new Date().toISOString().split('T')[0]}  
**MCP Protocol Version**: 2024-11-05  
**Documentation**: https://rystem.net  
**MCP Server**: https://rystem.cloud/mcp
`;

  writeFileSync(
    join(OUTPUT_DIR, 'MCP-SERVER.md'),
    readmeContent,
    'utf-8'
  );
  console.log('✓ Generated MCP-SERVER.md (Documentation)');

  console.log('\n✅ Static MCP server responses generated successfully!\n');
  console.log('📊 Summary:');
  console.log(`   Server: ${manifest.name} v${manifest.version}`);
  console.log(`   Tools: ${manifest.tools.length}`);
  console.log(`   Resources: ${manifest.resources.length}`);
  console.log(`   Prompts: ${manifest.prompts.length}`);
  console.log(`   Total: ${manifest.tools.length + manifest.resources.length + manifest.prompts.length}\n`);
  console.log('📄 Generated files:');
  console.log('   - mcp-server.json');
  console.log('   - mcp-tools-list.json');
  console.log('   - mcp-resources-list.json');
  console.log('   - mcp-prompts-list.json');
  console.log('   - MCP-SERVER.md\n');
}

main();
