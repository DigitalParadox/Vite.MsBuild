---
layout: default
title: Svelte + TypeScript
parent: Framework Examples
grand_parent: Examples
nav_order: 3
---

# Svelte + TypeScript Example
{: .fs-9 }

Complete example showing Vite.MsBuild with Svelte and TypeScript.
{: .fs-6 .fw-300 }

## Project Structure

```
SvelteWebApp/
├── SvelteWebApp.csproj
├── package.json
├── vite.config.ts
├── tsconfig.json
├── svelte.config.js
├── src/
│   ├── main.ts
│   ├── App.svelte
│   ├── lib/
│   │   └── Counter.svelte
│   └── app.css
└── wwwroot/
    └── index.html
```

## Files

### SvelteWebApp.csproj

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
    <!-- Svelte-specific configuration -->
    <ViteOutputDir>wwwroot/build</ViteOutputDir>
    <ViteBuildTiming>AfterCSharp</ViteBuildTiming>
  </PropertyGroup>
</Project>
```

### package.json

```json
{
  "name": "svelte-web-app",
  "private": true,
  "version": "0.0.0",
  "type": "module",
  "scripts": {
    "build": "vite build",
    "dev": "vite dev",
    "preview": "vite preview",
    "check": "svelte-kit sync && svelte-check --tsconfig ./tsconfig.json",
    "check:watch": "svelte-kit sync && svelte-check --tsconfig ./tsconfig.json --watch"
  },
  "devDependencies": {
    "@sveltejs/vite-plugin-svelte": "^2.4.0",
    "@tsconfig/svelte": "^5.0.0",
    "svelte": "^4.0.0",
    "svelte-check": "^3.4.0",
    "typescript": "^5.0.0",
    "vite": "^5.0.0"
  }
}
```

### vite.config.ts

```typescript
import { defineConfig } from 'vite'
import { svelte } from '@sveltejs/vite-plugin-svelte'

export default defineConfig({
  plugins: [svelte()],
  build: {
    outDir: 'wwwroot/build',
    manifest: true,
    rollupOptions: {
      input: 'src/main.ts'
    }
  },
  server: {
    port: 5173,
    hmr: {
      clientPort: 5173
    }
  }
})
```

### svelte.config.js

```javascript
import { vitePreprocess } from '@sveltejs/vite-plugin-svelte'

export default {
  preprocess: vitePreprocess(),
}
```

### src/main.ts

```typescript
import './app.css'
import App from './App.svelte'

const app = new App({
  target: document.getElementById('app')!,
})

export default app
```

### src/App.svelte

```svelte
<script lang="ts">
  import Counter from './lib/Counter.svelte'
  import { onMount } from 'svelte'

  let buildMode: string = 'unknown'
  
  onMount(() => {
    // Access Vite environment variables
    buildMode = import.meta.env.MODE
  })
</script>

<main>
  <div>
    <img src="/svelte.svg" class="logo svelte" alt="Svelte Logo" />
  </div>
  
  <h1>Svelte + ASP.NET Core + Vite</h1>
  
  <div class="card">
    <Counter />
  </div>
  
  <p>
    Build mode: <span class="mode {buildMode}">{buildMode}</span>
  </p>
  
  <p class="instructions">
    Check out <a href="https://kit.svelte.dev/docs" target="_blank" rel="noreferrer">SvelteKit</a>, 
    the official Svelte app framework powered by Vite!
  </p>
</main>

<style>
  .logo {
    height: 6em;
    padding: 1.5em;
    will-change: filter;
    transition: filter 300ms;
  }
  
  .logo:hover {
    filter: drop-shadow(0 0 2em #646cffaa);
  }
  
  .logo.svelte:hover {
    filter: drop-shadow(0 0 2em #ff3e00aa);
  }
  
  .card {
    padding: 2em;
  }
  
  .instructions {
    color: #888;
    margin-top: 2em;
  }
  
  .mode.development {
    color: #ff3e00;
    font-weight: bold;
  }
  
  .mode.production {
    color: #40b3ff;
    font-weight: bold;
  }
  
  main {
    text-align: center;
    padding: 1em;
    max-width: 240px;
    margin: 0 auto;
  }
  
  h1 {
    color: #ff3e00;
    text-transform: uppercase;
    font-size: 4em;
    font-weight: 100;
  }
  
  @media (min-width: 640px) {
    main {
      max-width: none;
    }
  }
</style>
```

### src/lib/Counter.svelte

```svelte
<script lang="ts">
  let count: number = 0
  
  const increment = (): void => {
    count += 1
  }
</script>

<button on:click={increment}>
  count is {count}
</button>

<style>
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
</style>
```

### src/app.css

```css
:root {
  font-family: Inter, system-ui, Avenir, Helvetica, Arial, sans-serif;
  line-height: 1.5;
  font-weight: 400;

  color-scheme: light dark;
  color: rgba(255, 255, 255, 0.87);
  background-color: #242424;

  font-synthesis: none;
  text-rendering: optimizeLegibility;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  -webkit-text-size-adjust: 100%;
}

a {
  font-weight: 500;
  color: #646cff;
  text-decoration: inherit;
}

a:hover {
  color: #535bf2;
}

body {
  margin: 0;
  display: flex;
  place-items: center;
  min-width: 320px;
  min-height: 100vh;
}

#app {
  max-width: 1280px;
  margin: 0 auto;
  padding: 2rem;
  text-align: center;
}

@media (prefers-color-scheme: light) {
  :root {
    color: #213547;
    background-color: #ffffff;
  }
  
  a:hover {
    color: #747bff;
  }
  
  button {
    background-color: #f9f9f9;
    color: #213547;
  }
}
```

## TypeScript Configuration

### tsconfig.json

```json
{
  "extends": "@tsconfig/svelte/tsconfig.json",
  "compilerOptions": {
    "target": "ESNext",
    "useDefineForClassFields": true,
    "module": "ESNext",
    "resolveJsonModule": true,
    "allowSyntheticDefaultImports": true,
    "moduleResolution": "bundler",
    "allowImportingTsExtensions": true,
    "isolatedModules": true,
    "noEmit": true,
    "strict": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noFallthroughCasesInSwitch": true
  },
  "include": ["src/**/*.ts", "src/**/*.svelte"],
  "references": [{ "path": "./tsconfig.node.json" }]
}
```

## ASP.NET Core Integration

### Views/Home/Index.cshtml

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8" />
    <link rel="icon" type="image/svg+xml" href="/svelte.svg" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Svelte + ASP.NET Core</title>
</head>
<body>
    <div id="app"></div>
    <script type="module" src="~/build/src/main.js"></script>
</body>
</html>
```

## Usage

```bash
# Install Svelte dependencies
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
📁 Collected 4 source files and 4 config files
🎯 Building Vite configuration: vite.config.ts (mode: development)
✅ Svelte components compiled successfully
✅ Vite build completed successfully
```