/// <reference types="vite/client" />
/// <reference types="vitest/globals" />

declare module '*.css' {
  const content: Record<string, string>
  export default content
}
