import { readdirSync, readFileSync, writeFileSync, existsSync, statSync, mkdirSync, copyFileSync, Dirent } from 'fs';
import { join, relative, dirname, parse, sep } from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

interface McpItem {
  name: string;
  path: string;
  title?: string;
  description?: string;
  inputSchema?: Record<string, { type: string; description: string; required: boolean }>;
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
const SRC_ROOT = join(__dirname, '..', '..', 'src');

// Directories to skip during auto-discovery
const SKIP_DIRS = new Set(['node_modules', 'bin', 'obj', '.git', 'dist', 'TestResults', '.vs', 'packages', 'out']);

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

// Auto-discover README.md files from projects that have a .csproj or package.json
// in the same folder. id = first path segment under src/, value = derived from project name.
function autoDiscoverProjectReadmes(
  srcRoot: string,
  existingDocs: Array<{ id: string; value: string }>
): Array<{ id: string; value: string; filename: string; filePath: string; metadata: { title?: string; description?: string } }> {
  const results: Array<{ id: string; value: string; filename: string; filePath: string; metadata: { title?: string; description?: string } }> = [];

  function scanDir(dir: string) {
    let entries: Dirent<string>[];
    try {
      entries = readdirSync(dir, { withFileTypes: true });
    } catch {
      return;
    }

    const fileNames = entries.filter(e => e.isFile()).map(e => e.name);
    const hasReadme = fileNames.some(f => f.toLowerCase() === 'readme.md');
    const csprojFile = fileNames.find(f => f.endsWith('.csproj'));
    const hasPackageJson = fileNames.includes('package.json');

    if (hasReadme && (csprojFile || hasPackageJson)) {
      // Derive id = first path segment under srcRoot (e.g. "AI", "Extensions")
      const rel = relative(srcRoot, dir);
      const parts = rel.split(sep);
      const id = parts[0].toLowerCase();

      // Skip inner TS packages: path has a 'src' segment after the first level
      if (parts.slice(1).some(p => p.toLowerCase() === 'src')) {
        // Recurse but don't register
        for (const entry of entries) {
          if (entry.isDirectory() && !SKIP_DIRS.has(entry.name)) {
            scanDir(join(dir, entry.name));
          }
        }
        return;
      }

      // Skip test projects: any path segment is 'test' or 'tests'
      if (parts.some(p => p.toLowerCase() === 'test' || p.toLowerCase() === 'tests')) {
        for (const entry of entries) {
          if (entry.isDirectory() && !SKIP_DIRS.has(entry.name)) {
            scanDir(join(dir, entry.name));
          }
        }
        return;
      }

      // Derive value from .csproj name or package.json name
      let value: string;
      if (csprojFile) {
        value = csprojFile
          .replace('.csproj', '')
          .replace(/^Rystem\./i, '')   // strip leading "Rystem."
          .replace(/\./g, '-')          // dots → dashes
          .toLowerCase();
      } else {
        try {
          const pkg = JSON.parse(readFileSync(join(dir, 'package.json'), 'utf-8'));
          value = (pkg.name as string || parts[parts.length - 1])
            .replace(/^@[^/]+\//, '')
            .replace(/[^a-z0-9]+/g, '-')
            .replace(/^-|-$/g, '')
            .toLowerCase();
        } catch {
          value = parts[parts.length - 1].toLowerCase().replace(/[^a-z0-9]+/g, '-');
        }
      }

      // Skip test/sample apps by project name (testapp, playground, sample, demo)
      const valueLower = value.toLowerCase();
      if (/testapp|playground|sample|\.demo/.test(valueLower)) {
        // Still recurse into subfolders
        for (const entry of entries) {
          if (entry.isDirectory() && !SKIP_DIRS.has(entry.name)) {
            scanDir(join(dir, entry.name));
          }
        }
        return;
      }

      // Skip if already covered by a manual doc (manual takes priority)
      const alreadyExists = existingDocs.some(d => d.id === id && d.value === value);
      if (!alreadyExists) {
        const readmePath = join(dir, fileNames.find(f => f.toLowerCase() === 'readme.md')!);
        const content = readFileSync(readmePath, 'utf-8');
        const meta = extractMetadata(content);

        // Extract a short description from first non-empty paragraph if no frontmatter
        let description = meta.description;
        if (!description) {
          const firstPara = meta.content
            .split('\n')
            .map(l => l.trim())
            .filter(l => l && !l.startsWith('#') && !l.startsWith('!') && !l.startsWith('<') && !l.startsWith('|') && !l.startsWith('```') && l.length > 20)
            .find(l => l.length > 0);
          description = firstPara?.replace(/[*_`[\]]/g, '').substring(0, 160);
        }

        const title = meta.title
          || meta.content.split('\n').find(l => l.startsWith('# '))?.replace(/^#\s+/, '').replace(/[*_`]/g, '').trim()
          || `${id}/${value}`;

        const filename = `${id}-${value}.md`;
        results.push({ id, value, filename, filePath: readmePath, metadata: { title, description } });
        console.log(`  ↳ auto: ${filename} — ${title}`);
      }
    }

    // Recurse into subdirectories, skipping noise folders
    for (const entry of entries) {
      if (entry.isDirectory() && !SKIP_DIRS.has(entry.name)) {
        scanDir(join(dir, entry.name));
      }
    }
  }

  if (existsSync(srcRoot)) {
    scanDir(srcRoot);
  }

  return results;
}

function main() {  console.log('🔧 Building MCP manifest...\n');

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

  // Auto-discover project READMEs from src/ and inject into get-rystem-docs
  console.log('\n🔍 Auto-discovering project READMEs from src/...');
  const getRystemDocsTool = dynamicTools.find(t => t.name === 'get-rystem-docs');
  if (getRystemDocsTool) {
    const existingDocs = getRystemDocsTool.documents.map(d => ({ id: d.id, value: d.value }));
    const autoDiscovered = autoDiscoverProjectReadmes(SRC_ROOT, existingDocs);

    // Copy discovered READMEs to public/mcp/tools/get-rystem-docs/{id}-{value}.md
    const toolOutputDir = join(OUTPUT_DIR, 'tools', 'get-rystem-docs');
    if (!existsSync(toolOutputDir)) {
      mkdirSync(toolOutputDir, { recursive: true });
    }

    for (const doc of autoDiscovered) {
      const destPath = join(toolOutputDir, doc.filename);
      copyFileSync(doc.filePath, destPath);
      getRystemDocsTool.documents.push({
        filename: doc.filename,
        id: doc.id,
        value: doc.value,
        metadata: { ...doc.metadata, autoDiscovered: true }
      });
    }

    // Regenerate description with the newly added docs
    getRystemDocsTool.description = generateToolDescription(getRystemDocsTool.name, getRystemDocsTool.documents);
    console.log(`✅ Auto-discovered ${autoDiscovered.length} project READMEs`);
  } else {
    console.warn('⚠️  No "get-rystem-docs" dynamic tool found; skipping auto-discovery');
  }

  // Scan regular items
  const staticTools = scanMcpDirectory(MCP_DIR, 'tools');
  const resources = scanMcpDirectory(MCP_DIR, 'resources');
  const prompts = scanMcpDirectory(MCP_DIR, 'prompts');

  // Populate tools[] with entries for each registered tool (main + list + search companions)
  // so that the MCP UI badge correctly shows a non-zero count
  const derivedTools: McpItem[] = [];
  for (const dt of dynamicTools) {
    derivedTools.push({ name: dt.name, path: '', title: dt.title, description: dt.description, inputSchema: dt.inputSchema });
    derivedTools.push({ name: `${dt.name}-list`, path: '', title: `List ${dt.title}`, description: `Get all available categories and topics for ${dt.name}`, inputSchema: { id: { type: 'string', description: 'Optional: filter by category', required: false } } });
    derivedTools.push({ name: `${dt.name}-search`, path: '', title: `Search ${dt.title}`, description: `Search documentation by keyword with progressive disambiguation`, inputSchema: { query: { type: 'string', description: 'Search query (space-separated keywords)', required: true } } });
  }
  const tools = [...staticTools, ...derivedTools];

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
      const manualCount = tool.documents.filter(d => !d.metadata?.autoDiscovered).length;
      const autoCount = tool.documents.filter(d => d.metadata?.autoDiscovered).length;
      const note = autoCount > 0 ? ` (${manualCount} manual + ${autoCount} auto)` : '';
      console.log(`     - ${tool.name} (${tool.documents.length} documents${note})`);
    });
  }
  console.log(`   Tools: ${tools.length} (${staticTools.length} static + ${derivedTools.length} derived from dynamic)`);
  console.log(`   Resources: ${resources.length}`);
  console.log(`   Prompts: ${prompts.length}`);
  console.log(`   Total: ${tools.length + resources.length + prompts.length}\n`);
}

main();
