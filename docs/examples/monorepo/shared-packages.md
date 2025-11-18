---
layout: default
title: Shared Packages
parent: Monorepo Examples
grand_parent: Examples
nav_order: 1
---

# Monorepo Configuration
{: .fs-9 }

Complete example showing Vite.MsBuild in a monorepo with shared packages and multiple applications.
{: .fs-6 .fw-300 }

## Scenario

Modern monorepo setup with:
- **Shared UI Library** (`@company/ui-components`)
- **Shared Utilities** (`@company/utils`)
- **Main Web Application** (consumes shared packages)
- **Mobile Web App** (consumes shared packages)

## Project Structure

```
MonorepoApp/
├── packages/
│   ├── ui-components/
│   │   ├── package.json
│   │   ├── vite.config.ts
│   │   ├── src/
│   │   │   ├── index.ts
│   │   │   └── components/
│   │   │       ├── Button/
│   │   │       └── Card/
│   │   └── dist/
│   └── utils/
│       ├── package.json
│       ├── vite.config.ts
│       ├── src/
│       │   ├── index.ts
│       │   ├── api/
│       │   └── helpers/
│       └── dist/
├── apps/
│   ├── web/
│   │   ├── WebApp.csproj
│   │   ├── package.json
│   │   ├── vite.config.ts
│   │   └── src/
│   │       └── main.tsx
│   └── mobile/
│       ├── MobileApp.csproj
│       ├── package.json
│       ├── vite.config.ts
│       └── src/
│           └── main.ts
├── package.json              ← Root package.json with workspaces
├── pnpm-workspace.yaml       ← PNPM workspace config
└── turbo.json                ← Turborepo config (optional)
```

## Root Configuration

### package.json (Root)

```json
{
  "name": "@company/monorepo",
  "version": "1.0.0",
  "private": true,
  "type": "module",
  "workspaces": [
    "packages/*",
    "apps/*/wwwroot"
  ],
  "scripts": {
    "build": "turbo build",
    "dev": "turbo dev",
    "build:packages": "turbo build --filter='packages/*'",
    "build:apps": "turbo build --filter='apps/*'"
  },
  "devDependencies": {
    "turbo": "^1.10.0",
    "typescript": "^5.0.0",
    "vite": "^5.0.0"
  },
  "engines": {
    "node": ">=18.0.0",
    "pnpm": ">=8.0.0"
  },
  "packageManager": "pnpm@8.10.0"
}
```

### pnpm-workspace.yaml

```yaml
packages:
  - 'packages/*'
  - 'apps/*/wwwroot'
  
# Optional: shared dependencies
shared-workspace-lockfile: true
link-workspace-packages: true
```

### turbo.json (Optional - for optimized builds)

```json
{
  "$schema": "https://turbo.build/schema.json",
  "pipeline": {
    "build": {
      "dependsOn": ["^build"],
      "outputs": ["dist/**", "wwwroot/**"],
      "env": ["NODE_ENV"]
    },
    "dev": {
      "cache": false,
      "persistent": true
    }
  }
}
```

## Shared Packages

### packages/ui-components/package.json

```json
{
  "name": "@company/ui-components",
  "version": "1.0.0",
  "type": "module",
  "main": "dist/index.js",
  "types": "dist/index.d.ts",
  "exports": {
    ".": {
      "import": "./dist/index.js",
      "types": "./dist/index.d.ts"
    }
  },
  "scripts": {
    "build": "vite build",
    "dev": "vite build --watch"
  },
  "dependencies": {
    "react": "^18.2.0",
    "@company/utils": "workspace:^"
  },
  "devDependencies": {
    "@types/react": "^18.2.0",
    "@vitejs/plugin-react": "^4.0.0",
    "typescript": "^5.0.0",
    "vite": "^5.0.0",
    "vite-plugin-dts": "^3.0.0"
  },
  "peerDependencies": {
    "react": "^18.2.0",
    "react-dom": "^18.2.0"
  }
}
```

### packages/ui-components/vite.config.ts

