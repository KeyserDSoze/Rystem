import { Routes, Route } from 'react-router-dom'
import Layout from './components/Layout'
import HomePage from './pages/HomePage'
import DocsPage from './pages/DocsPage'
import McpPage from './pages/McpPage'
import A2aPage from './pages/A2aPage'

function App() {
  return (
    <Layout>
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/docs/:docPath?" element={<DocsPage />} />
        <Route path="/mcp" element={<McpPage />} />
        <Route path="/a2a" element={<A2aPage />} />
      </Routes>
    </Layout>
  )
}

export default App
