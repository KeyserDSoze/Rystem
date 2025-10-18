import { Link } from 'react-router-dom'
import { ChevronRight, Folder, FileText } from 'lucide-react'
import { useState } from 'react'

export interface DocNode {
  id: string
  name: string
  title: string
  path?: string
  children?: DocNode[]
  type: 'category' | 'folder' | 'file'
}

interface SidebarProps {
  docs: DocNode[]
  currentPath?: string
}

function TreeNode({ node, currentPath, level = 0 }: { node: DocNode; currentPath?: string; level?: number }) {
  const [isExpanded, setIsExpanded] = useState(true)
  const hasChildren = node.children && node.children.length > 0
  const isActive = currentPath === node.path

  if (node.type === 'file' && node.path) {
    return (
      <div>
        <Link
          to={`/docs/${encodeURIComponent(node.path)}`}
          className={`flex items-center space-x-2 px-3 py-2 text-sm rounded-md transition-colors ${
            isActive
              ? 'bg-primary-100 text-primary-700 dark:bg-primary-900 dark:text-primary-300'
              : 'text-gray-700 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-700'
          }`}
          style={{ paddingLeft: `${level * 12 + 12}px` }}
        >
          <FileText className="w-3 h-3 flex-shrink-0" />
          <span className="truncate">{node.title}</span>
        </Link>
        {hasChildren && isExpanded && (
          <div className="mt-1 space-y-1">
            {node.children!.map((child) => (
              <TreeNode key={child.id} node={child} currentPath={currentPath} level={level + 1} />
            ))}
          </div>
        )}
      </div>
    )
  }

  return (
    <div>
      <button
        onClick={() => setIsExpanded(!isExpanded)}
        className="w-full flex items-center justify-between px-3 py-2 text-sm font-medium text-gray-900 dark:text-white hover:bg-gray-100 dark:hover:bg-gray-700 rounded-md transition-colors"
        style={{ paddingLeft: `${level * 12 + 12}px` }}
      >
        <div className="flex items-center space-x-2">
          <Folder className="w-4 h-4 flex-shrink-0" />
          <span>{node.title}</span>
        </div>
        <ChevronRight
          className={`w-4 h-4 transition-transform flex-shrink-0 ${
            isExpanded ? 'transform rotate-90' : ''
          }`}
        />
      </button>
      {isExpanded && hasChildren && (
        <div className="mt-1 space-y-1">
          {node.children!.map((child) => (
            <TreeNode key={child.id} node={child} currentPath={currentPath} level={level + 1} />
          ))}
        </div>
      )}
    </div>
  )
}

export default function Sidebar({ docs, currentPath }: SidebarProps) {
  return (
    <aside className="w-64 bg-gray-50 dark:bg-gray-800 border-r border-gray-200 dark:border-gray-700 overflow-y-auto h-full">
      <div className="p-4">
        <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
          Documentation
        </h2>
        <nav className="space-y-1">
          {docs.map((node) => (
            <TreeNode key={node.id} node={node} currentPath={currentPath} />
          ))}
        </nav>
      </div>
    </aside>
  )
}
