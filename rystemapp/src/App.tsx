import { Routes, Route } from 'react-router-dom'
import Layout from './components/Layout'
import HomePage from './pages/HomePage'
import DocsPage from './pages/DocsPage'
import McpPage from './pages/McpPage'

function App() {
  return (
    <Layout>
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/docs/:docPath?" element={<DocsPage />} />
        <Route path="/mcp" element={<McpPage />} />
      </Routes>
    </Layout>
  )
}

export default App
