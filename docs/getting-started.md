---
layout: default
title: Getting Started
nav_order: 2
has_children: false
---

# Getting Started
{: .no_toc }

Get up and running with ViteKit.Msbuild in minutes.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Prerequisites

Before you begin, ensure you have:

- **.NET 8.0 or later** installed
- **Node.js 18+** installed
- An **ASP.NET Core project** with Vite

{: .note }
If you don't have a Vite project yet, run `npm create vite@latest` in your project root.

---

## Installation

### Step 1: Add the NuGet package

```bash
dotnet add package ViteKit.Msbuild
```

Or add to your `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="ViteKit.Msbuild" Version="*" />
</ItemGroup>
```

### Step 2: Build your project

```bash
dotnet build
```

That's it! ViteKit automatically:
- ✅ Detects your Vite configuration
- ✅ Finds your package manager (npm/yarn/pnpm/bun)
- ✅ Installs dependencies if needed
- ✅ Runs Vite build


---

## Project Structure

ViteKit works with standard Vite project structures:

### Default Structure
```
MyProject/
├── package.json          # Vite dependencies
├── vite.config.ts        # Vite configuration
├── src/                  # Your frontend code
│   └── main.ts
├── wwwroot/              # ASP.NET Core static files
├── Program.cs            # ASP.NET Core app
└── MyProject.csproj      # Includes ViteKit.Msbuild
```

### Monorepo Structure
```
Solution/
├── package.json          # Shared dependencies
├── vite.config.ts        # Shared config
└── src/
    ├── WebApp/
    │   ├── WebApp.csproj
    │   └── Program.cs
    └── AdminApp/
        ├── AdminApp.csproj
        └── Program.cs
```

Both work automatically!

---

## Verify Installation

After building, check that:

1. **Vite outputs exist** in `wwwroot/`:
   ```
   wwwroot/
   ├── assets/
   │   ├── main-*.js
   │   └── main-*.css
   └── .vite/
       └── manifest.json
   ```

2. **Build logs show Vite execution**:
   ```
   [OK] Vite build completed successfully
   [BUILD] Output: wwwroot/assets
   ```

---

## Next Steps

Now that ViteKit is installed:

- [Configuration](configuration) - Customize build behavior
- [Examples](examples) - See framework-specific examples
- [Troubleshooting](troubleshooting) - Common issues and solutions

---

## Quick Configuration

Want to customize? Add properties to your `.csproj`:

```xml
<PropertyGroup>
  <!-- Custom output directory -->
  <ViteOutputDir>wwwroot/dist</ViteOutputDir>
  
  <!-- Custom Vite mode -->
  <ViteMode>staging</ViteMode>
  
  <!-- Use pnpm instead of auto-detect -->
  <PackageManager>pnpm</PackageManager>
</PropertyGroup>
```

See [Configuration](configuration) for all available options.
