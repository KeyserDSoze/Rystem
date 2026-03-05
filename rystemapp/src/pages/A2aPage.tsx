import { useState, useEffect } from 'react'
import { AlertCircle, Loader2, Code2, ExternalLink, Play, ChevronRight, Copy, Check } from 'lucide-react'
import MarkdownViewer from '../components/MarkdownViewer'

const A2A_ENDPOINT = (import.meta as any).env?.VITE_A2A_ENDPOINT ?? 'https://rystem.cloud/a2a'

// ── Types ─────────────────────────────────────────────────────────────────────

interface A2ASkill {
  id: string
  name: string
  description: string
  tags?: string[]
  inputModes: string[]
  outputModes: string[]
  examples?: string[]
}

interface AgentCard {
  name: string
  description: string
  url: string
  version: string
  documentationUrl: string
  capabilities: { streaming: boolean; pushNotifications: boolean; stateTransitionHistory: boolean }
  skills: A2ASkill[]
}

// Infer input fields from the skill id (based on our skill naming convention)
function inferArgs(skill: A2ASkill): Array<{ key: string; label: string; placeholder: string; required: boolean }> {
  const id = skill.id
  if (id.endsWith('-search')) {
    return [{ key: 'query', label: 'query', placeholder: 'Search keywords (e.g. dependency injection, ai, repository)', required: true }]
  }
  if (id.endsWith('-list')) {
    return [{ key: 'id', label: 'id (optional)', placeholder: 'Category to filter (e.g. auth, rystem, repository) — leave empty for all', required: false }]
  }
  // Main tool: id + value
  return [
    { key: 'id', label: 'id', placeholder: 'Category (e.g. auth, rystem, repository, ddd)', required: true },
    { key: 'value', label: 'value', placeholder: 'Topic (e.g. backgroundjob, social-server)', required: true },
  ]
}

// ── Page ──────────────────────────────────────────────────────────────────────

