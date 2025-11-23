---
layout: default
title: Vue Example
parent: Examples
nav_order: 1
---

# Vue 3 with ViteKit
{: .no_toc }

Complete example using Vue 3, Composition API, and TypeScript.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Project Setup

### 1. Create Vite + Vue Project

```bash
npm create vite@latest . -- --template vue-ts
npm install
```

### 2. Add ViteKit to ASP.NET Core

```bash
dotnet add package ViteKit.Msbuild
```

---

## File Structure

```
MyVueApp/
├── package.json
├── vite.config.ts
├── index.html
├── src/
│   ├── main.ts
│   ├── App.vue
│   └── components/
│       └── HelloWorld.vue
├── wwwroot/              # Vite outputs here
├── Program.cs
└── MyVueApp.csproj
```

---

## Configuration Files

### vite.config.ts

```typescript
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  build: {
    outDir: 'wwwroot',
    emptyOutDir: true,
    manifest: true,
    rollupOptions: {
      input: {
        main: './src/main.ts'
      }
    }
  }
})
```

### MyVueApp.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="ViteKit.Msbuild" Version="*" />
  </ItemGroup>
</Project>
```

### package.json

```json
{
  "name": "my-vue-app",
  "type": "module",
  "scripts": {
    "dev": "vite",
    "build": "vite build"
  },
  "dependencies": {
    "vue": "^3.4.0"
  },
  "devDependencies": {
    "@vitejs/plugin-vue": "^5.0.0",
    "typescript": "^5.3.0",
    "vite": "^5.0.0",
    "vue-tsc": "^1.8.0"
  }
}
```

---

## ASP.NET Core Integration

### Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseStaticFiles(); // Serves wwwroot

app.MapGet("/", () => Results.Content("""
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <title>Vue App</title>
    </head>
    <body>
        <div id="app"></div>
        <script type="module" src="/assets/main.js"></script>
    </body>
    </html>
    """, "text/html"));

app.Run();
```

---

## Build & Run

### Development
```bash
# Terminal 1: ASP.NET Core
dotnet watch run

# Terminal 2: Vite dev server
npm run dev
```

### Production
```bash
dotnet build --configuration Release
dotnet run --configuration Release
```

---

## Features Demonstrated

✅ **Vue 3 Composition API**
✅ **TypeScript Support**
✅ **Hot Module Replacement** (dev mode)
✅ **Optimized Production Builds**
✅ **Asset Manifest Integration**

---

## Next Steps

- Add [Vue Router](https://router.vuejs.org/)
- Integrate [Pinia](https://pinia.vuejs.org/) for state management
- Use [Vite Manifest](https://vitejs.dev/guide/backend-integration.html) for cache-busted URLs
