import { useState, useEffect } from 'react'
import { Download, BookOpen, AlertCircle, Loader2, ChevronRight, Package, Puzzle, ExternalLink, ChevronDown, Wrench, FileCode2 } from 'lucide-react'

// ── Types ─────────────────────────────────────────────────────────────────────

interface SkillEntry {
  id: string
  name: string
  description: string
  category: string
  tool: string
  docCount: number
  zipFile: string
}

// ── Helpers ────────────────────────────────────────────────────────────────────

function titleCase(s: string) {
  return s.replace(/(^|[-\s])([a-z])/g, (_, sep, c: string) => sep + c.toUpperCase())
}

const CATEGORY_COLORS: Record<string, string> = {
  auth: 'bg-purple-100 text-purple-800 dark:bg-purple-900/30 dark:text-purple-300',
  content: 'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-300',
  ddd: 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300',
  install: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-300',
  repository: 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-300',
  rystem: 'bg-orange-100 text-orange-800 dark:bg-orange-900/30 dark:text-orange-300',
}

function categoryBadge(cat: string) {
  const cls = CATEGORY_COLORS[cat] ?? 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300'
  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${cls}`}>
      {titleCase(cat)}
    </span>
  )
}

// ── Step component ─────────────────────────────────────────────────────────────

function Step({ n, title, children }: { n: number; title: string; children: React.ReactNode }) {
  return (
    <div className="flex gap-4">
      <div className="flex-shrink-0 w-8 h-8 rounded-full bg-indigo-600 text-white flex items-center justify-center text-sm font-bold">
        {n}
      </div>
      <div className="pt-1">
        <p className="font-semibold text-gray-900 dark:text-white mb-1">{title}</p>
        <div className="text-sm text-gray-600 dark:text-gray-400">{children}</div>
      </div>
    </div>
  )
}

// ── Skill card ─────────────────────────────────────────────────────────────────

function SkillCard({ skill }: { skill: SkillEntry }) {
  const [downloading, setDownloading] = useState(false)

  function download() {
    setDownloading(true)
    const a = document.createElement('a')
    a.href = skill.zipFile
    a.download = `${skill.id}.zip`
    a.click()
    setTimeout(() => setDownloading(false), 1500)
  }

  return (
    <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl p-5 flex flex-col gap-3 hover:border-indigo-400 dark:hover:border-indigo-500 transition-colors">
      <div className="flex items-start justify-between gap-3">
        <div className="flex items-center gap-2">
          <Puzzle className="w-4 h-4 text-indigo-500 flex-shrink-0 mt-0.5" />
          <span className="font-semibold text-gray-900 dark:text-white text-sm leading-tight">
            {skill.name}
          </span>
        </div>
        {categoryBadge(skill.category)}
      </div>

      <p className="text-xs text-gray-500 dark:text-gray-400 leading-relaxed line-clamp-3">
        {skill.description}
      </p>

      <div className="flex items-center justify-between mt-auto pt-1">
        <span className="text-xs text-gray-400 dark:text-gray-500 flex items-center gap-1">
          <BookOpen className="w-3 h-3" />
          {skill.docCount} doc{skill.docCount !== 1 ? 's' : ''}
        </span>
        <button
          onClick={download}
          disabled={downloading}
          className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-indigo-600 hover:bg-indigo-700 disabled:opacity-60 text-white text-xs font-medium transition-colors"
        >
          {downloading
            ? <Loader2 className="w-3.5 h-3.5 animate-spin" />
            : <Download className="w-3.5 h-3.5" />}
          Download .zip
        </button>
      </div>
    </div>
  )
}

// ── Page ──────────────────────────────────────────────────────────────────────

export default function SkillsPage() {
  const [skills, setSkills] = useState<SkillEntry[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [activeCategory, setActiveCategory] = useState<string>('all')
  const [guideOpen, setGuideOpen] = useState(false)
  const [createOpen, setCreateOpen] = useState(false)
  const [downloadingAll, setDownloadingAll] = useState(false)

  useEffect(() => {
    fetch('/skills/index.json')
      .then(r => {
        if (!r.ok) throw new Error(`${r.status} ${r.statusText}`)
        return r.json()
      })
      .then((data: SkillEntry[]) => { setSkills(data); setLoading(false) })
      .catch(e => { setError(String(e)); setLoading(false) })
  }, [])

  function downloadAll() {
    setDownloadingAll(true)
    const list = activeCategory === 'all' ? skills : skills.filter(s => s.category === activeCategory)
    let i = 0
    function next() {
      if (i >= list.length) { setDownloadingAll(false); return }
      const skill = list[i++]
      const a = document.createElement('a')
      a.href = skill.zipFile
      a.download = `${skill.id}.zip`
      a.click()
      setTimeout(next, 400)
    }
    next()
  }

  const categories = ['all', ...Array.from(new Set(skills.map(s => s.category))).sort()]
  const filtered = activeCategory === 'all' ? skills : skills.filter(s => s.category === activeCategory)

  return (
    <div className="max-w-6xl mx-auto px-4 py-10 space-y-10">

      {/* Header */}
      <div className="space-y-3">
        <div className="flex items-center gap-3">
          <div className="p-2 bg-indigo-100 dark:bg-indigo-900/30 rounded-xl">
            <Package className="w-6 h-6 text-indigo-600 dark:text-indigo-400" />
          </div>
          <div>
            <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Anthropic Skills</h1>
            <p className="text-sm text-gray-500 dark:text-gray-400">
              Download ready-to-use Skill packages for Claude.ai, Claude Code &amp; the Anthropic API
            </p>
          </div>
        </div>

        <div className="flex flex-wrap gap-2 text-xs">
          <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300">
            <ExternalLink className="w-3 h-3" />
            <a href="https://agentskills.io" target="_blank" rel="noopener noreferrer" className="hover:underline">
              agentskills.io spec
            </a>
          </span>
          <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300">
            <ExternalLink className="w-3 h-3" />
            <a href="https://github.com/anthropics/skills" target="_blank" rel="noopener noreferrer" className="hover:underline">
              anthropics/skills on GitHub
            </a>
          </span>
        </div>
      </div>

      {/* How to install — collapsible */}
      <div className="bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl">
        <button
          onClick={() => setGuideOpen(o => !o)}
          className="w-full flex items-center justify-between px-6 py-4 text-left"
        >
          <h2 className="text-base font-semibold text-gray-900 dark:text-white flex items-center gap-2">
            <ChevronRight className="w-4 h-4 text-indigo-500" />
            How to use a Skill in Claude.ai
          </h2>
          <ChevronDown className={`w-4 h-4 text-gray-400 transition-transform ${guideOpen ? 'rotate-180' : ''}`} />
        </button>
        {guideOpen && (
          <div className="px-6 pb-6 space-y-5">
          <Step n={1} title="Download the .zip">
            Click <strong>Download .zip</strong> on any skill below. Each ZIP contains a single{' '}
            <code className="px-1 py-0.5 bg-gray-200 dark:bg-gray-700 rounded text-xs">SKILL.md</code> file
            with full Rystem documentation for that category.
          </Step>
          <Step n={2} title="Upload to Claude.ai">
            Go to{' '}
            <strong>Customize → Skills</strong> in Claude.ai, click the{' '}
            <strong>+</strong> button, choose <strong>Upload a skill</strong>,
            and select the downloaded ZIP file.
          </Step>
          <Step n={3} title="Enable and use">
            Toggle the skill <strong>On</strong>. Claude will automatically load it when you ask anything about
            that category — e.g. <em>"How do I set up Blob storage in Rystem?"</em>
          </Step>
            <Step n={4} title="Claude Code / API">
              For Claude Code run{' '}
              <code className="px-1 py-0.5 bg-red-100 dark:bg-red-800/40 rounded text-xs font-mono">/plugin marketplace add anthropics/skills</code>,
              or pass the skill folder directly via the Skills API.
            </Step>
          </div>
        )}
      </div>

      {/* Create your own skill — collapsible */}
      <div className="bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl">
        <button
          onClick={() => setCreateOpen(o => !o)}
          className="w-full flex items-center justify-between px-6 py-4 text-left"
        >
          <h2 className="text-base font-semibold text-gray-900 dark:text-white flex items-center gap-2">
            <Wrench className="w-4 h-4 text-indigo-500" />
            Create your own custom Skill
          </h2>
          <ChevronDown className={`w-4 h-4 text-gray-400 transition-transform ${createOpen ? 'rotate-180' : ''}`} />
        </button>
        {createOpen && (
          <div className="px-6 pb-6 space-y-5">
            <p className="text-sm text-gray-600 dark:text-gray-400">
              A Skill is just a folder with a <code className="px-1 py-0.5 bg-gray-200 dark:bg-gray-700 rounded text-xs">SKILL.md</code> file.
              The YAML frontmatter tells Claude <em>when</em> to load it; the Markdown body tells it <em>what</em> to do.
            </p>

            {/* Minimal SKILL.md */}
            <div className="space-y-1">
              <p className="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide flex items-center gap-1">
                <FileCode2 className="w-3.5 h-3.5" /> Minimal SKILL.md
              </p>
              <pre className="bg-gray-900 dark:bg-gray-950 text-green-300 text-xs rounded-lg p-4 overflow-x-auto leading-relaxed">{`---
name: My Skill Name
description: What this skill does and WHEN Claude should use it. Be specific (max 200 chars).
---

# My Skill Name

[Your instructions here — written in plain Markdown]

## Examples
- Example input 1
- Example input 2

## Guidelines
- Keep it focused on one workflow
- Include concrete examples`}</pre>
            </div>

            {/* Package steps */}
            <div className="space-y-3">
              <p className="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">Package &amp; upload</p>
              <div className="space-y-2 text-sm text-gray-600 dark:text-gray-400">
                <div className="flex gap-2"><span className="text-indigo-400 font-mono text-xs mt-0.5">1.</span><span>Create a folder named after your skill, put <code className="px-1 bg-gray-200 dark:bg-gray-700 rounded text-xs">SKILL.md</code> inside it.</span></div>
                <div className="flex gap-2"><span className="text-indigo-400 font-mono text-xs mt-0.5">2.</span><span>ZIP the folder (the ZIP root must contain the folder, not the files directly).</span></div>
                <div className="flex gap-2"><span className="text-indigo-400 font-mono text-xs mt-0.5">3.</span><span>Upload via <strong>Customize → Skills → + → Upload a skill</strong> in Claude.ai.</span></div>
              </div>
            </div>

            {/* Official docs links */}
            <div className="flex flex-wrap gap-2 pt-1">
              <a
                href="https://support.anthropic.com/en/articles/11176823-how-to-create-custom-skills"
                target="_blank" rel="noopener noreferrer"
                className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-600 text-xs font-medium text-gray-700 dark:text-gray-300 hover:border-indigo-400 transition-colors"
              >
                <ExternalLink className="w-3 h-3" /> How to create custom Skills
              </a>
              <a
                href="https://support.anthropic.com/en/articles/11176314-what-are-skills"
                target="_blank" rel="noopener noreferrer"
                className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-600 text-xs font-medium text-gray-700 dark:text-gray-300 hover:border-indigo-400 transition-colors"
              >
                <ExternalLink className="w-3 h-3" /> What are Skills?
              </a>
              <a
                href="https://support.anthropic.com/en/articles/11176537-use-skills-in-claude"
                target="_blank" rel="noopener noreferrer"
                className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-600 text-xs font-medium text-gray-700 dark:text-gray-300 hover:border-indigo-400 transition-colors"
              >
                <ExternalLink className="w-3 h-3" /> Use Skills in Claude
              </a>
              <a
                href="https://agentskills.io"
                target="_blank" rel="noopener noreferrer"
                className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-600 text-xs font-medium text-gray-700 dark:text-gray-300 hover:border-indigo-400 transition-colors"
              >
                <ExternalLink className="w-3 h-3" /> agentskills.io open spec
              </a>
            </div>
          </div>
        )}
      </div>


      <div className="space-y-5">
        <div className="flex items-center justify-between flex-wrap gap-3">
          <h2 className="text-base font-semibold text-gray-900 dark:text-white">
            Available Skills {!loading && <span className="text-gray-400 font-normal">({filtered.length})</span>}
          </h2>

          <div className="flex flex-wrap items-center gap-2">
            {/* Download filtered/all */}
            {!loading && !error && filtered.length > 0 && (
              <button
                onClick={downloadAll}
                disabled={downloadingAll}
                className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-indigo-600 hover:bg-indigo-700 disabled:opacity-60 text-white text-xs font-medium transition-colors"
              >
                {downloadingAll
                  ? <Loader2 className="w-3.5 h-3.5 animate-spin" />
                  : <Download className="w-3.5 h-3.5" />}
                Download {activeCategory === 'all' ? 'all' : titleCase(activeCategory)} ({filtered.length})
              </button>
            )}

            {/* Category filter */}
            {categories.length > 2 && (
              <div className="flex flex-wrap gap-1.5">
                {categories.map(cat => (
                  <button
                    key={cat}
                    onClick={() => setActiveCategory(cat)}
                    className={`px-3 py-1 rounded-full text-xs font-medium transition-colors ${
                      activeCategory === cat
                        ? 'bg-indigo-600 text-white'
                        : 'bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600'
                    }`}
                  >
                    {cat === 'all' ? 'All' : titleCase(cat)}
                  </button>
                ))}
              </div>
            )}
          </div>
        </div>

        {loading && (
          <div className="flex items-center justify-center py-16 text-gray-400">
            <Loader2 className="w-6 h-6 animate-spin mr-3" />
            Loading skills…
          </div>
        )}

        {error && (
          <div className="flex items-center gap-3 p-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-xl text-red-600 dark:text-red-400 text-sm">
            <AlertCircle className="w-5 h-5 flex-shrink-0" />
            <span>
              Could not load skills index. Run{' '}
              <code className="px-1 py-0.5 bg-red-100 dark:bg-red-800/40 rounded text-xs">npm run build-skills</code>{' '}
              first.
            </span>
          </div>
        )}

        {!loading && !error && filtered.length === 0 && (
          <p className="text-center py-10 text-gray-400 text-sm">No skills found for this category.</p>
        )}

        {!loading && !error && (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {filtered.map(skill => (
              <SkillCard key={skill.id} skill={skill} />
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
