---
layout: default
title: Basic Examples
parent: Examples
nav_order: 1
has_children: true
---

# Basic ViteKit.Msbuild Usage
{: .fs-9 }

This example shows the simplest possible setup with zero configuration.
{: .fs-6 .fw-300 }

{: .highlight }
Perfect starting point for first-time users - get ViteKit.Msbuild working in under 5 minutes.

## Project Structure

```
MyWebApp/
├── MyWebApp.csproj
├── package.json
├── vite.config.ts
└── wwwroot/
    └── js/
        └── main.ts
```

## Files

### MyWebApp.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <!-- This is all you need! -->
    <PackageReference Include="ViteKit.Msbuild" Version="2.0.0" />
  </ItemGroup>
</Project>
```

### package.json

```json
{
  "name": "my-web-app",
  "private": true,
  "version": "0.0.0",
  "type": "module",
  "scripts": {
    "build": "vite build"
  },
  "devDependencies": {
    "vite": "^5.0.0",
    "typescript": "^5.0.0"
  }
}
```

### vite.config.ts

```typescript
import { defineConfig } from 'vite'

export default defineConfig({
  build: {
    outDir: 'wwwroot/dist',
    manifest: true,
    rollupOptions: {
      input: 'wwwroot/js/main.ts'
    }
  }
})
```

### wwwroot/js/main.ts

```typescript
console.log('Hello from Vite + ASP.NET Core!')

// Your application code here
document.addEventListener('DOMContentLoaded', () => {
  document.body.innerHTML += '<h1>Vite is working!</h1>'
})
```

## Usage

```bash
# Install dependencies
npm install

# Build the project (includes Vite build)
dotnet build

# Run the project
dotnet run
```

## What Happens

1. **Auto-Detection**: ViteKit.Msbuild automatically detects your `package.json` and `vite.config.ts`
2. **Package Manager**: Automatically detects npm and runs `npm ci` if needed
3. **Build Integration**: Vite build runs automatically during `dotnet build`
4. **Output**: Built assets appear in `wwwroot/dist/`

## Expected Output

```
🔧 Detected package manager: npm from package-lock.json
📁 Collected 1 source files and 1 config files
🎯 Building Vite configuration: vite.config.ts
✅ Vite build completed successfully
```