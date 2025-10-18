import { readFileSync, writeFileSync, mkdirSync, copyFileSync, existsSync, readdirSync, statSync } from 'fs';
import { join, relative, dirname, sep, basename, extname } from 'path';
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
  packageName?: string;
  packageType?: 'nuget' | 'npm';
}

interface PackageInfo {
  name: string;
  readmePath: string;
  projectPath: string;
  type: 'nuget' | 'npm';
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
  '.next',
  'dist',
  'build'
];

function getPackageNameFromCsproj(csprojPath: string): string | null {
  try {
    const content = readFileSync(csprojPath, 'utf-8');
    
    // Try to find PackageId first
    const packageIdMatch = content.match(/<PackageId>(.*?)<\/PackageId>/);
    if (packageIdMatch) {
      return packageIdMatch[1];
    }
    
    // Fallback to AssemblyName
    const assemblyMatch = content.match(/<AssemblyName>(.*?)<\/AssemblyName>/);
    if (assemblyMatch) {
      return assemblyMatch[1];
    }
    
    // If nothing found, use the filename without extension
    return basename(csprojPath, '.csproj');
  } catch (error) {
    return null;
  }
}

function getPackageNameFromPackageJson(packageJsonPath: string): string | null {
  try {
    const content = readFileSync(packageJsonPath, 'utf-8');
    const pkg = JSON.parse(content);
    return pkg.name || null;
  } catch (error) {
    return null;
  }
}

function cleanPackageName(name: string, category?: string): string {
  // Remove "Rystem." prefix
  let cleaned = name.replace(/^Rystem\./i, '');
  
  // Remove "RepositoryFramework." prefix for Repository category
  if (category?.toLowerCase() === 'repository') {
    cleaned = cleaned.replace(/^RepositoryFramework\./i, '');
  }
  
  // Remove category name prefix if present
  if (category) {
    const categoryRegex = new RegExp(`^${category}\\.`, 'i');
    cleaned = cleaned.replace(categoryRegex, '');
  }
  
  return cleaned;
}

function findPackagesWithReadme(dir: string, baseDir: string = dir): PackageInfo[] {
  const packages: PackageInfo[] = [];
  
  if (!existsSync(dir) || shouldExcludeDirectory(basename(dir))) {
    return packages;
  }
  
  try {
    const items = readdirSync(dir);
    const readmeFile = items.find(item => item.toLowerCase() === 'readme.md');
    
    if (readmeFile) {
      const readmePath = join(dir, readmeFile);
      
      // Look for .csproj files
      const csprojFiles = items.filter(item => item.endsWith('.csproj'));
      for (const csprojFile of csprojFiles) {
        const csprojPath = join(dir, csprojFile);
        const packageName = getPackageNameFromCsproj(csprojPath);
        if (packageName) {
          packages.push({
            name: packageName,
            readmePath: relative(baseDir, readmePath),
            projectPath: relative(baseDir, csprojPath),
            type: 'nuget'
          });
        }
      }
      
      // Look for package.json
      const packageJsonFile = items.find(item => item === 'package.json');
      if (packageJsonFile) {
        const packageJsonPath = join(dir, packageJsonFile);
        const packageName = getPackageNameFromPackageJson(packageJsonPath);
        if (packageName) {
          packages.push({
            name: packageName,
            readmePath: relative(baseDir, readmePath),
            projectPath: relative(baseDir, packageJsonPath),
            type: 'npm'
          });
        }
      }
      
      // If no project file found but README exists, use directory name
      if (packages.length === 0) {
        packages.push({
          name: basename(dir),
          readmePath: relative(baseDir, readmePath),
          projectPath: '',
          type: 'nuget' // Default type
        });
      }
    }
    
    // Recursively scan subdirectories
    for (const item of items) {
      const fullPath = join(dir, item);
      if (statSync(fullPath).isDirectory() && !shouldExcludeDirectory(item)) {
        packages.push(...findPackagesWithReadme(fullPath, baseDir));
      }
    }
  } catch (error) {
    console.warn(`Error scanning directory ${dir}:`, error);
  }
  
  return packages;
}