```typescript
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import dts from 'vite-plugin-dts'
import { resolve } from 'path'

export default defineConfig({
  plugins: [
    react(),
    dts({
      insertTypesEntry: true,
    }),
  ],
  build: {
    lib: {
      entry: resolve(__dirname, 'src/index.ts'),
      name: 'UIComponents',
      formats: ['es', 'cjs'],
      fileName: (format) => `index.${format}.js`,
    },
    rollupOptions: {
      external: ['react', 'react-dom'],
      output: {
        globals: {
          react: 'React',
          'react-dom': 'ReactDOM',
        },
      },
    },
  },
})
```

### packages/utils/package.json

```json
{
  "name": "@company/utils",
  "version": "1.0.0",
  "type": "module",
  "main": "dist/index.js",
  "types": "dist/index.d.ts",
  "exports": {
    ".": {
      "import": "./dist/index.js",
      "types": "./dist/index.d.ts"
    }
  },
  "scripts": {
    "build": "vite build",
    "dev": "vite build --watch"
  },
  "devDependencies": {
    "typescript": "^5.0.0",
    "vite": "^5.0.0",
    "vite-plugin-dts": "^3.0.0"
  }
}
```

### packages/utils/vite.config.ts

```typescript
import { defineConfig } from 'vite'
import dts from 'vite-plugin-dts'
import { resolve } from 'path'

export default defineConfig({
  plugins: [
    dts({
      insertTypesEntry: true,
    }),
  ],
  build: {
    lib: {
      entry: resolve(__dirname, 'src/index.ts'),
      name: 'Utils',
      formats: ['es', 'cjs'],
      fileName: (format) => `index.${format}.js`,
    },
  },
})
```

## Application Configurations

### apps/web/WebApp.csproj

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

  <!-- Monorepo-specific configuration -->
  <PropertyGroup>
    <ViteProjectRoot>wwwroot</ViteProjectRoot>
    <ViteConfigFile>wwwroot/vite.config.ts</ViteConfigFile>
    <ViteOutputDir>wwwroot/dist</ViteOutputDir>
    <PackageManager>pnpm</PackageManager>
  </PropertyGroup>

  <!-- Build dependencies in correct order -->
  <ItemGroup>
    <ViteConfig Include="../../packages/utils/vite.config.ts">
      <BuildId>utils</BuildId>
      <ProjectRoot>../../packages/utils</ProjectRoot>
      <OutputDir>../../packages/utils/dist</OutputDir>
    </ViteConfig>

    <ViteConfig Include="../../packages/ui-components/vite.config.ts">
      <BuildId>ui-components</BuildId>
      <ProjectRoot>../../packages/ui-components</ProjectRoot>
      <OutputDir>../../packages/ui-components/dist</OutputDir>
      <DependsOn>utils</DependsOn>
    </ViteConfig>

    <ViteConfig Include="wwwroot/vite.config.ts">
      <BuildId>web-app</BuildId>
      <ProjectRoot>wwwroot</ProjectRoot>
      <OutputDir>wwwroot/dist</OutputDir>
      <DependsOn>ui-components</DependsOn>
      <LinkDependencies>true</LinkDependencies>
    </ViteConfig>
  </ItemGroup>
</Project>
```

### apps/web/wwwroot/package.json

```json
{
  "name": "@company/web-app",
  "private": true,
  "version": "1.0.0",
  "type": "module",
  "scripts": {
    "build": "vite build",
    "dev": "vite"
  },
  "dependencies": {
    "react": "^18.2.0",
    "react-dom": "^18.2.0",
    "@company/ui-components": "workspace:^",
    "@company/utils": "workspace:^"
  },
  "devDependencies": {
    "@types/react": "^18.2.0",
    "@types/react-dom": "^18.2.0",
    "@vitejs/plugin-react": "^4.0.0",
    "typescript": "^5.0.0",
    "vite": "^5.0.0"
  }
}
```

### apps/web/wwwroot/vite.config.ts

```typescript
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  build: {
    outDir: 'dist',
    manifest: true,
    rollupOptions: {
      input: 'src/main.tsx'
    }
  },
  resolve: {
    alias: {
      '@company/ui-components': '../../packages/ui-components/src',
      '@company/utils': '../../packages/utils/src'
    }
  },
  server: {
    port: 3000
  }
})
```

### apps/web/wwwroot/src/main.tsx

```tsx
import React from 'react'
import ReactDOM from 'react-dom/client'
import { Button, Card } from '@company/ui-components'
import { formatDate, validateEmail } from '@company/utils'
import './App.css'

