import { readFile } from 'fs/promises';
import { join } from 'path';

interface DynamicToolDocument {
    filename: string;
    id: string;
    value: string;
    metadata?: {
        title?: string;
        description?: string;
    };
}

interface DynamicTool {
    name: string;
    documents: DynamicToolDocument[];
}

interface McpManifest {
    dynamicTools: DynamicTool[];
}

// Load manifest
const manifestPath = join(process.cwd(), 'public', 'mcp-manifest.json');
const manifestContent = await readFile(manifestPath, 'utf-8');
const manifest: McpManifest = JSON.parse(manifestContent);

const dynamicTool = manifest.dynamicTools[0];

// Build mapping
const mapping: Record<string, Record<string, string>> = {};
const categoryInfo: Record<string, Array<{ value: string; title?: string }>> = {};

for (const doc of dynamicTool.documents) {
    if (!mapping[doc.id]) {
        mapping[doc.id] = {};
        categoryInfo[doc.id] = [];
    }
    mapping[doc.id][doc.value] = doc.filename;
    categoryInfo[doc.id].push({
        value: doc.value,
        title: doc.metadata?.title
    });
}

// Smart search algorithm
const findBestMatch = (query: string): { id: string; value: string; title?: string; score: number } | null => {
    const keywords = query.toLowerCase().split(/\s+/).filter(k => k.length > 2);
    console.log(`\n🔍 Query: "${query}"`);
    console.log(`📝 Keywords: [${keywords.map(k => `"${k}"`).join(', ')}]`);
    
    if (keywords.length === 0) return null;

    let candidates: Array<{ id: string; value: string; title?: string; score: number; matchedKeywords: string[] }> = [];

    for (const [id, topics] of Object.entries(mapping)) {
        for (const value of Object.keys(topics)) {
            const info = categoryInfo[id].find(i => i.value === value);
            const searchText = `${id} ${value} ${info?.title || ''}`.toLowerCase();
            
            let score = 0;
            let matchedKeywords: string[] = [];
            
            for (const keyword of keywords) {
                if (searchText.includes(keyword)) {
                    matchedKeywords.push(keyword);
                    score += keyword.length * 100;
                }
            }
            
            if (matchedKeywords.length > 0) {
                const totalScore = matchedKeywords.length * 1000 + score;
                candidates.push({ id, value, title: info?.title, score: totalScore, matchedKeywords });
            }
        }
    }

    if (candidates.length === 0) {
        console.log('❌ No matches found');
        return null;
    }
    
    candidates.sort((a, b) => b.score - a.score);
    
    console.log(`\n📊 Top 5 candidates:`);
    candidates.slice(0, 5).forEach((c, i) => {
        console.log(`  ${i + 1}. [${c.id}/${c.value}] score=${c.score} matched=[${c.matchedKeywords.join(', ')}]${c.title ? ` - ${c.title}` : ''}`);
    });
    
    // Return best match if:
    // 1. Only one candidate
    // 2. Top candidate significantly better (1.2x threshold)
    if (candidates.length === 1) {
        console.log(`\n✅ Clear winner (only candidate): [${candidates[0].id}/${candidates[0].value}]`);
        return candidates[0];
    }
    
    const topScore = candidates[0].score;
    const secondScore = candidates[1].score;
    const ratio = topScore / secondScore;
    
    if (topScore > secondScore * 1.2) {
        console.log(`\n✅ Clear winner (${ratio.toFixed(2)}x > 1.2x): [${candidates[0].id}/${candidates[0].value}]`);
        return candidates[0];
    }
    
    console.log(`\n⚠️  Ambiguous (top score ${topScore} vs next ${secondScore}, ratio: ${ratio.toFixed(2)}x < 1.2x)`);
    return null;
};

// Test cases
console.log('═══════════════════════════════════════════════════════════════');
console.log('🧪 Testing Smart Fuzzy Search Algorithm');
console.log('═══════════════════════════════════════════════════════════════');

const testCases = [
    "blob storage repository configuration WithBlobStorage",
    "blob",
    "repository entityframework",
    "authentication social blazor",
    "cosmos sql azure",
    "background job cron",
    "content repository azure"
];

for (const testQuery of testCases) {
    findBestMatch(testQuery);
    console.log('───────────────────────────────────────────────────────────────');
}

console.log('\n✅ Test complete!');
