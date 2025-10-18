import { readFileSync, writeFileSync, mkdirSync, copyFileSync, existsSync, readdirSync, statSync } from 'fs';
import { join, relative, dirname, sep, basename } from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

interface DocNode {
  id: string;
  name: string;
  title: string;
  path?: string;
  children?: DocNode[];
  type: 'category' | 'folder' | 'file';
}

const PACKAGES_ROOT = join(__dirname, '..', '..', 'src');
const OUTPUT_DIR = join(__dirname, '..', 'public', 'generated');
const INDEX_FILE = join(OUTPUT_DIR, 'index.json');

// Directories to exclude from scanning
const EXCLUDED_DIRS = [
  'node_modules',
  'bin',
  'obj',
  '.git',
  '.vs',
  'TestResults',
  'packages',
  'wwwroot',
];

function ensureDirectoryExists(dirPath: string) {
  if (!existsSync(dirPath)) {
    mkdirSync(dirPath, { recursive: true });
  }
}

function shouldExcludeDirectory(dirName: string): boolean {
  return EXCLUDED_DIRS.some(excluded => 
    dirName.toLowerCase().includes(excluded.toLowerCase())
  );
}

function findAllReadmes(dir: string, baseDir: string = dir): DocNode[] {
  const entries: DocNode[] = [];

  if (!existsSync(dir)) {
    console.warn(`Directory not found: ${dir}`);
    return entries;
  }

  const items = readdirSync(dir);
  let hasReadme = false;
  const subdirs: string[] = [];

  // First pass: check for README and collect subdirectories
  for (const item of items) {
    const fullPath = join(dir, item);
    const stat = statSync(fullPath);

    if (stat.isDirectory()) {
      if (!shouldExcludeDirectory(item)) {
        subdirs.push(item);
      }
    } else if (item.toLowerCase() === 'readme.md') {
      hasReadme = true;
      hasReadme = true;
      const relativePath = relative(baseDir, fullPath);
      
      // Extract title from README content
      let title = basename(dir);
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
      
      // Copy README to output directory
      ensureDirectoryExists(dirname(outputPath));
      copyFileSync(fullPath, outputPath);
      console.log(`✓ Copied: ${relativePath}`);
    }
  }

  // If this directory has a README, create a node for it
  if (hasReadme) {
    const relativePath = relative(baseDir, join(dir, 'README.md'));
    const dirName = basename(dir);
    
    let title = dirName;
    const readmePath = join(dir, 'README.md');
    try {
      const content = readFileSync(readmePath, 'utf-8');
      const match = content.match(/^#\s+(.+)$/m);
      if (match) {
        title = match[1];
      }
    } catch (error) {
      // Title already set to dirName
    }

    const node: DocNode = {
      id: relativePath.replace(/\\/g, '/').replace(/\/README\.md$/i, ''),
      name: dirName,
      title: title,
      path: relativePath.replace(/\\/g, '/'),
      type: 'file',
      children: [],
    };

    // Process subdirectories
    for (const subdir of subdirs) {
      const subdirPath = join(dir, subdir);
      const subdirNodes = findAllReadmes(subdirPath, baseDir);
      if (subdirNodes.length > 0) {
        node.children!.push(...subdirNodes);
      }
    }

    // If no children, remove the children property
    if (node.children!.length === 0) {
      delete node.children;
    }

    return [node];
  } else {
    // No README in this directory, but process subdirectories
    for (const subdir of subdirs) {
      const subdirPath = join(dir, subdir);
      entries.push(...findAllReadmes(subdirPath, baseDir));
    }
  }

  return entries;
}

function buildCategoryTree(): DocNode[] {
  const categories: DocNode[] = [];
  
  if (!existsSync(PACKAGES_ROOT)) {
    console.error(`Source directory not found: ${PACKAGES_ROOT}`);
    return categories;
  }

  const categoryDirs = readdirSync(PACKAGES_ROOT);
  
  for (const categoryDir of categoryDirs) {
    const categoryPath = join(PACKAGES_ROOT, categoryDir);
    const stat = statSync(categoryPath);
    
    if (!stat.isDirectory() || shouldExcludeDirectory(categoryDir)) {
      continue;
    }

    console.log(`\n📁 Processing category: ${categoryDir}`);
    
    const categoryNodes = findAllReadmes(categoryPath, PACKAGES_ROOT);
    
    if (categoryNodes.length > 0) {
      const categoryNode: DocNode = {
        id: categoryDir,
        name: categoryDir,
        title: categoryDir,
        type: 'category',
        children: categoryNodes,
      };
      
      categories.push(categoryNode);
    }
  }
  
  return categories;
}

function countNodes(nodes: DocNode[]): number {
  let count = 0;
  for (const node of nodes) {
    count++;
    if (node.children) {
      count += countNodes(node.children);
    }
  }
  return count;
}

function main() {
  console.log('🔍 Scanning for README files in Rystem packages...\n');
  
  // Ensure output directory exists
  ensureDirectoryExists(OUTPUT_DIR);

  // Build the category tree
  const tree = buildCategoryTree();

  // Write index.json
  writeFileSync(INDEX_FILE, JSON.stringify(tree, null, 2), 'utf-8');
  
  const totalDocs = countNodes(tree);
  
  console.log(`\n✅ Generated documentation tree with ${totalDocs} entries`);
  console.log(`📄 Index file: ${relative(join(__dirname, '..'), INDEX_FILE)}\n`);

  // Print summary by category
  console.log('📊 Summary by category:');
  tree.forEach(category => {
    const count = countNodes(category.children || []);
    console.log(`   ${category.name}: ${count} packages`);
  });
}

main();
