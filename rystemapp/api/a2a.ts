import type { VercelRequest, VercelResponse } from '@vercel/node';
import { readFile } from 'fs/promises';
import { join } from 'path';

// ── A2A Protocol Types (JSON-RPC 2.0 over HTTP) ───────────────────────────────

type TaskState = 'submitted' | 'working' | 'completed' | 'failed' | 'canceled';

interface TextPart { type: 'text'; text: string }
interface DataPart { type: 'data'; data: Record<string, unknown> }
type Part = TextPart | DataPart;

interface Message {
    role: 'user' | 'agent';
    parts: Part[];
}

interface TaskStatus {
    state: TaskState;
    timestamp: string;
    message?: Message;
}

interface Artifact {
    name: string;
    description?: string;
    parts: Part[];
}

interface Task {
    id: string;
    status: TaskStatus;
    artifacts?: Artifact[];
}

interface TaskSendParams {
    id: string;
    skillId?: string;
    message: Message;
    metadata?: Record<string, unknown>;
}

interface JsonRpcRequest {
    jsonrpc: '2.0';
    id: string | number;
    method: string;
    params?: unknown;
}

// ── Manifest types ─────────────────────────────────────────────────────────────

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
    dynamicTools: DynamicTool[];
}

// ── Manifest cache ─────────────────────────────────────────────────────────────

let cachedManifest: McpManifest | null = null;

async function getManifest(): Promise<McpManifest> {
    if (!cachedManifest) {
        const raw = await readFile(join(process.cwd(), 'public', 'mcp-manifest.json'), 'utf-8');
        cachedManifest = JSON.parse(raw) as McpManifest;
    }
    return cachedManifest!;
}

// ── Skill executor ─────────────────────────────────────────────────────────────

async function executeSkill(
    skillId: string,
    args: Record<string, string | undefined>,
    manifest: McpManifest
): Promise<{ text: string; isError: boolean }> {
    // Find which dynamic tool handles this skillId
    const baseName = skillId.replace(/-list$/, '').replace(/-search$/, '');
    const dt = manifest.dynamicTools.find(t => t.name === baseName);

    if (!dt) {
        return { text: `❌ Unknown skill: "${skillId}". Available: ${manifest.dynamicTools.flatMap(t => [t.name, `${t.name}-list`, `${t.name}-search`]).join(', ')}`, isError: true };
    }

    // Build lookup maps
    const mapping: Record<string, Record<string, string>> = {};
    const categoryInfo: Record<string, Array<{ value: string; title?: string; description?: string }>> = {};
    for (const doc of dt.documents) {
        if (!mapping[doc.id]) { mapping[doc.id] = {}; categoryInfo[doc.id] = []; }
        mapping[doc.id][doc.value] = doc.filename;
        categoryInfo[doc.id].push({ value: doc.value, title: doc.metadata?.title, description: doc.metadata?.description });
    }

    // ── -list skill ───────────────────────────────────────────────────────────
    if (skillId.endsWith('-list')) {
        const id = args.id;
        if (id) {
            const topics = categoryInfo[id];
            if (!topics) return { text: `❌ Unknown category "${id}". Available: ${Object.keys(categoryInfo).join(', ')}`, isError: true };
            return { text: `Topics for "${id}":\n${topics.map(t => `  - ${t.value}${t.title ? ` (${t.title})` : ''}`).join('\n')}`, isError: false };
        }
        const text = Object.entries(categoryInfo)
            .map(([cat, topics]) => `**${cat}**\n${topics.map(t => `  - ${t.value}${t.title ? ` (${t.title})` : ''}`).join('\n')}`)
            .join('\n\n');
        return { text, isError: false };
    }

    // ── -search skill ─────────────────────────────────────────────────────────
    if (skillId.endsWith('-search')) {
        const query = args.query ?? '';
        const keywords = query.toLowerCase().split(/\s+/).filter(k => k.length > 0);
        if (keywords.length === 0) return { text: '⚠️ Empty query.', isError: false };

        const matches: Array<{ id: string; value: string; title?: string; score: number }> = [];
        for (const [id, topics] of Object.entries(mapping)) {
            for (const value of Object.keys(topics)) {
                const info = categoryInfo[id].find(i => i.value === value);
                const searchText = `${id} ${value} ${info?.title ?? ''} ${info?.description ?? ''}`.toLowerCase();
                const score = keywords.reduce((s, k) => s + (searchText.includes(k) ? k.length : 0), 0);
                if (score > 0) matches.push({ id, value, title: info?.title, score });
            }
        }
        matches.sort((a, b) => b.score - a.score);
        if (matches.length === 0) return { text: `❌ No matches for "${query}"`, isError: false };
        const text = matches.slice(0, 10).map((m, i) =>
            `${i + 1}. **${m.id}** → \`${m.value}\`${m.title ? ` (${m.title})` : ''}\n   Usage: ${dt.name}(id="${m.id}", value="${m.value}")`
        ).join('\n\n');
        return { text: `🔍 ${matches.length} matches:\n\n${text}`, isError: false };
    }

    // ── main skill ────────────────────────────────────────────────────────────
    const id = args.id;
    const value = args.value;

    if (!id || !value) {
        return {
            text: `❌ Missing required args: id and value.\n\nRequired: ${Object.entries(dt.inputSchema)
                .filter(([, v]) => v.required)
                .map(([k, v]) => `${k}: ${v.description}`)
                .join(', ')}`,
            isError: true
        };
    }

    const filename = mapping[id]?.[value];
    if (!filename) {
        const errorText = mapping[id]
            ? `❌ Not found: id="${id}", value="${value}"\n\nAvailable topics for "${id}":\n${categoryInfo[id].map(i => `  - ${i.value}`).join('\n')}`
            : `❌ Unknown category "${id}"\n\nAvailable: ${Object.keys(mapping).join(', ')}`;
        return { text: errorText, isError: true };
    }

    try {
        const content = await readFile(join(process.cwd(), 'public', 'mcp', 'tools', dt.name, filename), 'utf-8');
        return { text: content, isError: false };
    } catch (e) {
        return { text: `❌ Failed to read documentation: ${e}`, isError: true };
    }
}

