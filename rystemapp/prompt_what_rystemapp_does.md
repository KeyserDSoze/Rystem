# 📘 Rystem Docs & MCP Static Site Generator

## 🎯 Obiettivo

Costruire una **soluzione multiprogetto** (Visual Studio / TypeScript monorepo) che:

1. Legge automaticamente **tutti i file README.md** presenti nelle cartelle dei progetti .NET e TypeScript (ognuno rappresenta un pacchetto o libreria). Il progetto si trova dentro /rystemapp e tutti i progetti sono dentro ../src/ dove troverai tutti i progetti.
2. Costruisce da essi una **documentazione statica interattiva** (SPA React + TypeScript + Vite) che può essere **pubblicata su GitHub Pages**.
3. Include un sistema MCP statico: file Markdown predefiniti che descrivono *tools*, *resources* e *prompts* per GitHub Copilot o altri client MCP, **rilasciato anch’esso come contenuto statico** dentro la stessa app.
4. Tutta la pipeline deve essere **automatizzata**: build, generazione degli alberi di documentazione e deploy.

---

## 🧩 Struttura generale della soluzione

```

/rystemapp/
│
├── /src/                     # Codice React/TypeScript per il sito
│   ├── main.tsx
│   ├── vite-env.d.ts
│   ├── vite.config.ts
│   ├── /components/
│   ├── /pages/
│   ├── /generated/           # Cartella generata automaticamente con README.md
│   │   ├── Rystem.Core.md
│   │   ├── Rystem.Repository.md
│   │   └── ...
│   └── /mcp/                 # File statici MCP
│       ├── manifest.json
│       ├── /tools/
│       │   ├── ddd.md
│       │   ├── repository-setup.md
│       │   └── install-rystem.md
│       ├── /resources/
│       │   ├── background-jobs.md
│       │   └── content-repo.md
│       └── /prompts/
│           ├── auth-flow.md
│           └── service-setup.md
│
├── /packages/                # Le librerie reali (.NET o TS)
│   ├── Rystem.Core/
│   │   └── README.md
│   ├── Rystem.Repository/
│   │   └── README.md
│   ├── Rystem.SocialLogin/
│   │   └── README.md
│   └── ...
│
├── scripts/
│   ├── build-docs.ts         # Scansiona i README e genera la struttura JSON
│   └── build-mcp.ts          # Genera il manifest MCP statico
│
├── package.json
├── tsconfig.json
├── .github/workflows/deploy.yml
└── README.md

````

---

## ⚙️ Descrizione tecnica dei componenti

### 1️⃣ Script `build-docs.ts`
Scansiona ricorsivamente la cartella `/packages/`, trova tutti i `README.md`, e crea:

- Copia ogni README in `/src/generated/` mantenendo la struttura di cartelle originale.
- Crea un file `index.json` contenente l’alberatura di navigazione:

Esempio:
```json
[
  {
    "path": "Rystem.Core/README.md",
    "title": "Rystem.Core",
    "relative": "src/generated/Rystem.Core/README.md"
  },
  {
    "path": "Rystem.Repository/README.md",
    "title": "Rystem.Repository"
  }
]
````

### 2️⃣ Script `build-mcp.ts`

Legge le cartelle `/src/mcp/tools`, `/src/mcp/resources`, `/src/mcp/prompts`
e genera un `manifest.json` statico con struttura:

```json
{
  "name": "rystem-mcp",
  "version": "1.0.0",
  "tools": [
    { "name": "ddd", "path": "/mcp/tools/ddd.md" },
    { "name": "repository-setup", "path": "/mcp/tools/repository-setup.md" }
  ],
  "resources": [
    { "name": "content-repo", "path": "/mcp/resources/content-repo.md" }
  ],
  "prompts": [
    { "name": "auth-flow", "path": "/mcp/prompts/auth-flow.md" }
  ]
}
```

Questi file vengono poi serviti staticamente dal sito su GitHub Pages, in modo che client MCP (Copilot, Cursor, Claude Code, ecc.) possano leggerli.

### 3️⃣ Applicazione React (Vite)

* Legge `src/generated/index.json` per costruire un **menù di navigazione ad albero** basato sulle cartelle dei pacchetti.
* Ogni README viene renderizzato in markdown (con `react-markdown` o `mdx-bundler`).
* Layout pulito e responsive (Tailwind CSS o shadcn/ui).
* Sezione “MCP Docs” che mostra i tool statici (legge il `manifest.json`).

### 4️⃣ Deploy GitHub Pages

Workflow YAML (`.github/workflows/deploy.yml`):

```yaml
name: Deploy Docs
on:
  push:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: 20
      - run: npm ci
      - run: npm run build-docs
      - run: npm run build-mcp
      - run: npm run build
      - uses: peaceiris/actions-gh-pages@v3
        with:
          github_token: ${{ secrets.GITHUB_TOKEN }}
          publish_dir: ./dist
```

---

## 🧠 Linee guida per Copilot

Copilot deve:

1. Creare lo script `build-docs.ts` in Node/TypeScript.
2. Creare lo script `build-mcp.ts` in Node/TypeScript.
3. Configurare un progetto Vite + React + TypeScript.
4. Generare la navigazione dinamica da `/src/generated/index.json`.
5. Aggiungere un componente che mostra i file MCP e ne consente la lettura.
6. Includere Tailwind CSS o shadcn/ui per l’interfaccia.
7. Prevedere un comando npm:

   * `"build-docs": "ts-node scripts/build-docs.ts"`
   * `"build-mcp": "ts-node scripts/build-mcp.ts"`
   * `"build": "vite build"`

---

## 📦 Output finale atteso

* `/dist/` contiene l’app statica HTML/CSS/JS generata da Vite, più i file markdown e JSON per MCP.
* La documentazione è navigabile ad albero.
* Gli utenti o client MCP possono leggere `/mcp/manifest.json` per ottenere tool e risorse statiche.
* Tutto funziona interamente su **GitHub Pages** senza runtime server.

---

## ✨ Esempio di flusso

1. Aggiungo una nuova libreria `.NET` o `.ts` con un `README.md`.
2. Lancio `npm run build-docs`.
3. Il nuovo README appare nel sito come sezione dedicata.
4. Aggiungo un nuovo file markdown in `/src/mcp/tools/`.
5. Lancio `npm run build-mcp`.
6. `manifest.json` viene aggiornato automaticamente e disponibile su `https://keyserdsoze.github.io/rystem/manifest.json`.

---

## ✅ Obiettivo finale

Un’unica soluzione che:

* aggrega documentazione da tutti i README di progetto;
* genera un sito statico React + Vite + Tailwind CSS;
* esporta un MCP statico leggibile da GitHub Copilot;
* è **100 % compatibile con GitHub Pages** (nessun server richiesto).
