#!/usr/bin/env tsx
/**
 * build-a2a.ts
 * Generates public/.well-known/agent.json (A2A Agent Card) from the MCP manifest.
 * Skills map 1:1 to dynamic tools (each tool = one skill).
 * Run: npx tsx scripts/build-a2a.ts
 */

import { readFileSync, writeFileSync, mkdirSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const rootDir = join(__dirname, '..');
const manifestPath = join(rootDir, 'public', 'mcp-manifest.json');
const outDir = join(rootDir, 'public', '.well-known');
const outFile = join(outDir, 'agent.json');

// ── Types ──────────────────────────────────────────────────────────────────────

interface DynamicTool {
    name: string;
    title: string;
    description: string;
    inputSchema: Record<string, { type: string; description: string; required: boolean }>;
}

interface McpManifest {
    name: string;
    version: string;
    description: string;
    dynamicTools?: DynamicTool[];
    tools?: Array<{ name: string; title?: string; description?: string }>;
    resources?: Array<{ name: string; title?: string; description?: string }>;
    prompts?: Array<{ name: string; title?: string; description?: string }>;
}

// ── A2A Agent Card types (per spec) ─────────────────────────────────────────────

interface A2ASkill {
    id: string;
    name: string;
    description: string;
    tags?: string[];
    inputModes: string[];
    outputModes: string[];
    /** Declare the structured input schema so agents know what args to pass */
    examples?: string[];
}

interface AgentCapabilities {
    streaming: boolean;
    pushNotifications: boolean;
    stateTransitionHistory: boolean;
}

interface AgentCard {
    name: string;
    description: string;
    url: string;
    version: string;
    documentationUrl: string;
    capabilities: AgentCapabilities;
    defaultInputModes: string[];
    defaultOutputModes: string[];
    skills: A2ASkill[];
}

// ── Build ─────────────────────────────────────────────────────────────────────

const manifest: McpManifest = JSON.parse(readFileSync(manifestPath, 'utf-8'));

const skills: A2ASkill[] = [];

// Dynamic tools → skills (main tool only, not -list/-search companions)
for (const dt of manifest.dynamicTools ?? []) {
    const schemaDesc = Object.entries(dt.inputSchema)
        .map(([k, v]) => `${k}${v.required ? '' : '?'}: ${v.description}`)
        .join(' | ');

    skills.push({
        id: dt.name,
        name: dt.title,
        description: dt.description,
        tags: ['docs', 'rystem', 'framework'],
        inputModes: ['text', 'data'],
        outputModes: ['text'],
        examples: [
            `{ "skill": "${dt.name}", "args": { ${schemaDesc} } }`,
        ],
    });

    // Also expose -list and -search as separate skills
    skills.push({
        id: `${dt.name}-list`,
        name: `List ${dt.title}`,
        description: `List all available categories and topics for ${dt.name}. Optionally filter by category id.`,
        tags: ['docs', 'rystem', 'list'],
        inputModes: ['text', 'data'],
        outputModes: ['text'],
        examples: [`{ "skill": "${dt.name}-list", "args": {} }`, `{ "skill": "${dt.name}-list", "args": { "id": "auth" } }`],
    });

    skills.push({
        id: `${dt.name}-search`,
        name: `Search ${dt.title}`,
        description: `Search documentation by keyword with progressive disambiguation.`,
        tags: ['docs', 'rystem', 'search'],
        inputModes: ['text', 'data'],
        outputModes: ['text'],
        examples: [`{ "skill": "${dt.name}-search", "args": { "query": "dependency injection" } }`],
    });
}

const agentCard: AgentCard = {
    name: 'Rystem Framework Agent',
    description: manifest.description,
    url: 'https://rystem.cloud/a2a',
    version: manifest.version,
    documentationUrl: 'https://rystem.cloud/mcp',
    capabilities: {
        streaming: false,
        pushNotifications: false,
        stateTransitionHistory: false,
    },
    defaultInputModes: ['text', 'data'],
    defaultOutputModes: ['text'],
    skills,
};

mkdirSync(outDir, { recursive: true });
writeFileSync(outFile, JSON.stringify(agentCard, null, 2), 'utf-8');

console.log(`✅ A2A Agent Card written to public/.well-known/agent.json`);
console.log(`   Skills: ${skills.length} (${manifest.dynamicTools?.length ?? 0} tools × 3)`);