function createHierarchyFromPackageName(packageName: string, readmePath: string, packageType: 'nuget' | 'npm', category?: string): DocNode {
  // Clean the package name
  const cleanedName = cleanPackageName(packageName, category);
  
  // Split by dots to create hierarchy
  const parts = cleanedName.split('.').filter(p => p.trim().length > 0);
  
  if (parts.length === 0) {
    // Fallback to original name
    return {
      id: readmePath.replace(/\\/g, '/').replace(/\/README\.md$/i, ''),
      name: packageName,
      title: packageName,
      path: readmePath.replace(/\\/g, '/'),
      type: 'file',
      packageName: packageName,
      packageType: packageType
    };
  }
  
  if (parts.length === 1) {
    // No hierarchy needed
    return {
      id: readmePath.replace(/\\/g, '/').replace(/\/README\.md$/i, ''),
      name: parts[0],
      title: parts[0],
      path: readmePath.replace(/\\/g, '/'),
      type: 'file',
      packageName: packageName,
      packageType: packageType
    };
  }
  
  // Build hierarchy bottom-up
  let currentNode: DocNode = {
    id: readmePath.replace(/\\/g, '/').replace(/\/README\.md$/i, ''),
    name: parts[parts.length - 1],
    title: parts[parts.length - 1],
    path: readmePath.replace(/\\/g, '/'),
    type: 'file',
    packageName: packageName,
    packageType: packageType
  };
  
  // Create parent folders from right to left
  for (let i = parts.length - 2; i >= 0; i--) {
    const folderNode: DocNode = {
      id: parts.slice(0, i + 1).join('.'),
      name: parts[i],
      title: parts[i],
      type: 'folder',
      children: [currentNode],
    };
    currentNode = folderNode;
  }
  
  return currentNode;
}

function mergeNodes(existingNodes: DocNode[], newNode: DocNode): DocNode[] {
  // Find if we already have a node with the same name at this level
  const existingIndex = existingNodes.findIndex(n => n.name === newNode.name && n.type === newNode.type);
  
  if (existingIndex === -1) {
    // No existing node, just add the new one
    return [...existingNodes, newNode];
  }
  
  const existing = existingNodes[existingIndex];
  
  // If it's a file, don't merge (files are unique)
  if (newNode.type === 'file') {
    return [...existingNodes, newNode];
  }
  
  // If it's a folder, merge children
  if (newNode.children && existing.children) {
    let mergedChildren = [...existing.children];
    for (const child of newNode.children) {
      mergedChildren = mergeNodes(mergedChildren, child);
    }
    existing.children = mergedChildren;
  } else if (newNode.children) {
    existing.children = newNode.children;
  }
  
  return existingNodes;
}

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
    
    // Find all packages with README in this category
    const packages = findPackagesWithReadme(categoryPath, PACKAGES_ROOT);
    
    if (packages.length === 0) {
      continue;
    }
    
    // Copy READMEs and build hierarchy
    let hierarchicalChildren: DocNode[] = [];
    
    for (const pkg of packages) {
      // Copy README to output
      const readmeSourcePath = join(PACKAGES_ROOT, pkg.readmePath);
      const readmeOutputPath = join(OUTPUT_DIR, pkg.readmePath);
      
      ensureDirectoryExists(dirname(readmeOutputPath));
      copyFileSync(readmeSourcePath, readmeOutputPath);
      console.log(`✓ Copied: ${pkg.readmePath} (${pkg.name})`);
      
      // Create hierarchy node for this package
      const hierarchyNode = createHierarchyFromPackageName(pkg.name, pkg.readmePath, pkg.type, categoryDir);
      hierarchicalChildren = mergeNodes(hierarchicalChildren, hierarchyNode);
    }
    
    const categoryNode: DocNode = {
      id: categoryDir,
      name: categoryDir,
      title: categoryDir,
      type: 'category',
      children: hierarchicalChildren,
    };
    
    categories.push(categoryNode);
  }
  
  // Sort categories: Core first, then alphabetically
  categories.sort((a, b) => {
    if (a.name.toLowerCase() === 'core') return -1;
    if (b.name.toLowerCase() === 'core') return 1;
    return a.name.localeCompare(b.name);
  });
  
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
