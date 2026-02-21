# 🎮 Rystem PlayFramework Client

Modern React 18 + TypeScript 5 + Vite client for Rystem PlayFramework

[![React](https://img.shields.io/badge/React-18.3.1-blue?logo=react)](https://react.dev)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.7.3-blue?logo=typescript)](https://www.typescriptlang.org/)
[![Vite](https://img.shields.io/badge/Vite-6.1.6-purple?logo=vite)](https://vitejs.dev)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)

## ✨ Features

- ⚡ **Lightning Fast** - Vite dev server (1-2s startup)
- 📦 **Optimized Builds** - 40% smaller bundle than CRA
- 🔥 **Hot Module Replacement** - Instant updates
- 🧪 **Modern Testing** - Vitest + UI
- 📝 **Type Safe** - Full TypeScript strict mode
- 🎨 **Code Quality** - ESLint + Prettier
- 🚀 **Production Ready** - Optimized builds with code splitting

## 🚀 Quick Start

### Prerequisites
- Node.js 18+ 
- npm or yarn

### Installation

```bash
# Clone and setup
git clone https://github.com/KeyserDSoze/Rystem.git
cd src/AI/Rystem.PlayFramework.Client

# Install dependencies
npm install

# Start dev server
npm run dev
```

The app will open at `http://localhost:3000`

## 📜 Available Scripts

```bash
# Development
npm run dev          # Start dev server with HMR
npm run build        # Build for production
npm run preview      # Preview production build locally

# Testing
npm run test         # Run tests in watch mode
npm run test:ui      # Interactive test UI

# Code Quality
npm run lint         # Check for linting errors
npm run lint:fix     # Auto-fix linting errors
npm run format       # Format code with Prettier
```

## 📁 Project Structure

```
src/
├── components/           # React components
├── hooks/               # Custom React hooks
├── services/            # API & business logic
│   └── api.ts          # HTTP client setup
├── types/              # TypeScript types
├── utils/              # Utility functions
├── App.tsx             # Root component
├── App.css             # App styles
├── index.tsx           # Entry point
├── index.css           # Global styles
├── env.d.ts            # Environment types
└── rystem/             # PlayFramework client library

public/
├── index.html          # HTML template (Vite entry)
└── ...                 # Static assets

vite.config.ts         # Vite configuration
tsconfig.json          # TypeScript configuration
vitest.config.ts       # Test configuration
.eslintrc.json         # ESLint rules
.prettierrc.json       # Prettier rules
```

## 🔧 Configuration

### Environment Variables

Create `.env.local`:
```env
VITE_API_URL=http://localhost:5000
VITE_DEBUG=false
```

Access in code:
```typescript
const apiUrl = import.meta.env.VITE_API_URL
```

### API Proxy

The dev server proxies `/api/*` to your backend. Configure in `vite.config.ts`:

```typescript
proxy: {
  '/api': {
    target: 'http://localhost:5000',  // Your .NET backend
    changeOrigin: true,
  },
}
```

## 🧪 Testing

### Run Tests
```bash
npm run test
```

### Test UI
```bash
npm run test:ui
```

### Coverage
```bash
npm run test -- --coverage
```

## 🚢 Deployment

### Build for Production
```bash
npm run build
```

This generates optimized files in `dist/` folder.

### Deploy to Netlify/Vercel
1. Push to GitHub
2. Connect repository to Netlify/Vercel
3. Set build command: `npm run build`
4. Set publish directory: `dist`

### Docker

```dockerfile
FROM node:18-alpine
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build
EXPOSE 3000
CMD ["npm", "run", "preview"]
```

## 📊 Performance

| Metric | react-scripts | Vite |
|--------|--------------|------|
| Dev Start | 8-10s | 1-2s |
| HMR | 5-8s | <1s |
| Build | 60-90s | 15-25s |
| Bundle | 150kb | 95-110kb |

## 🔗 Backend Integration

This client connects to the Rystem PlayFramework .NET backend.

### API Configuration
```typescript
// In src/services/api.ts
const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000',
})

// Auto-add Bearer token
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('auth_token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})
```

## 📚 Documentation

- [Modernization Guide](./MODERNIZATION.md) - Migration from react-scripts to Vite
- [Development Guide](./DEVELOPMENT.md) - Best practices and patterns
- [Vite Docs](https://vitejs.dev/)
- [React Docs](https://react.dev/)
- [TypeScript Docs](https://www.typescriptlang.org/docs/)

## 🐛 Troubleshooting

### Dependencies conflict
```bash
rm -rf node_modules package-lock.json
npm install
```

### Port 3000 already in use
```bash
npm run dev -- --port 3001
```

### TypeScript errors
```bash
npx tsc --noEmit
```

### Linting issues
```bash
npm run lint:fix
npm run format
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🔗 Links

- [Rystem Repository](https://github.com/KeyserDSoze/Rystem)
- [PlayFramework Documentation](https://rystem.net)
- [Issues](https://github.com/KeyserDSoze/Rystem/issues)

---

**Built with ❤️ using React, TypeScript, and Vite**
