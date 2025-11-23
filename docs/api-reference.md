---
layout: default
title: API Reference
nav_order: 6
---

# API Reference
{: .no_toc }

Complete reference for all ViteKit.Msbuild properties and items.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## MSBuild Properties

### Core Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `EnableViteBuild` | bool | `true` | Enable/disable Vite builds |
| `ViteProjectRoot` | string | Auto-detected | Path to Vite project root |
| `ViteOutputDir` | string | `wwwroot` | Vite build output directory |
| `ViteMode` | string | `development` or `production` | Vite build mode |
| `ViteConfigFile` | string | Auto-detected | Path to vite.config file |
| `PackageManager` | string | Auto-detected | Package manager to use |
| `ViteBuildTiming` | string | `BeforeCSharp` | When to run Vite build |
| `ShowWelcomeMessage` | bool | `true` | Show ViteKit welcome message |

### Advanced Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ViteMissingEnvAction` | string | `warn` | Action when .env files missing (`error`, `warn`, `silent`) |
| `ViteBuildMarker` | string | `obj/ViteKit.Build.marker` | Incremental build marker file |
| `NodeRestoreMarker` | string | `obj/ViteKit.NodeRestore.marker` | Dependency install marker |

#### Command Resolution Precedence

Execution strategy is chosen in this strict order:

| Priority | Trigger | Execution Mode |
|----------|---------|----------------|
| 1 | `ViteBuildCommand` non-empty | Use exact command string verbatim |
| 2 | `DirectViteBuild` == `true` | Direct CLI (`npx vite build`) ignoring scripts |
| 3 | `ViteBuildScript` == empty string | Direct CLI (explicit script opt-out) |
| 4 | `ViteBuildScript` value AND script exists | Run script (`<pm> run <script>`) |
| 5 | Fallback (no script / missing) | Direct CLI (`npx vite build`) |

Notes:
- Empty `ViteBuildScript` differs from unset: it explicitly forces direct CLI.
- Script existence is determined by reading `package.json` once per build (future optimization: caching).
- Package manager used (npm/pnpm/yarn/bun) is resolved per configuration item; if absent falls back to global detection.


---

## MSBuild Items

### ViteConfiguration

Define multiple Vite build configurations:

```xml
<ItemGroup>
  <ViteConfiguration Include="path/to/config.ts">
    <Mode>production</Mode>
    <OutputDir>wwwroot/app</OutputDir>
  </ViteConfiguration>
</ItemGroup>
```

**Metadata**:
- `Mode` (string): Override build mode for this config
- `OutputDir` (string): Override output directory

---

### ViteInputFiles

Files that trigger Vite rebuilds when changed:

```xml
<ItemGroup>
  <!-- Add custom patterns -->
  <ViteInputFiles Include="custom/**/*.ts" />
  
  <!-- Remove patterns -->
  <ViteInputFiles Remove="legacy/**/*" />
</ItemGroup>
```

**Default patterns** (auto-included):
- `**/*.ts`, `**/*.tsx`, `**/*.mts`
- `**/*.js`, `**/*.jsx`, `**/*.mjs`
- `**/*.vue`
- `**/*.svelte`
- `**/*.css`, `**/*.scss`, `**/*.less`
- `**/*.json` (except `package.json`, lock files)
- `vite.config.*`
- `package.json`

---

## Targets

### Public Targets

Targets you can depend on or extend:

| Target | Description | Runs |
|--------|-------------|------|
| `ViteBuildAssets` | Main Vite build target | Before `Build` |
| `ViteClean` | Clean Vite outputs | During `Clean` |
| `ValidateViteSetup` | Validate configuration | Before build |
| `EnsureNodeDependencies` | Install npm packages | Before build |

---

### Extensibility Points

Hook into the build pipeline:

```xml
<!-- Before Vite build -->
<Target Name="MyCustomTarget" BeforeTargets="ViteBuildAssets">
  <Message Text="Running before Vite..." />
</Target>

<!-- After Vite build -->
<Target Name="MyCustomTarget" AfterTargets="ViteBuildAssets">
  <Message Text="Vite build completed!" />
</Target>
```

---

## Property Defaults by Configuration

### Debug Configuration

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <ViteMode>development</ViteMode>
</PropertyGroup>
```

### Release Configuration

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <ViteMode>production</ViteMode>
</PropertyGroup>
```

---

## Auto-Detection Rules

### Package Manager

ViteKit detects package managers in this order:

1. `<PackageManager>` property (if set)
2. `bun.lockb` → bun
3. `pnpm-lock.yaml` → pnpm
4. `yarn.lock` → yarn
5. `package-lock.json` → npm
6. Default → npm

### Vite Config File

ViteKit searches for these files in order:

1. `<ViteConfigFile>` property (if set)
2. `vite.config.ts`
3. `vite.config.mts`
4. `vite.config.js`
5. `vite.config.mjs`

### Project Root

ViteKit searches for `package.json` in:

1. `<ViteProjectRoot>` property (if set)
2. `$(MSBuildProjectDirectory)`
3. `$(MSBuildProjectDirectory)/..` (solution root)

---

## Verbosity Mapping

MSBuild verbosity automatically maps to Vite:

| MSBuild (`-v`) | Vite | Details |
|----------------|------|---------|
| `quiet` | `silent` | Suppress all output |
| `minimal` | `warn` | Warnings and errors only |
| `normal` | `info` | Standard build info |
| `detailed` | `info` | + ViteKit diagnostics |
| `diagnostic` | `debug` | Full debug output |

Example:
```bash
dotnet build -v:diagnostic
```

---

## Environment Variables

ViteKit respects standard Vite environment variables:

- `VITE_*` - Exposed to client code
- `NODE_ENV` - Node environment
- `MODE` - Vite mode (overridden by `ViteMode`)

---

## Complete Example

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    
    <!-- Core ViteKit settings -->
    <EnableViteBuild>true</EnableViteBuild>
    <ViteProjectRoot>$(MSBuildProjectDirectory)/client</ViteProjectRoot>
    <ViteOutputDir>wwwroot/dist</ViteOutputDir>
    <ViteMode Condition="'$(Configuration)' == 'Staging'">staging</ViteMode>
    <PackageManager>pnpm</PackageManager>
    <ViteBuildTiming>AfterCSharp</ViteBuildTiming>
  </PropertyGroup>

  <ItemGroup>
    <!-- ViteKit package -->
    <PackageReference Include="ViteKit.Msbuild" Version="*" />
  </ItemGroup>

  <ItemGroup>
    <!-- Multi-config setup -->
    <ViteConfiguration Include="vite.main.config.ts">
      <OutputDir>wwwroot/main</OutputDir>
    </ViteConfiguration>
    <ViteConfiguration Include="vite.admin.config.ts">
      <OutputDir>wwwroot/admin</OutputDir>
    </ViteConfiguration>
  </ItemGroup>

  <ItemGroup>
    <!-- Custom file tracking -->
    <ViteInputFiles Include="shared/**/*.ts" />
    <ViteInputFiles Remove="temp/**/*" />
  </ItemGroup>
</Project>
```

---

## See Also

- [Configuration Guide](configuration) - Detailed property explanations
- [Examples](examples) - Real-world usage patterns
- [Troubleshooting](troubleshooting) - Common issues and solutions
