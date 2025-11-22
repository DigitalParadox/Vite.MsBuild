# Real-World Monorepo with ViteKit.Msbuild

## Typical Enterprise Monorepo Structure

```
Enterprise.Solution/
├── Enterprise.sln                    ← Solution file
├── Directory.Build.props              ← Shared MSBuild properties
├── package.json                      ← Root package.json (shared deps)
├── pnpm-workspace.yaml               ← PNPM workspace config
│
├── apps/
│   ├── AdminPortal/
│   │   ├── AdminPortal.csproj        ← Each app = separate .csproj
│   │   ├── package.json              ← App-specific frontend deps
│   │   ├── vite.config.ts            ← App-specific Vite config
│   │   ├── wwwroot/
│   │   └── src/
│   │
│   ├── CustomerPortal/
│   │   ├── CustomerPortal.csproj     ← Separate .csproj
│   │   ├── package.json              ← Different frontend stack
│   │   ├── vite.config.ts            ← Different Vite config
│   │   ├── wwwroot/
│   │   └── src/
│   │
│   └── MobileApi/
│       ├── MobileApi.csproj          ← API-only, no frontend
│       └── Controllers/
│
├── packages/                         ← Shared libraries
│   ├── UI.Components/
│   │   ├── UI.Components.csproj      ← Shared UI library
│   │   ├── package.json              ← UI components as NPM package
│   │   ├── vite.config.ts            ← Library build config
│   │   └── src/
│   │
│   └── Shared.Models/
│       ├── Shared.Models.csproj      ← C# models only
│       └── Models/
│
└── tools/
    └── BuildScripts/
        └── BuildScripts.csproj       ← Build automation
```

## **How ViteKit.Msbuild Works in This Structure**

### **Each .csproj Gets Independent Vite Integration** ✅

```xml
<!-- apps/AdminPortal/AdminPortal.csproj -->
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  
  <PackageReference Include="ViteKit.Msbuild" Version="1.0.0" />
  <!-- ViteKit.Msbuild auto-detects: apps/AdminPortal/vite.config.ts -->
  <!-- Uses: apps/AdminPortal/package.json -->
  <!-- Outputs to: apps/AdminPortal/wwwroot/dist -->
</Project>
```

```xml
<!-- apps/CustomerPortal/CustomerPortal.csproj -->
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  
  <PackageReference Include="ViteKit.Msbuild" Version="1.0.0" />
  <!-- Completely independent:
       - apps/CustomerPortal/vite.config.ts
       - apps/CustomerPortal/package.json  
       - apps/CustomerPortal/wwwroot/dist -->
</Project>
```

```xml
<!-- packages/UI.Components/UI.Components.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
  </PropertyGroup>
  
  <PackageReference Include="ViteKit.Msbuild" Version="1.0.0" />
  <!-- Library mode:
       - packages/UI.Components/vite.config.ts (library build)
       - packages/UI.Components/package.json
       - packages/UI.Components/dist/ (for NPM publishing) -->
</Project>
```

## **Package Manager Detection in Monorepos**

### **Workspace Detection** 📦

Our current logic handles this perfectly:

```xml
<!-- From ViteKit.Msbuild.props -->
<ViteProjectRoot Condition="'$(ViteProjectRoot)' == '' AND Exists('$(MSBuildProjectDirectory)\package.json')">
  $(MSBuildProjectDirectory)\  <!-- ✅ Use project-specific package.json -->
</ViteProjectRoot>

<ViteProjectRoot Condition="'$(ViteProjectRoot)' == ''">
  $(MSBuildThisFileDirectory)   <!-- ✅ Fall back to repo root -->
</ViteProjectRoot>
```

**Real behavior in monorepo:**

1. **AdminPortal.csproj** → Uses `apps/AdminPortal/package.json` (if exists)
2. **CustomerPortal.csproj** → Uses `apps/CustomerPortal/package.json` (if exists)  
3. **MobileApi.csproj** → Uses root `package.json` (no local package.json)

### **Package Manager Markers Are Per-Project** 🎯

Each project gets its own marker files:
```
apps/AdminPortal/obj/ViteKit.Msbuild.pnpm.marker
apps/CustomerPortal/obj/ViteKit.Msbuild.npm.marker  
packages/UI.Components/obj/ViteKit.Msbuild.yarn.marker
```

**This is perfect!** Each project can use different package managers if needed.

## **Common Monorepo Patterns**

### **Pattern 1: Shared Package Manager** 
```
├── pnpm-workspace.yaml       ← PNPM workspace
├── package.json              ← Root dependencies
├── apps/AdminPortal/         ← No package.json (uses root)
└── apps/CustomerPortal/      ← No package.json (uses root)
```
**Result**: Both projects use root package.json + pnpm

### **Pattern 2: Mixed Package Managers**
```
├── package.json              ← Root uses npm
├── apps/AdminPortal/
│   └── package.json          ← Admin uses pnpm  
└── apps/CustomerPortal/
    └── package.json          ← Customer uses yarn
```
**Result**: Each project uses its preferred package manager

### **Pattern 3: Hybrid Dependencies**
```
├── package.json              ← Shared backend tools
├── apps/AdminPortal/
│   └── package.json          ← Vue 3 + TypeScript
└── apps/CustomerPortal/  
    └── package.json          ← React + JavaScript
```
**Result**: Different frontend stacks, shared tooling

## **Testing Strategy for Monorepos** 🧪

We should add tests for:

1. **Project-specific package.json detection**
2. **Fallback to repo root package.json**  
3. **Independent package manager markers**
4. **Shared vs project-specific Vite configs**

## **Benefits of This Approach** ✅

1. **🎯 Natural Separation** - Each app is truly independent
2. **🔧 Different Tech Stacks** - Admin can use Vue, Customer can use React
3. **📦 Flexible Dependencies** - Each project manages its own frontend deps
4. **⚡ Parallel Builds** - MSBuild can build projects in parallel
5. **🧹 Clean Architecture** - Clear boundaries between applications

You're absolutely right - **real monorepos use separate .csproj files**, which makes our current implementation actually perfect for this scenario! Each project gets its own Vite integration automatically.