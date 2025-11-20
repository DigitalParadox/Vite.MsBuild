# Vite.MsBuild

> **Framework-agnostic MSBuild integration for Vite in ASP.NET Core projects**

[![NuGet Package](https://img.shields.io/nuget/v/Vite.MsBuild)](https://www.nuget.org/packages/Vite.MsBuild)
[![Build Status](https://github.com/DigitalParadox/Vite.MsBuild/workflows/CI/badge.svg)](https://github.com/DigitalParadox/Vite.MsBuild/actions)
[![Tests](https://img.shields.io/badge/tests-434%20passing-brightgreen)](https://github.com/DigitalParadox/Vite.MsBuild/actions)

**Seamlessly integrate Vite with ASP.NET Core** - Build Vue, React, Svelte, or any Vite-supported framework directly from `dotnet build`.

---

## ✨ Features

- 🎯 **Zero Configuration** - Auto-detects your project structure and package manager
- ⚡ **Incremental Builds** - Only rebuilds when source files actually change  
- 🔄 **Parallel Build Safe** - Prevents npm install conflicts in CI/CD
- 🌍 **Framework Agnostic** - Vue, React, Svelte, Solid, Preact, vanilla JS/TS
- 📦 **Package Manager Smart** - Auto-detects npm, pnpm, yarn, or bun
- 🛡️ **Enterprise Ready** - Robust error handling and comprehensive logging
- 🎛️ **MSBuild Native** - Uses proper MSBuild targets, not hacky scripts

---

## 🚀 Quick Start

### 1. Install the NuGet Package

```bash
dotnet add package Vite.MsBuild
```

### 2. Initialize Your Frontend

Choose your preferred framework:

```bash
# Vue + TypeScript
npm create vite@latest . -- --template vue-ts

# React + TypeScript  
npm create vite@latest . -- --template react-ts

# Svelte + TypeScript
npm create vite@latest . -- --template svelte-ts

# Or any other Vite template
```

### 3. Build

```bash
dotnet build
```

**That's it!** Your Vite assets are now built automatically during MSBuild.

---

## 📋 Requirements

- **.NET 6.0+** (supports .NET 8, .NET 9)
- **Node.js 18+** 
- **Vite 4.0+** or **Vite 5.0+**
- **ASP.NET Core project** (Web, MVC, API, Blazor)

---

## ⚙️ Configuration

### Zero Configuration (Recommended)

Vite.MsBuild works out-of-the-box with sensible defaults:

```xml
<PackageReference Include="Vite.MsBuild" Version="2.0.0" />
<!-- That's it! No configuration needed -->
```

### Custom Configuration (Optional)

Override defaults when needed:

```xml
<PropertyGroup>
  <!-- Disable Vite builds -->
  <EnableViteBuild>false</EnableViteBuild>
  
  <!-- Custom Vite config location -->
  <ViteConfigFile>custom.vite.config.ts</ViteConfigFile>
  
  <!-- Custom output directory -->
  <ViteOutputDir>wwwroot/dist</ViteOutputDir>
  
  <!-- Force specific package manager -->
  <PackageManager>pnpm</PackageManager>
  
  <!-- Override Vite mode -->
  <ViteMode>staging</ViteMode>
  
  <!-- Control build timing -->
  <ViteBuildTiming>AfterCSharp</ViteBuildTiming>
</PropertyGroup>
```

### Advanced File Filtering

```xml
<ItemGroup>
  <!-- Include additional source files -->
  <ViteInputFiles Include="custom/**/*.ts" />
  <ViteInputFiles Include="shared/**/*.vue" />
  
  <!-- Exclude specific patterns -->
  <ViteInputFiles Remove="legacy/**/*" />
  <ViteInputFiles Remove="temp/**/*" />
</ItemGroup>
```

---

## 📦 Package Manager Support

Automatically detects and works with all major package managers:

| Package Manager | Lock File | Install Command | Notes |
|---|---|---|---|
| **npm** | `package-lock.json` | `npm ci` | Default Node.js package manager |
| **pnpm** | `pnpm-lock.yaml` | `pnpm install --frozen-lockfile` | Fast, disk-space efficient |
| **Yarn** | `yarn.lock` | `yarn install --frozen-lockfile` | Classic and Berry (PnP) supported |
| **Bun** | `bun.lockb` | `bun install --frozen-lockfile` | Ultra-fast JavaScript runtime |

### Package Manager Conflicts

If multiple lock files are detected, Vite.MsBuild provides clear guidance:

```
⚠️  Multiple package manager lock files detected: yarn.lock, package-lock.json
💡 To resolve with yarn:
   rm package-lock.json && yarn install --frozen-lockfile
💡 Then commit the updated yarn lock file to your repository
```

---

## 🏗️ Build Integration

### MSBuild Target Execution Order

```
ResolveStaticWebAssetsInputs
├── ShowViteDiagnostics (if verbosity >= detailed)
├── ResolveViteMode (Configuration → Vite mode mapping)
├── ValidateViteSetup (validation + welcome message)
├── EnsureNodeDependencies (npm install if needed)
└── ViteBuildAssets (main Vite build)
```

### Build Timing Options

Control when Vite builds relative to C# compilation:

```xml
<PropertyGroup>
  <!-- Build before C# (default) -->
  <ViteBuildTiming>BeforeCSharp</ViteBuildTiming>
  
  <!-- Build after C# compilation -->
  <ViteBuildTiming>AfterCSharp</ViteBuildTiming>
</PropertyGroup>
```

### Verbosity Mapping

MSBuild verbosity automatically maps to Vite log levels:

```bash
dotnet build -v:quiet     # → vite build --logLevel silent
dotnet build -v:minimal   # → vite build --logLevel warn
dotnet build -v:normal    # → vite build --logLevel info
dotnet build -v:detailed  # → vite build --logLevel info + diagnostics
dotnet build -v:diag      # → vite build --logLevel debug
```

---

## 🏢 Enterprise & Monorepo Support

### Multi-SPA Configuration

For complex applications with multiple frontend entry points:

```xml
<ItemGroup>
  <ViteConfig Include="Areas/Admin/vite.config.ts">
    <BuildId>admin</BuildId>
    <OutputDir>wwwroot/admin</OutputDir>
    <Mode>development</Mode>
  </ViteConfig>
  
  <ViteConfig Include="Areas/Portal/vite.config.ts">
    <BuildId>portal</BuildId>
    <OutputDir>wwwroot/portal</OutputDir>
    <Mode>production</Mode>
  </ViteConfig>
</ItemGroup>
```

### Monorepo Project Structure

**Option 1: Shared Package.json**
```
MyProject/
├── package.json              ← Shared dependencies
├── vite.config.ts            ← Shared Vite config  
└── src/
    └── WebApp/
        └── WebApp.csproj     ← References Vite.MsBuild
```

**Option 2: Project-Specific Dependencies**
```
MyProject/
└── src/
    └── WebApp/
        ├── package.json      ← WebApp-specific dependencies
        ├── vite.config.ts    ← WebApp-specific config
        └── WebApp.csproj     ← References Vite.MsBuild
```

---

## 🧪 Framework Examples

### Vue 3 + TypeScript

```bash
npm create vite@latest . -- --template vue-ts
```

**vite.config.ts:**
```typescript
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  build: {
    outDir: 'wwwroot',
    manifest: true,
    rollupOptions: {
      input: 'src/main.ts'
    }
  }
})
```

### React + TypeScript

```bash
npm create vite@latest . -- --template react-ts
```

**vite.config.ts:**
```typescript
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  build: {
    outDir: 'wwwroot',
    manifest: true,
    rollupOptions: {
      input: 'src/main.tsx'
    }
  }
})
```

### Svelte + TypeScript

```bash
npm create vite@latest . -- --template svelte-ts
```

**vite.config.ts:**
```typescript
import { defineConfig } from 'vite'
import { svelte } from '@sveltejs/vite-plugin-svelte'

export default defineConfig({
  plugins: [svelte()],
  build: {
    outDir: 'wwwroot',
    manifest: true,
    rollupOptions: {
      input: 'src/main.ts'
    }
  }
})
```

---

## 🔄 Migration Guide

### From Vite.MsBuild 1.x to 2.x

**Breaking Changes:**

1. **New Task Architecture** - Migrated from XML-based tasks to high-performance C# tasks
2. **Auto-Enable by Default** - Package now auto-enables when installed (can be disabled)
3. **Improved File Detection** - Better handling of framework-specific files and config files

**Migration Steps:**

1. **Update Package Reference:**
   ```xml
   <!-- Old -->
   <PackageReference Include="Vite.MsBuild" Version="1.x" />
   
   <!-- New -->
   <PackageReference Include="Vite.MsBuild" Version="2.0.0" />
   ```

2. **Remove Old Configuration (if using):**
   ```xml
   <!-- These are now auto-detected -->
   <PropertyGroup>
     <ViteTasksLoaded>true</ViteTasksLoaded>  ← Remove
     <ViteAssetsEnabled>true</ViteAssetsEnabled>  ← Remove
   </PropertyGroup>
   ```

3. **Verify Output Directory:**
   ```xml
   <!-- Ensure your Vite config outputs to wwwroot (default) -->
   <PropertyGroup>
     <ViteOutputDir>wwwroot</ViteOutputDir>
   </PropertyGroup>
   ```

**New Features in 2.x:**
- ✅ Automatic package manager detection
- ✅ Better error messages and validation
- ✅ Improved incremental build performance
- ✅ Enhanced monorepo support
- ✅ Comprehensive logging and diagnostics

---

## 🛠️ Troubleshooting

### Common Issues

**Build fails with "Task attempted to log before it was initialized"**
- **Fixed in 2.x** - This was resolved with the new C# task architecture

**Multiple package manager lock files detected**
- Choose one package manager and remove other lock files
- Follow the guidance messages for your preferred package manager

**Vite config not found**
- Ensure `vite.config.ts`, `vite.config.js`, or `vite.config.mjs` exists
- Use `<ViteConfigFile>` to specify custom location

**Assets not appearing in output**
- Check that Vite build outputs to the correct directory (usually `wwwroot`)
- Verify `<ViteOutputDir>` property matches your Vite config `build.outDir`

### Debug Information

Enable detailed logging to diagnose issues:

```bash
dotnet build -v:detailed
```

This shows:
- ✅ Package manager detection
- ✅ File discovery process  
- ✅ Vite command execution
- ✅ Build timing information

---

## 📈 Performance

### Incremental Build Optimization

Vite.MsBuild uses MSBuild's incremental build system:

- **Input Files**: All frontend source files + config files
- **Output Marker**: Timestamp file in `obj/` directory  
- **Build Logic**: Only rebuilds if inputs are newer than outputs

**Typical Performance:**
- **Clean Build**: 5-15 seconds (depending on project size)
- **Incremental Build**: 0.1-0.5 seconds (if no changes)
- **Changed File Build**: 1-5 seconds (Vite HMR speed)

### CI/CD Optimization

For optimal CI/CD performance:

```yaml
# GitHub Actions example
- name: Build
  run: dotnet build --configuration Release --verbosity minimal
  
# Only install dependencies if lock file changed
- name: Cache Node modules
  uses: actions/cache@v3
  with:
    path: node_modules
    key: ${{ runner.os }}-node-${{ hashFiles('**/package-lock.json') }}
```

---

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

### Development Setup

```bash
# Clone the repository
git clone https://github.com/DigitalParadox/Vite.MsBuild.git
cd Vite.MsBuild

# Restore dependencies
dotnet restore

# Run tests
dotnet test

# Build package
.\build-package.ps1 -Version "1.0.0"
```

### Test Coverage

Current test coverage: **434 tests passing** (100% pass rate)

- ✅ **Core Tasks**: ValidateViteProjectTask, DetectPackageManagerTask, CollectViteInputFilesTask
- ✅ **Resolver Tasks**: ViteConfigurationResolver, ViteConfigDependencyResolver, ViteModeResolver
- ✅ **Command Builders**: ViteCommandFactory, ScriptBasedCommandBuilder, DirectToolCommandBuilder, CustomCommandBuilder
- ✅ **Validation**: ViteConfig, ValidateViteConfig
- ✅ **Orchestration**: OrchestrateBuildTask (multi-config builds, dependency ordering, incremental builds)
- ✅ **Build Integration**: MSBuild target execution, parallel builds, marker files
- ✅ **Framework Support**: Vue, React, Svelte file detection
- ✅ **Package Managers**: npm, pnpm, yarn, bun detection and command generation
- ✅ **Advanced Features**: Dependency graphs, topological sorting, circular detection, mode resolution, architecture detection
- ✅ **Error Scenarios**: Validation, conflict resolution, helpful messages, encoding compatibility

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).

---

## 🔗 Links

- **📦 NuGet Package**: https://www.nuget.org/packages/Vite.MsBuild
- **🐛 Issues**: https://github.com/DigitalParadox/Vite.MsBuild/issues
- **💬 Discussions**: https://github.com/DigitalParadox/Vite.MsBuild/discussions
- **🔀 Pull Requests**: https://github.com/DigitalParadox/Vite.MsBuild/pulls

---

*Made with ❤️ for the ASP.NET Core and Vite communities*