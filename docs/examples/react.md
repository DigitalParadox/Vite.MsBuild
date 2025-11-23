---
layout: default
title: React Example
parent: Examples
nav_order: 2
---

# React with ViteKit
{: .no_toc }

Complete example using React, TypeScript, and Fast Refresh.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Project Setup

### 1. Create Vite + React Project

```bash
npm create vite@latest . -- --template react-ts
npm install
```

### 2. Add ViteKit

```bash
dotnet add package ViteKit.Msbuild
```

---

## Configuration

### vite.config.ts

```typescript
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  build: {
    outDir: 'wwwroot',
    emptyOutDir: true,
    manifest: true
  },
  server: {
    port: 5173,
    strictPort: true
  }
})
```

### MyReactApp.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="ViteKit.Msbuild" Version="*" />
  </ItemGroup>
</Project>
```

---

## Sample Component

### src/App.tsx

```tsx
import { useState } from 'react'
import './App.css'

function App() {
  const [count, setCount] = useState(0)

  return (
    <div className="App">
      <h1>Vite + React + ASP.NET Core</h1>
      <div className="card">
        <button onClick={() => setCount((count) => count + 1)}>
          count is {count}
        </button>
      </div>
    </div>
  )
}

export default App
```

---

## Build & Run

```bash
# Production build
dotnet build --configuration Release

# Run
dotnet run --configuration Release
```

---

## Hot Module Replacement

In development, use Vite's dev server:

```bash
# Terminal 1
dotnet watch run

# Terminal 2
npm run dev
```

Then point your app to `http://localhost:5173` for HMR.

---

## Production Optimization

Vite automatically:
- ✅ Tree-shakes unused code
- ✅ Minifies JS/CSS
- ✅ Code-splits on dynamic imports
- ✅ Generates optimized chunks

Example with React.lazy:

```tsx
import { lazy, Suspense } from 'react'

const Dashboard = lazy(() => import('./Dashboard'))

function App() {
  return (
    <Suspense fallback={<div>Loading...</div>}>
      <Dashboard />
    </Suspense>
  )
}
```

Vite creates separate chunks automatically!
