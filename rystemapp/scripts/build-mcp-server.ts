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
  const readmeContent = `# Rystem MCP Server (Static)

This directory contains static JSON responses for Model Context Protocol (MCP) integration.

## 🌐 Endpoints

All endpoints are available at: \`https://rystem.net/\`

- **Initialize**: \`mcp-server.json\` - Server capabilities and info
- **Tools**: \`mcp-tools-list.json\` - List of available tools
- **Resources**: \`mcp-resources-list.json\` - List of available resources
- **Prompts**: \`mcp-prompts-list.json\` - List of available prompts

## 📚 Content

Tool/Resource/Prompt content is available at:
- Tools: \`/mcp/tools/{name}.md\`
- Resources: \`/mcp/resources/{name}.md\`
- Prompts: \`/mcp/prompts/{name}.md\`

## 🔧 GitHub Copilot Configuration

Add to your \`.github/copilot-instructions.md\` or VS Code settings:

\`\`\`json
{
  "github.copilot.chat.codeGeneration.instructions": [
    {
      "text": "Use Rystem Framework patterns and best practices from https://rystem.net/mcp-manifest.json"
    }
  ]
}
\`\`\`

## 📖 Usage in AI Tools

### Claude Desktop

Add to \`~/Library/Application Support/Claude/claude_desktop_config.json\`:

\`\`\`json
{
  "mcpServers": {
    "rystem": {
      "url": "https://rystem.net/mcp-server.json",
      "type": "static"
    }
  }
}
\`\`\`

### VS Code Copilot

Reference in your codebase:
\`\`\`typescript
// @mcp-server https://rystem.net/mcp-manifest.json
\`\`\`

## 📊 Current Status

- **Version**: ${manifest.version}
- **Tools**: ${manifest.tools.length}
- **Resources**: ${manifest.resources.length}
- **Prompts**: ${manifest.prompts.length}
- **Total Items**: ${manifest.tools.length + manifest.resources.length + manifest.prompts.length}

## 🔄 Updates

This file is automatically generated during build. To update:

\`\`\`bash
npm run build-mcp
\`\`\`

## 📝 Available Tools

${manifest.tools.map(t => `- **${t.title || t.name}**: ${t.description || 'No description'}`).join('\n')}

## 📚 Available Resources

${manifest.resources.map(r => `- **${r.title || r.name}**: ${r.description || 'No description'}`).join('\n')}

## 💬 Available Prompts

${manifest.prompts.map(p => `- **${p.title || p.name}**: ${p.description || 'No description'}`).join('\n')}

---

Generated: ${new Date().toISOString()}
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
