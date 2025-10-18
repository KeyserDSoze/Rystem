import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import rehypeHighlight from 'rehype-highlight'
import rehypeSlug from 'rehype-slug'
import { useEffect } from 'react'
import { useLocation } from 'react-router-dom'
import 'highlight.js/styles/github-dark.css'

interface MarkdownViewerProps {
  content: string
}

export default function MarkdownViewer({ content }: MarkdownViewerProps) {
  const location = useLocation()

  // Scroll to hash on mount and when hash changes
  useEffect(() => {
    if (location.hash) {
      const id = location.hash.substring(1) // Remove the '#'
      const element = document.getElementById(id)
      if (element) {
        // Small delay to ensure content is rendered
        setTimeout(() => {
          element.scrollIntoView({ behavior: 'smooth', block: 'start' })
        }, 100)
      }
    } else {
      // Scroll to top when no hash
      window.scrollTo({ top: 0, behavior: 'smooth' })
    }
  }, [location.hash, content])

  return (
    <div className="markdown-content prose prose-slate dark:prose-invert max-w-none">
      <ReactMarkdown
        remarkPlugins={[remarkGfm]}
        rehypePlugins={[rehypeHighlight, rehypeSlug]}
        components={{
          a: ({ node, ...props }) => {
            const href = props.href || ''
            const isExternal = href.startsWith('http')
            const isHash = href.startsWith('#')
            
            if (isHash) {
              // Handle internal hash links
              return (
                <a
                  {...props}
                  onClick={(e) => {
                    e.preventDefault()
                    const id = href.substring(1)
                    const element = document.getElementById(id)
                    if (element) {
                      element.scrollIntoView({ behavior: 'smooth', block: 'start' })
                      window.history.pushState(null, '', href)
                    }
                  }}
                />
              )
            }
            
            return (
              <a
                {...props}
                target={isExternal ? '_blank' : undefined}
                rel={isExternal ? 'noopener noreferrer' : undefined}
              />
            )
          },
        }}
      >
        {content}
      </ReactMarkdown>
    </div>
  )
}
