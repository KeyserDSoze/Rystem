import { readdirSync, readFileSync, writeFileSync, existsSync, statSync } from 'fs';
import { join, relative, dirname, parse } from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

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

const MCP_DIR = join(__dirname, '..', 'src', 'mcp');
const OUTPUT_FILE = join(__dirname, '..', 'public', 'mcp-manifest.json');

function extractMetadata(content: string): { title?: string; description?: string } {
  const titleMatch = content.match(/^#\s+(.+)$/m);
  const descMatch = content.match(/^##\s+(.+)$/m) || content.match(/^>\s+(.+)$/m);

  return {
    title: titleMatch ? titleMatch[1] : undefined,
    description: descMatch ? descMatch[1] : undefined,
  };
}

function scanMcpDirectory(dir: string, type: 'tools' | 'resources' | 'prompts'): McpItem[] {
  const items: McpItem[] = [];
  const fullPath = join(dir, type);

  if (!existsSync(fullPath)) {
    console.warn(`⚠️  MCP ${type} directory not found: ${fullPath}`);
    return items;
  }

  const files = readdirSync(fullPath);

  for (const file of files) {
    const filePath = join(fullPath, file);
    const stat = statSync(filePath);

    if (stat.isFile() && file.endsWith('.md')) {
      const content = readFileSync(filePath, 'utf-8');
      const metadata = extractMetadata(content);
      const { name } = parse(file);

      items.push({
        name,
        path: `/mcp/${type}/${file}`,
        title: metadata.title || name,
        description: metadata.description,
      });

      console.log(`✓ Found ${type}: ${name}`);
    }
  }

  return items;
}

function main() {
  console.log('🔧 Building MCP manifest...\n');

  const manifest: McpManifest = {
    name: 'rystem-mcp',
    version: '1.0.0',
    description: 'Rystem Framework Model Context Protocol - Tools, Resources, and Prompts for developers',
    tools: scanMcpDirectory(MCP_DIR, 'tools'),
    resources: scanMcpDirectory(MCP_DIR, 'resources'),
    prompts: scanMcpDirectory(MCP_DIR, 'prompts'),
  };

  // Ensure public directory exists
  const publicDir = dirname(OUTPUT_FILE);
  if (!existsSync(publicDir)) {
    const { mkdirSync } = require('fs');
    mkdirSync(publicDir, { recursive: true });
  }

  writeFileSync(OUTPUT_FILE, JSON.stringify(manifest, null, 2), 'utf-8');

  console.log(`\n✅ MCP manifest generated successfully`);
  console.log(`📄 Manifest file: ${relative(join(__dirname, '..'), OUTPUT_FILE)}\n`);
  console.log('📊 Summary:');
  console.log(`   Tools: ${manifest.tools.length}`);
  console.log(`   Resources: ${manifest.resources.length}`);
  console.log(`   Prompts: ${manifest.prompts.length}`);
  console.log(`   Total: ${manifest.tools.length + manifest.resources.length + manifest.prompts.length}\n`);
}

main();