// ── Args extraction from A2A message ─────────────────────────────────────────

function extractSkillAndArgs(params: TaskSendParams): { skillId: string; args: Record<string, string | undefined> } {
    // 1. skillId from params + DataPart args
    if (params.skillId) {
        const dataPart = params.message.parts.find((p): p is DataPart => p.type === 'data');
        const args = ((dataPart?.data?.args ?? dataPart?.data) as Record<string, string | undefined>) ?? {};
        return { skillId: params.skillId, args };
    }

    // 2. DataPart with { skill, args }
    const dataPart = params.message.parts.find((p): p is DataPart => p.type === 'data');
    if (dataPart?.data?.skill && typeof dataPart.data.skill === 'string') {
        return {
            skillId: dataPart.data.skill,
            args: (dataPart.data.args as Record<string, string | undefined>) ?? {},
        };
    }

    // 3. TextPart as plain query → route to -search
    const textPart = params.message.parts.find((p): p is TextPart => p.type === 'text');
    const text = textPart?.text?.trim() ?? '';
    return { skillId: 'get-rystem-docs-search', args: { query: text } };
}

// ── JSON-RPC helpers ──────────────────────────────────────────────────────────

function rpcResult(id: string | number, result: unknown) {
    return { jsonrpc: '2.0', id, result };
}
function rpcError(id: string | number | null, code: number, message: string, data?: unknown) {
    return { jsonrpc: '2.0', id, error: { code, message, ...(data !== undefined ? { data } : {}) } };
}

// ── Vercel handler ─────────────────────────────────────────────────────────────

export default async function handler(req: VercelRequest, res: VercelResponse) {
    // CORS
    res.setHeader('Access-Control-Allow-Origin', '*');
    res.setHeader('Access-Control-Allow-Methods', 'POST, GET, OPTIONS');
    res.setHeader('Access-Control-Allow-Headers', 'Content-Type');

    if (req.method === 'OPTIONS') { res.status(204).end(); return; }

    // Agent Card discovery via GET
    if (req.method === 'GET') {
        try {
            const card = await readFile(join(process.cwd(), 'public', '.well-known', 'agent.json'), 'utf-8');
            res.setHeader('Content-Type', 'application/json');
            res.status(200).send(card);
        } catch {
            res.status(404).json({ error: 'Agent card not found' });
        }
        return;
    }

    if (req.method !== 'POST') {
        res.status(405).json(rpcError(null, -32600, 'Method not allowed'));
        return;
    }

    const rpc = req.body as JsonRpcRequest;
    if (!rpc || rpc.jsonrpc !== '2.0' || !rpc.method) {
        res.status(400).json(rpcError(rpc?.id ?? null, -32600, 'Invalid JSON-RPC request'));
        return;
    }

    // Only tasks/send supported (stateless — no tasks/get or tasks/cancel)
    if (rpc.method !== 'tasks/send') {
        res.status(200).json(rpcError(rpc.id, -32601, `Method "${rpc.method}" not supported. Supported: tasks/send`));
        return;
    }

    const params = rpc.params as TaskSendParams;
    if (!params?.id || !params?.message) {
        res.status(200).json(rpcError(rpc.id, -32602, 'Invalid params: id and message are required'));
        return;
    }

    try {
        const manifest = await getManifest();
        const { skillId, args } = extractSkillAndArgs(params);
        const { text, isError } = await executeSkill(skillId, args, manifest);

        const task: Task = {
            id: params.id,
            status: {
                state: isError ? 'failed' : 'completed',
                timestamp: new Date().toISOString(),
            },
            artifacts: isError ? [] : [
                {
                    name: 'result',
                    description: `Output from skill: ${skillId}`,
                    parts: [{ type: 'text', text }],
                },
            ],
        };

        // If failed, put error in status message
        if (isError) {
            task.status.message = { role: 'agent', parts: [{ type: 'text', text }] };
        }

        res.status(200).json(rpcResult(rpc.id, task));
    } catch (err) {
        console.error('[A2A] Error:', err);
        res.status(200).json(rpcError(rpc.id, -32603, 'Internal error', err instanceof Error ? err.message : String(err)));
    }
}
