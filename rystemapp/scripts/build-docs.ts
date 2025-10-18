import { readFileSync, writeFileSync, mkdirSync, copyFileSync, existsSync, readdirSync, statSync } from 'fs';
import { join, relative, dirname, sep } from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

interface DocEntry {
  path: string;
  title: string;
  relativePath: string;
  category?: string;
}

const PACKAGES_ROOT = join(__dirname, '..', '..', 'src');
const OUTPUT_DIR = join(__dirname, '..', 'src', 'generated');
const INDEX_FILE = join(OUTPUT_DIR, 'index.json');

function ensureDirectoryExists(dirPath: string) {
  if (!existsSync(dirPath)) {
    mkdirSync(dirPath, { recursive: true });
  }
}

function findAllReadmes(dir: string, baseDir: string = dir): DocEntry[] {
  const entries: DocEntry[] = [];

  if (!existsSync(dir)) {
    console.warn(`Directory not found: ${dir}`);
    return entries;
  }

  const items = readdirSync(dir);

  for (const item of items) {
    const fullPath = join(dir, item);
    const stat = statSync(fullPath);

    if (stat.isDirectory()) {
      // Recursively search subdirectories
      entries.push(...findAllReadmes(fullPath, baseDir));
    } else if (item.toLowerCase() === 'readme.md') {
      const relativePath = relative(baseDir, fullPath);
      const pathParts = relativePath.split(sep);
      
      // Extract category from path (e.g., "Core", "Repository", "Extensions")
      let category = 'Other';
      if (pathParts.length > 0) {
        category = pathParts[0];
      }

      // Extract title from the directory name or first heading in README
      let title = pathParts[pathParts.length - 2] || 'Root';
      
      // Try to extract title from README content
      try {
        const content = readFileSync(fullPath, 'utf-8');
        const match = content.match(/^#\s+(.+)$/m);
        if (match) {
          title = match[1];
        }
      } catch (error) {
        console.warn(`Could not read ${fullPath} for title extraction`);
      }

      const outputPath = join(OUTPUT_DIR, relativePath);
      
      entries.push({
        path: relativePath,
        title,
        relativePath: relativePath.replace(/\\/g, '/'),
        category,
      });

      // Copy README to output directory
      ensureDirectoryExists(dirname(outputPath));
      copyFileSync(fullPath, outputPath);
      console.log(`✓ Copied: ${relativePath}`);
    }
  }

  return entries;
}

function main() {
  console.log('🔍 Scanning for README files...\n');
  
  // Ensure output directory exists
  ensureDirectoryExists(OUTPUT_DIR);

  // Find all READMEs
  const docs = findAllReadmes(PACKAGES_ROOT);

  // Sort by category and title
  docs.sort((a, b) => {
    if (a.category !== b.category) {
      return (a.category || '').localeCompare(b.category || '');
    }
    return a.title.localeCompare(b.title);
  });

  // Write index.json
  writeFileSync(INDEX_FILE, JSON.stringify(docs, null, 2), 'utf-8');
  
  console.log(`\n✅ Generated documentation index with ${docs.length} entries`);
  console.log(`📄 Index file: ${relative(join(__dirname, '..'), INDEX_FILE)}\n`);

  // Print summary by category
  const categories = new Map<string, number>();
  docs.forEach(doc => {
    const cat = doc.category || 'Other';
    categories.set(cat, (categories.get(cat) || 0) + 1);
  });

  console.log('📊 Summary by category:');
  categories.forEach((count, category) => {
    console.log(`   ${category}: ${count} packages`);
  });
}

main();
