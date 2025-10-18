import { Link } from 'react-router-dom'
import { ChevronRight, Folder, FileText } from 'lucide-react'
import { useState } from 'react'

export interface DocEntry {
  path: string
  title: string
  relativePath: string
  category?: string
}

interface SidebarProps {
  docs: DocEntry[]
  currentPath?: string
}

export default function Sidebar({ docs, currentPath }: SidebarProps) {
  const [expandedCategories, setExpandedCategories] = useState<Set<string>>(
    new Set(docs.map((doc) => doc.category || 'Other'))
  )

  // Group docs by category
  const docsByCategory = docs.reduce((acc, doc) => {
    const category = doc.category || 'Other'
    if (!acc[category]) {
      acc[category] = []
    }
    acc[category].push(doc)
    return acc
  }, {} as Record<string, DocEntry[]>)

  const toggleCategory = (category: string) => {
    setExpandedCategories((prev) => {
      const next = new Set(prev)
      if (next.has(category)) {
        next.delete(category)
      } else {
        next.add(category)
      }
      return next
    })
  }

  return (
    <aside className="w-64 bg-gray-50 dark:bg-gray-800 border-r border-gray-200 dark:border-gray-700 overflow-y-auto">
      <div className="p-4">
        <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
          Documentation
        </h2>
        <nav className="space-y-1">
          {Object.entries(docsByCategory).map(([category, categoryDocs]) => {
            const isExpanded = expandedCategories.has(category)
            return (
              <div key={category}>
                <button
                  onClick={() => toggleCategory(category)}
                  className="w-full flex items-center justify-between px-3 py-2 text-sm font-medium text-gray-900 dark:text-white hover:bg-gray-100 dark:hover:bg-gray-700 rounded-md transition-colors"
                >
                  <div className="flex items-center space-x-2">
                    <Folder className="w-4 h-4" />
                    <span>{category}</span>
                  </div>
                  <ChevronRight
                    className={`w-4 h-4 transition-transform ${
                      isExpanded ? 'transform rotate-90' : ''
                    }`}
                  />
                </button>
                {isExpanded && (
                  <div className="ml-4 mt-1 space-y-1">
                    {categoryDocs.map((doc) => {
                      const isActive = currentPath === doc.relativePath
                      return (
                        <Link
                          key={doc.path}
                          to={`/docs/${encodeURIComponent(doc.relativePath)}`}
                          className={`flex items-center space-x-2 px-3 py-2 text-sm rounded-md transition-colors ${
                            isActive
                              ? 'bg-primary-100 text-primary-700 dark:bg-primary-900 dark:text-primary-300'
                              : 'text-gray-700 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-700'
                          }`}
                        >
                          <FileText className="w-3 h-3" />
                          <span className="truncate">{doc.title}</span>
                        </Link>
                      )
                    })}
                  </div>
                )}
              </div>
            )
          })}
        </nav>
      </div>
    </aside>
  )
}
