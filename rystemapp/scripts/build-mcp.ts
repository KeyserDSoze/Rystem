import { readdirSync, readFileSync, writeFileSync, existsSync, statSync, mkdirSync, copyFileSync } from 'fs';
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
const OUTPUT_DIR = join(__dirname, '..', 'public', 'mcp');
const OUTPUT_FILE = join(__dirname, '..', 'public', 'mcp-manifest.json');

interface Metadata {
  title?: string;
  description?: string;
  content: string; // Content without frontmatter
}

function extractMetadata(content: string): Metadata {
  // Check for YAML frontmatter
  const frontmatterRegex = /^---\s*\n([\s\S]*?)\n---\s*\n([\s\S]*)$/;
  const match = content.match(frontmatterRegex);

  if (match) {
    // Parse YAML frontmatter
    const frontmatter = match[1];
    const contentWithoutFrontmatter = match[2];

    const titleMatch = frontmatter.match(/title:\s*["']?(.+?)["']?\s*$/m);
    const descMatch = frontmatter.match(/description:\s*["']?(.+?)["']?\s*$/m);

    return {
      title: titleMatch ? titleMatch[1] : undefined,
      description: descMatch ? descMatch[1] : undefined,
      content: contentWithoutFrontmatter,
    };
  }

  // Fallback to H1 title if no frontmatter
  const titleMatch = content.match(/^#\s+(.+)$/m);
  const descMatch = content.match(/^##\s+(.+)$/m) || content.match(/^>\s+(.+)$/m);

  return {
    title: titleMatch ? titleMatch[1] : undefined,
    description: descMatch ? descMatch[1] : undefined,
    content: content,
  };
}

function scanMcpDirectory(dir: string, type: 'tools' | 'resources' | 'prompts'): McpItem[] {
  const items: McpItem[] = [];
  const fullPath = join(dir, type);

  if (!existsSync(fullPath)) {
    console.warn(`⚠️  MCP ${type} directory not found: ${fullPath}`);
    return items;
  }

  // Create output directory for this type
  const outputPath = join(OUTPUT_DIR, type);
  if (!existsSync(outputPath)) {
    mkdirSync(outputPath, { recursive: true });
  }

  const files = readdirSync(fullPath);

  for (const file of files) {
    const filePath = join(fullPath, file);
    const stat = statSync(filePath);

    if (stat.isFile() && file.endsWith('.md')) {
      const content = readFileSync(filePath, 'utf-8');
      const metadata = extractMetadata(content);
      const { name } = parse(file);

      // Write the file WITHOUT frontmatter to public/mcp/
      const destPath = join(outputPath, file);
      writeFileSync(destPath, metadata.content, 'utf-8');

      items.push({
        name,
        path: `/mcp/${type}/${file}`,
        title: metadata.title || name,
        description: metadata.description,
      });

      console.log(`✓ Copied ${type}: ${name} (title: ${metadata.title || name})`);
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

  // Ensure output directories exist
  if (!existsSync(OUTPUT_DIR)) {
    mkdirSync(OUTPUT_DIR, { recursive: true });
  }
  const publicDir = dirname(OUTPUT_FILE);
  if (!existsSync(publicDir)) {
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