export default function A2aPage() {
  const [agentCard, setAgentCard] = useState<AgentCard | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [selectedSkill, setSelectedSkill] = useState<A2ASkill | null>(null)
  const [args, setArgs] = useState<Record<string, string>>({})
  const [running, setRunning] = useState(false)
  const [result, setResult] = useState<{ content: string; isError: boolean; raw?: string } | null>(null)
  const [showRaw, setShowRaw] = useState(false)
  const [copied, setCopied] = useState(false)

  useEffect(() => {
    fetch('/.well-known/agent.json')
      .then(r => r.json())
      .then((card: AgentCard) => { setAgentCard(card); setLoading(false) })
      .catch(() => { setError('Failed to load agent card'); setLoading(false) })
  }, [])

  const selectSkill = (skill: A2ASkill) => {
    setSelectedSkill(skill)
    setArgs({})
    setResult(null)
    setShowRaw(false)
  }

  const runSkill = async () => {
    if (!selectedSkill) return
    setRunning(true)
    setResult(null)

    const dataPart = { type: 'data', data: { skill: selectedSkill.id, args } }
    const body = {
      jsonrpc: '2.0',
      id: Date.now(),
      method: 'tasks/send',
      params: {
        id: `task-${Date.now()}`,
        skillId: selectedSkill.id,
        message: {
          role: 'user',
          parts: [dataPart],
        },
      },
    }

    try {
      const res = await fetch(A2A_ENDPOINT, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
        body: JSON.stringify(body),
      })
      const json = await res.json()
      const task = json?.result
      const isError = task?.status?.state === 'failed'

      let text = ''
      if (isError) {
        text = task?.status?.message?.parts?.find((p: { type: string }) => p.type === 'text')?.text ?? JSON.stringify(json, null, 2)
      } else {
        text = task?.artifacts?.[0]?.parts?.find((p: { type: string }) => p.type === 'text')?.text ?? JSON.stringify(json, null, 2)
      }

      setResult({ content: text, isError, raw: JSON.stringify(json, null, 2) })
    } catch (err) {
      setResult({ content: `❌ Fetch error: ${err}`, isError: true })
    } finally {
      setRunning(false)
    }
  }

  const copyExample = (example: string) => {
    navigator.clipboard.writeText(example).then(() => {
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    })
  }

  const argFields = selectedSkill ? inferArgs(selectedSkill) : []

  if (loading) return (
    <div className="flex h-screen items-center justify-center">
      <Loader2 className="h-8 w-8 animate-spin text-primary-600" />
    </div>
  )

  if (error || !agentCard) return (
    <div className="flex h-screen items-center justify-center">
      <div className="text-center">
        <AlertCircle className="mx-auto h-12 w-12 text-red-500" />
        <h3 className="mt-2 text-lg font-semibold text-gray-900 dark:text-white">Error</h3>
        <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">{error}</p>
      </div>
    </div>
  )

  // Group skills by base name
  const skillGroups: Record<string, A2ASkill[]> = {}
  for (const skill of agentCard.skills) {
    const base = skill.id.replace(/-list$/, '').replace(/-search$/, '')
    if (!skillGroups[base]) skillGroups[base] = []
    skillGroups[base].push(skill)
  }

  return (
    <div className="bg-gray-50 dark:bg-gray-900 h-full overflow-y-auto">
      <div className="mx-auto max-w-7xl px-6 py-8">

        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white">
            Agent-to-Agent (A2A)
          </h1>
          <p className="mt-2 text-lg text-gray-600 dark:text-gray-300">
            {agentCard.description}
          </p>
          <div className="mt-4 flex flex-wrap items-center gap-4">
            <span className="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200">
              v{agentCard.version}
            </span>
            <span className="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300">
              {agentCard.skills.length} skills
            </span>
            <a href="/.well-known/agent.json" target="_blank" rel="noopener noreferrer"
              className="inline-flex items-center space-x-1 text-sm text-primary-600 hover:text-primary-700 dark:text-primary-400">
              <span>Agent Card</span>
              <ExternalLink className="w-4 h-4" />
            </a>
            <a href={agentCard.documentationUrl} target="_blank" rel="noopener noreferrer"
              className="inline-flex items-center space-x-1 text-sm text-primary-600 hover:text-primary-700 dark:text-primary-400">
              <span>MCP Docs</span>
              <ExternalLink className="w-4 h-4" />
            </a>
          </div>
          {/* Capabilities */}
          <div className="mt-4 flex gap-3 text-xs text-gray-500 dark:text-gray-400">
            <span>Streaming: {agentCard.capabilities.streaming ? '✅' : '❌'}</span>
            <span>Push notifications: {agentCard.capabilities.pushNotifications ? '✅' : '❌'}</span>
            <span>State history: {agentCard.capabilities.stateTransitionHistory ? '✅' : '❌'}</span>
          </div>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">

          {/* Sidebar — skills grouped */}
          <div className="space-y-6">
            {Object.entries(skillGroups).map(([base, skills]) => (
              <div key={base}>
                <h2 className="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-2 px-1">
                  {base}
                </h2>
                <div className="space-y-2">
                  {skills.map(skill => (
                    <button key={skill.id} onClick={() => selectSkill(skill)}
                      className={`w-full text-left p-3 rounded-lg border transition-colors ${
                        selectedSkill?.id === skill.id
                          ? 'bg-primary-50 border-primary-300 dark:bg-primary-900 dark:border-primary-700'
                          : 'bg-white border-gray-200 hover:border-primary-300 dark:bg-gray-800 dark:border-gray-700 dark:hover:border-primary-700'
                      }`}>
                      <div className="flex items-start space-x-2">
                        <Code2 className={`w-4 h-4 mt-0.5 flex-shrink-0 ${selectedSkill?.id === skill.id ? 'text-primary-600' : 'text-gray-400'}`} />
                        <div className="min-w-0">
                          <p className="text-sm font-medium text-gray-900 dark:text-white truncate">{skill.name}</p>
                          <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5 line-clamp-2">{skill.description.split('\n')[0].substring(0, 100)}</p>
                          {skill.tags && (
                            <div className="flex flex-wrap gap-1 mt-1">
                              {skill.tags.map(t => (
                                <span key={t} className="text-xs px-1.5 py-0.5 bg-gray-100 dark:bg-gray-700 text-gray-500 dark:text-gray-400 rounded">{t}</span>
                              ))}
                            </div>
                          )}
                        </div>
                      </div>
                    </button>
                  ))}
                </div>
              </div>
            ))}
          </div>

          {/* Main content */}
          <div className="lg:col-span-2">
            {!selectedSkill ? (
              <div className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-8 text-center">
                <Code2 className="mx-auto w-12 h-12 text-gray-400 mb-4" />
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white">Select a skill</h3>
                <p className="mt-2 text-sm text-gray-500 dark:text-gray-400">
                  Choose a skill from the sidebar to test it live against the A2A endpoint.
                </p>
                <div className="mt-6 text-left bg-gray-50 dark:bg-gray-900 rounded-lg p-4 text-sm">
                  <p className="font-mono text-xs text-gray-500 dark:text-gray-400 mb-2">Endpoint</p>
                  <code className="text-primary-600 dark:text-primary-400">{A2A_ENDPOINT}</code>
                  <p className="font-mono text-xs text-gray-500 dark:text-gray-400 mt-4 mb-2">Protocol</p>
                  <code className="text-gray-700 dark:text-gray-300">JSON-RPC 2.0 · method: tasks/send</code>
                </div>
              </div>
            ) : (
              <div className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden">
                {/* Breadcrumb */}
                <div className="px-6 py-4 border-b border-gray-200 dark:border-gray-700 flex items-center space-x-2 text-sm text-gray-500 dark:text-gray-400">
                  <span className="font-medium text-gray-700 dark:text-gray-300">SKILL</span>
                  <ChevronRight className="w-4 h-4" />
                  <span className="font-semibold text-gray-900 dark:text-white">{selectedSkill.name}</span>
                  <span className="ml-auto font-mono text-xs text-gray-400">{selectedSkill.id}</span>
                </div>

                <div className="p-6 space-y-6">
                  {/* Description */}
                  <div>
                    <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Description</h3>
                    <p className="text-sm text-gray-600 dark:text-gray-400">{selectedSkill.description.split('\n')[0]}</p>
                    <div className="flex gap-2 mt-2">
                      {selectedSkill.inputModes.map(m => <span key={m} className="text-xs px-2 py-0.5 bg-blue-100 dark:bg-blue-900/40 text-blue-700 dark:text-blue-300 rounded">in: {m}</span>)}
                      {selectedSkill.outputModes.map(m => <span key={m} className="text-xs px-2 py-0.5 bg-green-100 dark:bg-green-900/40 text-green-700 dark:text-green-300 rounded">out: {m}</span>)}
                    </div>
                  </div>

                  {/* Playground */}
                  <div>
                    <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">
                      ▶ Playground
                      <span className="ml-2 text-xs font-normal text-gray-400">{A2A_ENDPOINT}</span>
                    </h3>

                    <div className="space-y-3">
                      {argFields.map(field => (
                        <div key={field.key}>
                          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                            {field.label} {field.required && <span className="text-red-500">*</span>}
                          </label>
                          <input
                            type="text"
                            value={args[field.key] ?? ''}
                            onChange={e => setArgs(prev => ({ ...prev, [field.key]: e.target.value }))}
                            placeholder={field.placeholder}
                            className="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-gray-50 dark:bg-gray-900 text-gray-900 dark:text-gray-100 text-sm placeholder-gray-400 focus:ring-2 focus:ring-primary-500 focus:border-transparent outline-none"
                            onKeyDown={e => { if (e.key === 'Enter') runSkill() }}
                          />
                          <p className="mt-1 text-xs text-gray-400">{field.placeholder}</p>
                        </div>
                      ))}
                    </div>

                    <button onClick={runSkill} disabled={running}
                      className="mt-4 inline-flex items-center space-x-2 px-5 py-2.5 rounded-lg bg-primary-600 hover:bg-primary-700 disabled:opacity-50 text-white text-sm font-semibold transition-colors">
                      {running ? <Loader2 className="w-4 h-4 animate-spin" /> : <Play className="w-4 h-4" />}
                      <span>{running ? 'Running…' : 'Run Skill'}</span>
                    </button>
                  </div>

                  {/* Result */}
                  {result && (
                    <div className={`rounded-lg border ${result.isError ? 'border-red-300 dark:border-red-700 bg-red-50 dark:bg-red-900/20' : 'border-green-300 dark:border-green-700 bg-green-50 dark:bg-green-900/20'}`}>
                      <div className="flex items-center justify-between px-4 py-2 border-b border-inherit">
                        <span className={`text-sm font-semibold ${result.isError ? 'text-red-700 dark:text-red-400' : 'text-green-700 dark:text-green-400'}`}>
                          {result.isError ? '❌ Failed' : '✅ Response'}
                        </span>
                        <div className="flex items-center gap-2">
                          <button onClick={() => setShowRaw(v => !v)}
                            className="text-xs text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200 transition-colors">
                            {showRaw ? 'Formatted' : 'Raw JSON'}
                          </button>
                          {result.raw && (
                            <button onClick={() => copyExample(showRaw ? result.raw! : result.content)}
                              className="text-xs text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200 transition-colors">
                              {copied ? <Check className="w-3.5 h-3.5" /> : <Copy className="w-3.5 h-3.5" />}
                            </button>
                          )}
                        </div>
                      </div>
                      <div className="p-4 max-h-96 overflow-y-auto">
                        {showRaw ? (
                          <pre className="text-xs text-gray-700 dark:text-gray-300 whitespace-pre-wrap">{result.raw}</pre>
                        ) : (
                          <MarkdownViewer content={result.content} />
                        )}
                      </div>
                    </div>
                  )}

                  {/* Examples */}
                  {selectedSkill.examples && selectedSkill.examples.length > 0 && (
                    <div>
                      <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Examples</h3>
                      <div className="space-y-2">
                        {selectedSkill.examples.map((ex, i) => (
                          <div key={i} className="flex items-start justify-between bg-gray-50 dark:bg-gray-900 rounded-lg p-3 text-xs font-mono text-gray-600 dark:text-gray-400">
                            <pre className="whitespace-pre-wrap flex-1">{ex}</pre>
                            <button onClick={() => copyExample(ex)} className="ml-2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 flex-shrink-0">
                              {copied ? <Check className="w-3.5 h-3.5" /> : <Copy className="w-3.5 h-3.5" />}
                            </button>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}

                  {/* Raw request preview */}
                  <div>
                    <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Request payload</h3>
                    <pre className="bg-gray-900 text-green-400 text-xs rounded-lg p-4 overflow-x-auto whitespace-pre-wrap">
{JSON.stringify({
  jsonrpc: '2.0',
  id: '<timestamp>',
  method: 'tasks/send',
  params: {
    id: '<task-id>',
    skillId: selectedSkill.id,
    message: {
      role: 'user',
      parts: [{ type: 'data', data: { skill: selectedSkill.id, args } }]
    }
  }
}, null, 2)}
                    </pre>
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