function App() {
  const handleClick = () => {
    console.log('Button clicked at:', formatDate(new Date()))
  }

  const email = 'user@example.com'
  const isValidEmail = validateEmail(email)

  return (
    <div className="App">
      <Card>
        <h1>Monorepo Web Application</h1>
        <p>Email validation example: {email} is {isValidEmail ? 'valid' : 'invalid'}</p>
        <Button onClick={handleClick}>
          Click me! ({formatDate(new Date())})
        </Button>
      </Card>
    </div>
  )
}

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
)
```

## Mobile App Configuration

### apps/mobile/MobileApp.csproj

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

  <!-- Mobile-specific configuration -->
  <PropertyGroup>
    <ViteProjectRoot>wwwroot</ViteProjectRoot>
    <ViteOutputDir>wwwroot/mobile</ViteOutputDir>
    <PackageManager>pnpm</PackageManager>
    <ViteMode>production</ViteMode>
  </PropertyGroup>

  <!-- Reuse shared packages but build separately -->
  <ItemGroup>
    <ViteConfig Include="wwwroot/vite.config.ts">
      <BuildId>mobile-app</BuildId>
      <DependsOn>utils,ui-components</DependsOn>
      <LinkDependencies>true</LinkDependencies>
    </ViteConfig>
  </ItemGroup>
</Project>
```

## Build Commands

### Development

```bash
# Install all dependencies (from root)
pnpm install

# Build shared packages first
pnpm build:packages

# Build applications (which will also rebuild dependencies if needed)
cd apps/web
dotnet build

cd ../mobile  
dotnet build
```

### Production

```bash
# Clean build everything
pnpm install --frozen-lockfile

# Build with turbo for optimal caching
pnpm build

# Or build specific app
cd apps/web
dotnet build --configuration Release
```

### Individual Package Development

```bash
# Work on UI components with watch mode
cd packages/ui-components
pnpm dev

# In another terminal, work on the web app
cd apps/web/wwwroot
pnpm dev
```

## Expected Build Output

```
🔗 Building monorepo packages in dependency order...

[utils] 🔧 Detected package manager: pnpm from pnpm-lock.yaml
[utils] 🎯 Building Vite library: @company/utils
[utils] ✅ Utils library build completed → dist/

[ui-components] 🔧 Detected package manager: pnpm from pnpm-lock.yaml
[ui-components] 🔗 Linking dependency packages for 'ui-components'
[ui-components] ✅ Linked package '@company/utils' → file:../utils/dist
[ui-components] 🎯 Building Vite library: @company/ui-components  
[ui-components] ✅ UI components library build completed → dist/

[web-app] 🔧 Detected package manager: pnpm from pnpm-lock.yaml
[web-app] 🔗 Linking dependency packages for 'web-app'
[web-app] ✅ Linked package '@company/utils' → file:../../packages/utils/dist
[web-app] ✅ Linked package '@company/ui-components' → file:../../packages/ui-components/dist
[web-app] 🎯 Building Vite configuration: vite.config.ts
[web-app] ✅ Web application build completed → wwwroot/dist/

🎯 Monorepo build completed successfully in 8.7s
```

## Benefits

✅ **Shared Code Reuse** - UI components and utilities shared across apps  
✅ **Type Safety** - Full TypeScript support across package boundaries  
✅ **Optimized Builds** - Only rebuilds changed packages  
✅ **Independent Deployment** - Each app can be deployed separately  
✅ **Consistent Tooling** - Same build process across all packages  
✅ **Hot Module Replacement** - Fast development with HMR across packages