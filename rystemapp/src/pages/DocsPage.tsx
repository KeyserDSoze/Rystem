import { useParams } from 'react-router-dom'
import { useState, useEffect } from 'react'
import Sidebar, { DocEntry } from '../components/Sidebar'
import MarkdownViewer from '../components/MarkdownViewer'
import { AlertCircle, Loader2 } from 'lucide-react'

export default function DocsPage() {
  const { docPath } = useParams<{ docPath?: string }>()
  const [docs, setDocs] = useState<DocEntry[]>([])
  const [content, setContent] = useState<string>('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  // Load docs index
  useEffect(() => {
    fetch('/Rystem/generated/index.json')
      .then((res) => res.json())
      .then((data) => {
        setDocs(data)
        setLoading(false)
      })
      .catch((err) => {
        console.error('Failed to load docs index:', err)
        setError('Failed to load documentation index')
        setLoading(false)
      })
  }, [])

  // Load specific document
  useEffect(() => {
    if (!docPath) {
      setContent('# Welcome to Rystem Documentation\n\nSelect a package from the sidebar to get started.')
      return
    }

    setLoading(true)
    setError(null)

    const decodedPath = decodeURIComponent(docPath)
    fetch(`/Rystem/generated/${decodedPath}`)
      .then((res) => {
        if (!res.ok) throw new Error('Document not found')
        return res.text()
      })
      .then((text) => {
        setContent(text)
        setLoading(false)
      })
      .catch((err) => {
        console.error('Failed to load document:', err)
        setError('Failed to load document')
        setContent('')
        setLoading(false)
      })
  }, [docPath])

  if (error && docs.length === 0) {
    return (
      <div className="flex h-screen items-center justify-center">
        <div className="text-center">
          <AlertCircle className="mx-auto h-12 w-12 text-red-500" />
          <h3 className="mt-2 text-lg font-semibold text-gray-900 dark:text-white">Error</h3>
          <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">{error}</p>
        </div>
      </div>
    )
  }

  return (
    <div className="flex h-[calc(100vh-4rem)]">
      <Sidebar docs={docs} currentPath={docPath} />
      <div className="flex-1 overflow-y-auto">
        <div className="mx-auto max-w-4xl px-6 py-8">
          {loading ? (
            <div className="flex items-center justify-center py-12">
              <Loader2 className="h-8 w-8 animate-spin text-primary-600" />
            </div>
          ) : error ? (
            <div className="text-center py-12">
              <AlertCircle className="mx-auto h-12 w-12 text-red-500" />
              <h3 className="mt-2 text-lg font-semibold text-gray-900 dark:text-white">Error</h3>
              <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">{error}</p>
            </div>
          ) : (
            <MarkdownViewer content={content} />
          )}
        </div>
      </div>
    </div>
  )
}
