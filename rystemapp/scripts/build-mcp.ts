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

interface DynamicToolDocument {
  filename: string;
  id: string;
  value: string;
  metadata?: {
    title?: string;
    description?: string;
    [key: string]: any;
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
  name: string;
  version: string;
  description: string;
  tools: McpItem[];
  resources: McpItem[];
  prompts: McpItem[];
  dynamicTools: DynamicTool[];
}

const MCP_DIR = join(__dirname, '..', 'src', 'mcp');
const OUTPUT_DIR = join(__dirname, '..', 'public', 'mcp');
const OUTPUT_FILE = join(__dirname, '..', 'public', 'mcp-manifest.json');
const PACKAGE_JSON_PATH = join(__dirname, '..', 'package.json');

interface Metadata {
  title?: string;
  description?: string;
  content: string;
}

// Parse "rystem-json-extensions.md" → { id: "rystem", value: "json-extensions" }
function parseDocumentFilename(filename: string): { id: string; value: string } | null {
  const baseName = filename.replace(/\.md$/, '');
  const firstHyphenIndex = baseName.indexOf('-');
  
  if (firstHyphenIndex === -1) {
    console.warn(`⚠️  Skipping ${filename}: expected format "id-value.md" with at least one hyphen`);
    return null;
  }
  
  // Split ONLY at first hyphen
  const id = baseName.substring(0, firstHyphenIndex);
  const value = baseName.substring(firstHyphenIndex + 1);
  
  return { id, value };
}

// Extract YAML frontmatter
function extractMetadata(content: string): Metadata {
  const frontmatterRegex = /^---\s*\n([\s\S]*?)\n---\s*\n([\s\S]*)$/;
  const match = content.match(frontmatterRegex);

  if (match) {
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

  return { content };
}

// Generate enhanced tool description from documents
function generateToolDescription(toolName: string, documents: DynamicToolDocument[]): string {
  const categoriesMap = new Map<string, Array<{ value: string; title?: string; description?: string }>>();
  
  for (const doc of documents) {
    if (!categoriesMap.has(doc.id)) {
      categoriesMap.set(doc.id, []);
    }
    
    categoriesMap.get(doc.id)!.push({
      value: doc.value,
      title: doc.metadata?.title,
      description: doc.metadata?.description
    });
  }
  
  const categoriesList = Array.from(categoriesMap.entries())
    .map(([id, values]) => {
      const valuesList = values
        .map(v => {
          const title = v.title || v.value;
          const desc = v.description ? ` - ${v.description}` : '';
          return `  - ${id} + ${v.value}: ${title}${desc}`;
        })
        .join('\n');
      
      return `**${id}**: ${values.map(v => v.value).join(', ')}\n${valuesList}`;
    })
    .join('\n\n');
  
  return `Retrieve specific Rystem Framework documentation by category (id) and topic (value).

Available categories and their topics:

${categoriesList}

Usage example: ${toolName}(id="auth", value="blazor")`;
}

// Scan tool-* directories for dynamic tools
function scanDynamicTools(srcDir: string): DynamicTool[] {
  const dynamicTools: DynamicTool[] = [];
  
  if (!existsSync(srcDir)) {
    return dynamicTools;
  }
  
  const entries = readdirSync(srcDir, { withFileTypes: true });
  
  for (const entry of entries) {
    if (!entry.isDirectory() || !entry.name.startsWith('tool-')) {
      continue;
    }
    
    const toolName = entry.name.replace(/^tool-/, '');
    const toolDir = join(srcDir, entry.name);
    
    // Read tool.description (optional)
    const descriptionPath = join(toolDir, 'tool.description');
    let toolTitle = toolName.replace(/-/g, ' ').replace(/\b\w/g, l => l.toUpperCase());
    
    // Scan markdown files
    const documents: DynamicToolDocument[] = [];
    const files = readdirSync(toolDir);
    
    for (const file of files) {
      if (!file.endsWith('.md')) continue;
      
      const filePath = join(toolDir, file);
      const mapping = parseDocumentFilename(file);
      
      if (!mapping) continue;
      
      const fileContent = readFileSync(filePath, 'utf-8');
      const metadata = extractMetadata(fileContent);
      
      documents.push({
        filename: file,
        id: mapping.id,
        value: mapping.value,
        metadata: {
          title: metadata.title,
          description: metadata.description
        }
      });
    }
    
    if (documents.length === 0) {
      console.warn(`⚠️  No documents found in ${entry.name}, skipping`);
      continue;
    }
    
    // Generate enhanced description
    const enhancedDescription = generateToolDescription(toolName, documents);
    
    // Build input schema
    const categoriesMap = new Map<string, string[]>();
    for (const doc of documents) {
      if (!categoriesMap.has(doc.id)) {
        categoriesMap.set(doc.id, []);
      }
      categoriesMap.get(doc.id)!.push(doc.value);
    }
    
    const availableCategories = Array.from(categoriesMap.keys()).join(', ');
    
    dynamicTools.push({
      name: toolName,
      title: toolTitle,
      description: enhancedDescription,
      inputSchema: {
        id: {
          type: 'string',
          description: `Documentation category. Available: ${availableCategories}`,
          required: true
        },
        value: {
          type: 'string',
          description: `Specific topic within the category. Use "${toolName}-list" to see all available topics per category.`,
          required: true
        }
      },
      documents
    });
    
    console.log(`✓ Found dynamic tool: ${toolName} (${documents.length} documents)`);
  }
  
  return dynamicTools;
}

// Copy dynamic tool files to public/mcp/tools/{toolName}/
function copyDynamicToolFiles(srcDir: string, outputDir: string): void {
  if (!existsSync(srcDir)) {
    return;
  }
  
  const entries = readdirSync(srcDir, { withFileTypes: true });
  
  for (const entry of entries) {
    if (!entry.isDirectory() || !entry.name.startsWith('tool-')) {
      continue;
    }
    
    const toolName = entry.name.replace(/^tool-/, '');
    const srcToolDir = join(srcDir, entry.name);
    const destToolDir = join(outputDir, 'tools', toolName);
    
    // Create destination directory
    if (!existsSync(destToolDir)) {
      mkdirSync(destToolDir, { recursive: true });
    }
    
    // Copy all markdown files and tool.description
    const files = readdirSync(srcToolDir);
    for (const file of files) {
      if (file.endsWith('.md') || file === 'tool.description') {
        const srcPath = join(srcToolDir, file);
        const destPath = join(destToolDir, file);
        copyFileSync(srcPath, destPath);
      }
    }
  }
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

      // Write the file WITHOUT frontmatter to public/mcp/ with UTF-8 BOM
      const destPath = join(outputPath, file);
      const utf8BOM = '\uFEFF';
      writeFileSync(destPath, utf8BOM + metadata.content, 'utf-8');

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

  // Read version from package.json
  let version = '1.0.0'; // fallback version
  try {
    const packageJson = JSON.parse(readFileSync(PACKAGE_JSON_PATH, 'utf-8'));
    version = packageJson.version || version;
    console.log(`📦 Using version from package.json: ${version}\n`);
  } catch (error) {
    console.warn('⚠️  Could not read package.json, using default version:', version);
  }

  // Scan dynamic tools (tool-* folders)
  const dynamicTools = scanDynamicTools(MCP_DIR);
  
  // Copy dynamic tool files to public
  copyDynamicToolFiles(MCP_DIR, OUTPUT_DIR);
  
  // Scan regular items
  const tools = scanMcpDirectory(MCP_DIR, 'tools');
  const resources = scanMcpDirectory(MCP_DIR, 'resources');
  const prompts = scanMcpDirectory(MCP_DIR, 'prompts');

  const manifest: McpManifest = {
    name: 'rystem-mcp',
    version: version,
    description: 'Rystem Framework Model Context Protocol - Tools, Resources, and Prompts for developers',
    tools,
    resources,
    prompts,
    dynamicTools
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
  console.log(`   Dynamic Tools: ${dynamicTools.length}`);
  if (dynamicTools.length > 0) {
    dynamicTools.forEach(tool => {
      console.log(`     - ${tool.name} (${tool.documents.length} documents)`);
    });
  }
  console.log(`   Tools: ${tools.length}`);
  console.log(`   Resources: ${resources.length}`);
  console.log(`   Prompts: ${prompts.length}`);
  console.log(`   Total: ${dynamicTools.length + tools.length + resources.length + prompts.length}\n`);
}

main();
