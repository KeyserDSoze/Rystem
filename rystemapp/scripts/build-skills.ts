#!/usr/bin/env tsx
/**
 * build-skills.ts
 * Generates Anthropic-compatible Skill ZIP packages from the MCP manifest.
 * One ZIP per category (id) per dynamic tool.
 * Output: public/skills/{tool}-{category}.zip  + public/skills/index.json
 *
 * Spec: https://agentskills.io
 * Run:  npx tsx scripts/build-skills.ts
 */

import { readFileSync, writeFileSync, mkdirSync, existsSync, createWriteStream } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';
import archiver from 'archiver';

const __dirname = dirname(fileURLToPath(import.meta.url));
const rootDir = join(__dirname, '..');
const manifestPath = join(rootDir, 'public', 'mcp-manifest.json');
const toolsDir = join(rootDir, 'public', 'mcp', 'tools');
const outDir = join(rootDir, 'public', 'skills');

// ── Types ──────────────────────────────────────────────────────────────────────

interface DynamicToolDocument {
    filename: string;
    id: string;
    value: string;
    metadata?: { title?: string; description?: string };
}

interface DynamicTool {
    name: string;
    title: string;
    description: string;
    inputSchema: Record<string, { type: string; description: string; required: boolean }>;
    documents: DynamicToolDocument[];
}

interface McpManifest {
    name: string;
    version: string;
    description: string;
    dynamicTools?: DynamicTool[];
}

interface SkillEntry {
    id: string;
    name: string;
    description: string;
    category: string;
    tool: string;
    docCount: number;
    zipFile: string;
}

// ── Helpers ────────────────────────────────────────────────────────────────────

/** Capitalize first letter of each word */
function titleCase(s: string): string {
    return s.replace(/(^|[-\s])([a-z])/g, (_, sep, c) => sep + c.toUpperCase());
}

/** Truncate to max chars, append ellipsis if needed */
function trunc(s: string, max: number): string {
    return s.length <= max ? s : s.slice(0, max - 1) + '…';
}

/** Write an archiver zip and return a promise that resolves when done */
function writeZip(zipPath: string, entries: Array<{ name: string; content: string }>): Promise<void> {
    return new Promise((resolve, reject) => {
        const out = createWriteStream(zipPath);
        const archive = archiver('zip', { zlib: { level: 9 } });

        out.on('close', resolve);
        archive.on('error', reject);

        archive.pipe(out);
        for (const e of entries) {
            archive.append(e.content, { name: e.name });
        }
        archive.finalize();
    });
}

// ── Build ─────────────────────────────────────────────────────────────────────

const manifest: McpManifest = JSON.parse(readFileSync(manifestPath, 'utf-8'));
mkdirSync(outDir, { recursive: true });

const index: SkillEntry[] = [];
const tasks: Array<Promise<void>> = [];

let totalZips = 0;

for (const tool of manifest.dynamicTools ?? []) {
    const toolFiles = join(toolsDir, tool.name);

    // Group documents by category (id)
    const byCategory = new Map<string, DynamicToolDocument[]>();
    for (const doc of tool.documents) {
        if (!byCategory.has(doc.id)) byCategory.set(doc.id, []);
        byCategory.get(doc.id)!.push(doc);
    }

    for (const [category, docs] of byCategory) {
        const skillId = `${tool.name}-${category}`;
        const skillName = trunc(`Rystem - ${titleCase(tool.title)} / ${titleCase(category)}`, 64);

        // Build description: list first few topics
        const topicList = docs.slice(0, 4).map(d => d.metadata?.title ?? titleCase(d.value)).join(', ');
        const more = docs.length > 4 ? ` and ${docs.length - 4} more` : '';
        const description = trunc(
            `Rystem Framework docs: ${topicList}${more}. Use for ${category} questions in Rystem.`,
            200,
        );

        // Build SKILL.md content
        const lines: string[] = [
            '---',
            `name: ${skillName}`,
            `description: ${description}`,
            '---',
            '',
            `# ${skillName}`,
            '',
            `> Auto-generated from Rystem MCP manifest (tool: \`${tool.name}\`, category: \`${category}\`)`,
            '',
        ];

        for (const doc of docs) {
            const filePath = join(toolFiles, doc.filename);
            if (!existsSync(filePath)) {
                console.warn(`  ⚠️  Missing file: ${filePath}`);
                continue;
            }
            const content = readFileSync(filePath, 'utf-8');

            lines.push(`---`);
            lines.push('');
            if (doc.metadata?.title) {
                lines.push(`## ${doc.metadata.title}`);
                lines.push('');
            }
            if (doc.metadata?.description) {
                lines.push(`> ${doc.metadata.description}`);
                lines.push('');
            }
            lines.push(content.trim());
            lines.push('');
        }

        const skillMd = lines.join('\n');
        const zipName = `${skillId}.zip`;
        const zipPath = join(outDir, zipName);

        // ZIP: skillId/SKILL.md  (folder name = skill id)
        tasks.push(
            writeZip(zipPath, [
                { name: `${skillId}/SKILL.md`, content: skillMd },
            ]).then(() => {
                console.log(`  ✅ ${zipName}  (${docs.length} docs)`);
                totalZips++;
            }),
        );

        index.push({ id: skillId, name: skillName, description, category, tool: tool.name, docCount: docs.length, zipFile: `/skills/${zipName}` });
    }
}

await Promise.all(tasks);

// Write index.json for the UI
writeFileSync(join(outDir, 'index.json'), JSON.stringify(index, null, 2), 'utf-8');

console.log(`\n✅ ${totalZips} Skill ZIPs written to public/skills/`);
console.log(`✅ Index written to public/skills/index.json`);
