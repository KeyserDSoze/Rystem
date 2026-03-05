import { useState, useEffect } from 'react'
import { useSearchParams } from 'react-router-dom'
import { AlertCircle, Loader2, Code2, FileText, MessageSquare, ExternalLink, Share2, Copy, Check, Play, ChevronRight } from 'lucide-react'
import MarkdownViewer from '../components/MarkdownViewer'

interface InputSchemaField {
  type: string
  description: string
  required: boolean
}

interface McpItem {
  name: string
  path: string
  title?: string
  description?: string
  inputSchema?: Record<string, InputSchemaField>
}

interface McpManifest {
  name: string
  version: string
  description: string
  tools: McpItem[]
  resources: McpItem[]
  prompts: McpItem[]
}

const MCP_ENDPOINT = (import.meta as any).env?.VITE_MCP_ENDPOINT ?? '/mcp'

export default function McpPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const [manifest, setManifest] = useState<McpManifest | null>(null)
  const [selectedItem, setSelectedItem] = useState<{ type: string; item: McpItem } | null>(null)
  const [itemContent, setItemContent] = useState<string>('')
  const [loading, setLoading] = useState(true)
  const [contentLoading, setContentLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [copied, setCopied] = useState(false)
  // Playground
  const [activeTab, setActiveTab] = useState<'description' | 'playground'>('description')
  const [playgroundArgs, setPlaygroundArgs] = useState<Record<string, string>>({})
  const [playgroundRunning, setPlaygroundRunning] = useState(false)
  const [playgroundResult, setPlaygroundResult] = useState<{ content: string; isError: boolean } | null>(null)

  // Load MCP manifest
  useEffect(() => {
    fetch('/mcp-manifest.json')
      .then((res) => res.json())
      .then((data) => {
        setManifest(data)
        setLoading(false)
      })
      .catch((err) => {
        console.error('Failed to load MCP manifest:', err)
        setError('Failed to load MCP manifest')
        setLoading(false)
      })
  }, [])

  // Load item from URL parameters on mount, or show guide by default
  useEffect(() => {
    if (!manifest) return

    const type = searchParams.get('type')
    const name = searchParams.get('name')

    if (type && name) {
      let items: McpItem[] = []
      switch (type) {
        case 'tool':
          items = manifest.tools
          break
        case 'resource':
          items = manifest.resources
          break
        case 'prompt':
          items = manifest.prompts
          break
      }

      const item = items.find((i) => i.name === name)
      if (item) {
        loadItem(type, item)
      }
    } else {
      // No URL params, load the Getting Started guide by default
      loadGettingStartedGuide()
    }
  }, [manifest, searchParams])

  // Load Getting Started guide
  const loadGettingStartedGuide = () => {
    setSelectedItem({ 
      type: 'guide', 
      item: { 
        name: 'getting-started',
        path: '/MCP-SERVER.md',
        title: 'MCP Server Guide',
        description: 'How to use Rystem MCP Server with AI tools'
      }
    })
    setContentLoading(true)
    fetch('/MCP-SERVER.md')
      .then((res) => res.arrayBuffer())
      .then((buffer) => {
        const decoder = new TextDecoder('utf-8')
        const text = decoder.decode(buffer)
        setItemContent(text)
        setContentLoading(false)
      })
      .catch((err) => {
        console.error('Failed to load guide:', err)
        setItemContent('# Error\n\nFailed to load getting started guide')
        setContentLoading(false)
      })
  }

  // Load specific MCP item content
  const loadItem = (type: string, item: McpItem) => {
    setSelectedItem({ type, item })
    setSearchParams({ type, name: item.name })
    setActiveTab('description')
    setPlaygroundResult(null)
    setPlaygroundArgs({})

    // Dynamic tools have no file path — show description directly
    if (!item.path) {
      setItemContent(item.description ?? `# ${item.title ?? item.name}\n\nNo description available.`)
      return
    }

    setContentLoading(true)
    fetch(item.path, { headers: { Accept: 'text/markdown; charset=utf-8' } })
      .then((res) => res.arrayBuffer())
      .then((buffer) => {
        setItemContent(new TextDecoder('utf-8').decode(buffer))
        setContentLoading(false)
      })
      .catch(() => {
        setItemContent(`# Error\n\nFailed to load content for ${item.name}`)
        setContentLoading(false)
      })
  }

  // Call the live MCP server from the playground
  const runPlayground = async () => {
    if (!selectedItem) return
    setPlaygroundRunning(true)
    setPlaygroundResult(null)
    const args: Record<string, string> = {}
    for (const [k, v] of Object.entries(playgroundArgs)) {
      if (v.trim()) args[k] = v.trim()
    }
    try {
      const res = await fetch(MCP_ENDPOINT, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          jsonrpc: '2.0',
          id: Date.now(),
          method: 'tools/call',
          params: { name: selectedItem.item.name, arguments: args },
        }),
      })
      const json = await res.json()
      const toolResult = json?.result
      const isError = toolResult?.isError === true
      const text = (toolResult?.content ?? [])
        .filter((c: { type: string }) => c.type === 'text')
        .map((c: { text: string }) => c.text)
        .join('\n\n') || JSON.stringify(json, null, 2)
      setPlaygroundResult({ content: text, isError })
    } catch (err) {
      setPlaygroundResult({ content: `❌ Fetch error: ${err}`, isError: true })
    } finally {
      setPlaygroundRunning(false)
    }
  }

  const hasPlayground =
    selectedItem?.type === 'tool' &&
    !!selectedItem.item.inputSchema &&
    Object.keys(selectedItem.item.inputSchema).length > 0

  // Copy link to clipboard
  const copyLink = () => {
    const url = window.location.href
    navigator.clipboard.writeText(url).then(() => {
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    })
  }

  // Share on WhatsApp
  const shareWhatsApp = () => {
    const url = window.location.href
    const text = selectedItem ? `Check out this ${selectedItem.type}: ${selectedItem.item.title || selectedItem.item.name}` : 'Rystem MCP Tools'
    const whatsappUrl = `https://wa.me/?text=${encodeURIComponent(text + ' - ' + url)}`
    window.open(whatsappUrl, '_blank')
  }

  const renderItemCard = (type: string, item: McpItem, Icon: React.ComponentType<{ className?: string }>) => {
    const isSelected = selectedItem?.item.name === item.name
    return (
      <button
        key={item.name}
        onClick={() => loadItem(type, item)}
        className={`w-full text-left p-4 rounded-lg border transition-colors ${
          isSelected
            ? 'bg-primary-50 border-primary-300 dark:bg-primary-900 dark:border-primary-700'
            : 'bg-white border-gray-200 hover:border-primary-300 dark:bg-gray-800 dark:border-gray-700 dark:hover:border-primary-700'
        }`}
      >
        <div className="flex items-start space-x-3">
          <Icon className={`w-5 h-5 mt-0.5 flex-shrink-0 ${isSelected ? 'text-primary-600' : 'text-gray-500'}`} />
          <div className="flex-1 min-w-0">
            <h3 className="text-sm font-semibold text-gray-900 dark:text-white truncate">
              {item.title || item.name}
            </h3>
            {item.description && (
              <p className="mt-1 text-xs text-gray-600 dark:text-gray-400 line-clamp-2">
                {item.description.split('\n')[0].replace(/\*\*/g, '').substring(0, 120)}
              </p>
            )}
          </div>
        </div>
      </button>
    )
  }

  if (loading) {
    return (
      <div className="flex h-screen items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-primary-600" />
      </div>
    )
  }

  if (error || !manifest) {
    return (
      <div className="flex h-screen items-center justify-center">
        <div className="text-center">
          <AlertCircle className="mx-auto h-12 w-12 text-red-500" />
          <h3 className="mt-2 text-lg font-semibold text-gray-900 dark:text-white">Error</h3>
          <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">{error || 'Failed to load MCP data'}</p>
        </div>
      </div>
    )
  }

  return (
    <div className="bg-gray-50 dark:bg-gray-900 h-full overflow-y-auto">
      <div className="mx-auto max-w-7xl px-6 py-8">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white">
            Model Context Protocol (MCP)
          </h1>
          <p className="mt-2 text-lg text-gray-600 dark:text-gray-300">
            {manifest.description}
          </p>
          <div className="mt-4 flex items-center space-x-4">
            <span className="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium bg-primary-100 text-primary-800 dark:bg-primary-900 dark:text-primary-200">
              Version {manifest.version}
            </span>
            <a
              href="/mcp-manifest.json"
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex items-center space-x-1 text-sm text-primary-600 hover:text-primary-700 dark:text-primary-400"
            >
              <span>View Manifest</span>
              <ExternalLink className="w-4 h-4" />
            </a>
          </div>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          {/* Sidebar */}
          <div className="space-y-6">
            {/* Getting Started Guide */}
            <div>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-3">
                📖 Getting Started
              </h2>
              <button
                onClick={() => {
                  loadGettingStartedGuide()
                  // Clear URL params for guide
                  setSearchParams({})
                }}
                className={`w-full text-left p-4 rounded-lg border transition-colors ${
                  selectedItem?.type === 'guide'
                    ? 'bg-primary-50 border-primary-300 dark:bg-primary-900 dark:border-primary-700'
                    : 'bg-white border-gray-200 hover:border-primary-300 dark:bg-gray-800 dark:border-gray-700 dark:hover:border-primary-700'
                }`}
              >
                <div className="flex items-start space-x-3">
                  <FileText className={`w-5 h-5 mt-0.5 flex-shrink-0 ${
                    selectedItem?.type === 'guide' ? 'text-primary-600' : 'text-gray-500'
                  }`} />
                  <div className="flex-1 min-w-0">
                    <h3 className="text-sm font-semibold text-gray-900 dark:text-white">
                      MCP Server Guide
                    </h3>
                    <p className="mt-1 text-sm text-gray-600 dark:text-gray-400 line-clamp-2">
                      How to use Rystem MCP Server with AI tools (GitHub Copilot, Claude, Cursor)
                    </p>
                  </div>
                </div>
              </button>
            </div>

            {/* Tools */}
            <div>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-3">
                🛠️ Tools ({manifest.tools.length})
              </h2>
              <div className="space-y-2">
                {manifest.tools.map((tool) => renderItemCard('tool', tool, Code2))}
              </div>
            </div>

            {/* Resources */}
            <div>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-3">
                📚 Resources ({manifest.resources.length})
              </h2>
              <div className="space-y-2">
                {manifest.resources.map((resource) => renderItemCard('resource', resource, FileText))}
              </div>
            </div>

            {/* Prompts */}
            <div>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-3">
                💬 Prompts ({manifest.prompts.length})
              </h2>
              <div className="space-y-2">
                {manifest.prompts.map((prompt) => renderItemCard('prompt', prompt, MessageSquare))}
              </div>
            </div>
          </div>

          {/* Content Area */}
          <div className="lg:col-span-2">
            <div className="bg-white dark:bg-gray-800 rounded-lg shadow">
              {/* Toolbar */}
              {selectedItem && (
                <div className="border-b border-gray-200 dark:border-gray-700 px-6 py-3 flex items-center justify-between flex-wrap gap-2">
                  <div className="flex items-center space-x-2">
                    <span className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">{selectedItem.type}</span>
                    <ChevronRight className="w-3 h-3 text-gray-400" />
                    <span className="text-sm font-semibold text-gray-900 dark:text-white">
                      {selectedItem.item.title || selectedItem.item.name}
                    </span>
                  </div>
                  <div className="flex items-center space-x-2">
                    {selectedItem.item.path && (
                      <button
                        onClick={() => window.open(selectedItem.item.path, '_blank')}
                        className="inline-flex items-center space-x-1 px-3 py-1.5 text-sm text-blue-600 dark:text-blue-400 hover:bg-blue-50 dark:hover:bg-blue-900/20 rounded-md transition-colors"
                      >
                        <ExternalLink className="w-4 h-4" />
                        <span>Open File</span>
                      </button>
                    )}
                    <button onClick={copyLink} className="inline-flex items-center space-x-1 px-3 py-1.5 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-md transition-colors">
                      {copied ? <><Check className="w-4 h-4" /><span>Copied!</span></> : <><Copy className="w-4 h-4" /><span>Copy Link</span></>}
                    </button>
                    <button onClick={shareWhatsApp} className="inline-flex items-center space-x-1 px-3 py-1.5 text-sm text-green-600 dark:text-green-400 hover:bg-green-50 dark:hover:bg-green-900/20 rounded-md transition-colors">
                      <Share2 className="w-4 h-4" /><span>WhatsApp</span>
                    </button>
                  </div>
                </div>
              )}

              {/* Description / Playground tabs */}
              {hasPlayground && (
                <div className="border-b border-gray-200 dark:border-gray-700 px-6 flex">
                  {(['description', 'playground'] as const).map((tab) => (
                    <button
                      key={tab}
                      onClick={() => setActiveTab(tab)}
                      className={`px-4 py-3 text-sm font-medium border-b-2 transition-colors capitalize ${
                        activeTab === tab
                          ? 'border-primary-500 text-primary-600 dark:text-primary-400'
                          : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200'
                      }`}
                    >
                      {tab === 'description' ? '📄 Description' : '▶ Playground'}
                    </button>
                  ))}
                </div>
              )}

              <div className="p-6">
                {selectedItem ? (
                  <>
                    {/* Description */}
                    {activeTab === 'description' && (
                      contentLoading ? (
                        <div className="flex items-center justify-center py-12">
                          <Loader2 className="h-8 w-8 animate-spin text-primary-600" />
                        </div>
                      ) : (
                        <MarkdownViewer content={itemContent} />
                      )
                    )}

                    {/* Playground */}
                    {activeTab === 'playground' && hasPlayground && selectedItem.item.inputSchema && (
                      <div className="space-y-6">
                        <div>
                          <h3 className="text-base font-semibold text-gray-900 dark:text-white mb-1">
                            Test: <code className="text-primary-600 dark:text-primary-400">{selectedItem.item.name}</code>
                          </h3>
                          <p className="text-xs text-gray-500 dark:text-gray-400">
                            Endpoint: <code className="bg-gray-100 dark:bg-gray-700 px-1 rounded">{MCP_ENDPOINT}</code>
                          </p>
                        </div>

                        {/* Parameter inputs */}
                        <div className="space-y-4">
                          {Object.entries(selectedItem.item.inputSchema).map(([key, field]) => (
                            <div key={key}>
                              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                                {key}
                                {field.required
                                  ? <span className="ml-1 text-red-500">*</span>
                                  : <span className="ml-1 text-gray-400 text-xs">(optional)</span>}
                              </label>
                              <input
                                type="text"
                                placeholder={field.description}
                                value={playgroundArgs[key] ?? ''}
                                onChange={(e) => setPlaygroundArgs((p) => ({ ...p, [key]: e.target.value }))}
                                className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md text-sm bg-white dark:bg-gray-900 text-gray-900 dark:text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-primary-500"
                              />
                              <p className="mt-1 text-xs text-gray-500 dark:text-gray-400">{field.description}</p>
                            </div>
                          ))}
                        </div>

                        <button
                          onClick={runPlayground}
                          disabled={playgroundRunning}
                          className="inline-flex items-center space-x-2 px-5 py-2.5 bg-primary-600 hover:bg-primary-700 disabled:opacity-50 text-white text-sm font-medium rounded-lg transition-colors"
                        >
                          {playgroundRunning
                            ? <><Loader2 className="w-4 h-4 animate-spin" /><span>Running…</span></>
                            : <><Play className="w-4 h-4" /><span>Run Tool</span></>}
                        </button>

                        {playgroundResult && (
                          <div className={`rounded-lg border ${
                            playgroundResult.isError
                              ? 'border-red-300 bg-red-50 dark:border-red-700 dark:bg-red-900/20'
                              : 'border-green-300 bg-green-50 dark:border-green-700 dark:bg-green-900/20'
                          }`}>
                            <div className={`px-4 py-2 border-b text-xs font-semibold rounded-t-lg ${
                              playgroundResult.isError
                                ? 'border-red-300 text-red-700 dark:border-red-700 dark:text-red-400'
                                : 'border-green-300 text-green-700 dark:border-green-700 dark:text-green-400'
                            }`}>
                              {playgroundResult.isError ? '❌ Error' : '✅ Response'}
                            </div>
                            <div className="p-4">
                              <MarkdownViewer content={playgroundResult.content} />
                            </div>
                          </div>
                        )}
                      </div>
                    )}
                  </>
                ) : (
                  <div className="text-center py-12">
                    <Code2 className="mx-auto h-12 w-12 text-gray-400" />
                    <h3 className="mt-2 text-lg font-semibold text-gray-900 dark:text-white">Select an item</h3>
                    <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
                      Choose a tool, resource, or prompt from the sidebar to view its documentation
                    </p>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
