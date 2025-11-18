---
layout: default
title: React + TypeScript
parent: Framework Examples
grand_parent: Examples
nav_order: 2
---

# React + TypeScript Example
{: .fs-9 }

Complete example showing Vite.MsBuild with React and TypeScript.
{: .fs-6 .fw-300 }

## Project Structure

```
ReactWebApp/
├── ReactWebApp.csproj
├── package.json
├── vite.config.ts
├── tsconfig.json
├── src/
│   ├── main.tsx
│   ├── App.tsx
│   ├── App.css
│   └── components/
│       └── Counter.tsx
└── wwwroot/
    └── index.html
```

## Files

### ReactWebApp.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Vite.MsBuild" Version="2.0.0" />
  </ItemGroup>

  <PropertyGroup>
    <!-- React-specific configuration -->
    <ViteOutputDir>wwwroot/assets</ViteOutputDir>
    <PackageManager>npm</PackageManager>
  </PropertyGroup>
</Project>
```

### package.json

```json
{
  "name": "react-web-app",
  "private": true,
  "version": "0.0.0",
  "type": "module",
  "scripts": {
    "build": "tsc && vite build",
    "dev": "vite",
    "lint": "eslint . --ext ts,tsx --report-unused-disable-directives --max-warnings 0",
    "preview": "vite preview"
  },
  "dependencies": {
    "react": "^18.2.0",
    "react-dom": "^18.2.0"
  },
  "devDependencies": {
    "@types/react": "^18.2.0",
    "@types/react-dom": "^18.2.0",
    "@typescript-eslint/eslint-plugin": "^6.0.0",
    "@typescript-eslint/parser": "^6.0.0",
    "@vitejs/plugin-react": "^4.0.0",
    "eslint": "^8.45.0",
    "eslint-plugin-react-hooks": "^4.6.0",
    "eslint-plugin-react-refresh": "^0.4.0",
    "typescript": "^5.0.0",
    "vite": "^5.0.0"
  }
}
```

### vite.config.ts

```typescript
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  build: {
    outDir: 'wwwroot/assets',
    manifest: true,
    rollupOptions: {
      input: 'src/main.tsx'
    }
  },
  server: {
    port: 3000,
    hmr: {
      clientPort: 3000
    }
  }
})
```

### src/main.tsx

```tsx
import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App.tsx'
import './index.css'

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
)
```

### src/App.tsx

```tsx
import { useState } from 'react'
import Counter from './components/Counter'
import './App.css'

function App() {
  const [message, setMessage] = useState('React + ASP.NET Core + Vite')

  return (
    <div className="App">
      <div>
        <img src="/react.svg" className="logo react" alt="React logo" />
      </div>
      <h1>{message}</h1>
      <div className="card">
        <Counter />
        <p>
          Edit <code>src/App.tsx</code> and save to test HMR
        </p>
      </div>
      <p className="read-the-docs">
        Build mode: <span className={`mode ${import.meta.env.MODE}`}>
          {import.meta.env.MODE}
        </span>
      </p>
    </div>
  )
}

export default App
```

### src/components/Counter.tsx

```tsx
import { useState } from 'react'

interface CounterProps {
  initialCount?: number
}

function Counter({ initialCount = 0 }: CounterProps) {
  const [count, setCount] = useState(initialCount)

  return (
    <button onClick={() => setCount((count) => count + 1)}>
      count is {count}
    </button>
  )
}

export default Counter
```

### src/App.css

```css
#root {
  max-width: 1280px;
  margin: 0 auto;
  padding: 2rem;
  text-align: center;
}

.logo {
  height: 6em;
  padding: 1.5em;
  will-change: filter;
  transition: filter 300ms;
}

.logo:hover {
  filter: drop-shadow(0 0 2em #646cffaa);
}

.logo.react:hover {
  filter: drop-shadow(0 0 2em #61dafbaa);
}

.card {
  padding: 2em;
}

.read-the-docs {
  color: #888;
}

.mode.development {
  color: #61dafb;
  font-weight: bold;
}

.mode.production {
  color: #ff6b6b;
  font-weight: bold;
}

button {
  border-radius: 8px;
  border: 1px solid transparent;
  padding: 0.6em 1.2em;
  font-size: 1em;
  font-weight: 500;
  font-family: inherit;
  background-color: #1a1a1a;
  color: #ffffff;
  cursor: pointer;
  transition: border-color 0.25s;
}

button:hover {
  border-color: #646cff;
}

button:focus,
button:focus-visible {
  outline: 4px auto -webkit-focus-ring-color;
}
```

## ASP.NET Core Integration

### Views/Home/Index.cshtml

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8" />
    <link rel="icon" type="image/svg+xml" href="/react.svg" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>React + ASP.NET Core</title>
</head>
<body>
    <div id="root"></div>
    <script type="module" src="~/assets/src/main.js"></script>
</body>
</html>
```

## Usage

```bash
# Install React dependencies
npm install

# Development build
dotnet build --configuration Debug

# Production build
dotnet build --configuration Release

# Run the application
dotnet run
```

## Expected Output

```
🔧 Detected package manager: npm from package-lock.json
📁 Collected 5 source files and 3 config files
🎯 Building Vite configuration: vite.config.ts (mode: development)
✅ React components compiled successfully
✅ Vite build completed successfully
```

## TypeScript Configuration

### tsconfig.json

```json
{
  "compilerOptions": {
    "target": "ES2020",
    "useDefineForClassFields": true,
    "lib": ["ES2020", "DOM", "DOM.Iterable"],
    "module": "ESNext",
    "skipLibCheck": true,
    "moduleResolution": "bundler",
    "allowImportingTsExtensions": true,
    "resolveJsonModule": true,
    "isolatedModules": true,
    "noEmit": true,
    "jsx": "react-jsx",
    "strict": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noFallthroughCasesInSwitch": true
  },
  "include": ["src"],
  "references": [{ "path": "./tsconfig.node.json" }]
}
```