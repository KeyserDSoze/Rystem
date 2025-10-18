import { useState, useEffect } from 'react'
import { AlertCircle, Loader2, Code2, FileText, MessageSquare, ExternalLink } from 'lucide-react'
import MarkdownViewer from '../components/MarkdownViewer'

interface McpItem {
  name: string
  path: string
  title?: string
  description?: string
}

interface McpManifest {
  name: string
  version: string
  description: string
  tools: McpItem[]
  resources: McpItem[]
  prompts: McpItem[]
}

export default function McpPage() {
  const [manifest, setManifest] = useState<McpManifest | null>(null)
  const [selectedItem, setSelectedItem] = useState<{ type: string; item: McpItem } | null>(null)
  const [itemContent, setItemContent] = useState<string>('')
  const [loading, setLoading] = useState(true)
  const [contentLoading, setContentLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

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

  // Load specific MCP item content
  const loadItem = (type: string, item: McpItem) => {
    setSelectedItem({ type, item })
    setContentLoading(true)
    fetch(item.path)
      .then((res) => res.text())
      .then((text) => {
        setItemContent(text)
        setContentLoading(false)
      })
      .catch((err) => {
        console.error('Failed to load item content:', err)
        setItemContent(`# Error\n\nFailed to load content for ${item.name}`)
        setContentLoading(false)
      })
  }

  const renderItemCard = (type: string, item: McpItem, icon: any) => {
    const Icon = icon
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
          <Icon className={`w-5 h-5 mt-0.5 flex-shrink-0 ${
            isSelected ? 'text-primary-600' : 'text-gray-500'
          }`} />
          <div className="flex-1 min-w-0">
            <h3 className="text-sm font-semibold text-gray-900 dark:text-white truncate">
              {item.title || item.name}
            </h3>
            {item.description && (
              <p className="mt-1 text-sm text-gray-600 dark:text-gray-400 line-clamp-2">
                {item.description}
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
            <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
              {selectedItem ? (
                <>
                  {contentLoading ? (
                    <div className="flex items-center justify-center py-12">
                      <Loader2 className="h-8 w-8 animate-spin text-primary-600" />
                    </div>
                  ) : (
                    <MarkdownViewer content={itemContent} />
                  )}
                </>
              ) : (
                <div className="text-center py-12">
                  <Code2 className="mx-auto h-12 w-12 text-gray-400" />
                  <h3 className="mt-2 text-lg font-semibold text-gray-900 dark:text-white">
                    Select an item
                  </h3>
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
  )
}
