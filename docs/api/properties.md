---
layout: default
title: MSBuild Properties
parent: API Reference
nav_order: 1
---

# MSBuild Properties
{: .fs-9 }

Complete reference for all MSBuild properties in ViteKit.Msbuild.
{: .fs-6 .fw-300 }

## Core Properties

### EnableViteBuild

Enable or disable Vite builds entirely.

| Property | Type | Default |
|:---------|:-----|:--------|
| `EnableViteBuild` | boolean | `true` (NuGet), `false` (Directory.Build.props) |

**Example:**
```xml
<PropertyGroup>
  <EnableViteBuild>false</EnableViteBuild>
</PropertyGroup>
```

### ViteProjectRoot

Root directory containing `package.json` and Vite configuration.

| Property | Type | Default |
|:---------|:-----|:--------|
| `ViteProjectRoot` | path | Auto-detected (project dir or repo root) |

**Auto-detection logic:**
1. If `package.json` exists in project directory → use project directory
2. Otherwise → use repository root

**Example:**
```xml
<PropertyGroup>
  <ViteProjectRoot>$(MSBuildThisFileDirectory)frontend\</ViteProjectRoot>
</PropertyGroup>
```

### ViteConfigFile

Path to Vite configuration file.

| Property | Type | Default |
|:---------|:-----|:--------|
| `ViteConfigFile` | path | Auto-detected (`vite.config.ts`, `.js`, `.mjs`) |

**Auto-detection order:**
1. `$(MSBuildProjectDirectory)\vite.config.ts`
2. `$(MSBuildProjectDirectory)\vite.config.js`
3. `$(MSBuildProjectDirectory)\vite.config.mjs`
4. `$(ViteProjectRoot)vite.config.ts`
5. `$(ViteProjectRoot)vite.config.js`
6. `$(ViteProjectRoot)vite.config.mjs`

**Example:**
```xml
<PropertyGroup>
  <ViteConfigFile>custom.vite.config.ts</ViteConfigFile>
</PropertyGroup>
```

### ViteOutputDir

Output directory for Vite build artifacts (relative to project directory).

| Property | Type | Default |
|:---------|:-----|:--------|
| `ViteOutputDir` | path | `wwwroot\dist` |

**Example:**
```xml
<PropertyGroup>
  <ViteOutputDir>wwwroot\assets</ViteOutputDir>
</PropertyGroup>
```

### ViteMode

Vite build mode (development, production, staging, etc.).

| Property | Type | Default |
|:---------|:-----|:--------|
| `ViteMode` | string | Auto-mapped from `$(Configuration)` |

**Auto-mapping:**
- `Debug` → `development`
- `Release` → `production`
- Custom configurations → lowercase configuration name

**Example:**
```xml
<PropertyGroup>
  <ViteMode>staging</ViteMode>
</PropertyGroup>
```

## Package Manager Properties

### PackageManager

Which package manager to use for npm install and Vite commands.

| Property | Type | Default |
|:---------|:-----|:--------|
| `PackageManager` | string | Auto-detected from lock files |

**Supported values:** `npm`, `pnpm`, `yarn`, `bun`

**Auto-detection priority:**
1. `bun.lockb` → `bun`
2. `pnpm-lock.yaml` → `pnpm`
3. `yarn.lock` → `yarn`
4. `package-lock.json` → `npm`
5. Default → `npm`

**Example:**
```xml
<PropertyGroup>
  <PackageManager>pnpm</PackageManager>
</PropertyGroup>
```

### VitePackageManagerConflictAction

Behavior when multiple lock files are detected.

| Property | Type | Default |
|:---------|:-----|:--------|
| `VitePackageManagerConflictAction` | string | `warn` (`error` in CI) |

**Supported values:** `silent`, `warn`, `error`

**Example:**
```xml
<PropertyGroup>
  <VitePackageManagerConflictAction>error</VitePackageManagerConflictAction>
</PropertyGroup>
```

## Build Command Properties

### ViteBuildCommand

Custom build command override (highest priority).

| Property | Type | Default |
|:---------|:-----|:--------|
| `ViteBuildCommand` | string | Empty (use auto-detection) |

**Example:**
```xml
<PropertyGroup>
  <ViteBuildCommand>npm run build:production</ViteBuildCommand>
</PropertyGroup>
```

### ViteBuildScript

Which package.json script to run.

| Property | Type | Default |
|:---------|:-----|:--------|
| `ViteBuildScript` | string | `build` |

**Example:**
```xml
<PropertyGroup>
  <ViteBuildScript>build:staging</ViteBuildScript>
</PropertyGroup>
```

**To force direct Vite CLI calls:**
```xml
<PropertyGroup>
  <ViteBuildScript></ViteBuildScript> <!-- Empty string -->
</PropertyGroup>
```

### DirectViteBuild

Force direct Vite CLI calls, bypassing package.json scripts.

| Property | Type | Default |
|:---------|:-----|:--------|
| `DirectViteBuild` | boolean | `false` |

**Example:**
```xml
<PropertyGroup>
  <DirectViteBuild>true</DirectViteBuild>
</PropertyGroup>
```

## Build Timing Properties

### ViteBuildTiming

When to run Vite build relative to C# compilation.

| Property | Type | Default |
|:---------|:-----|:--------|
| `ViteBuildTiming` | string | `BeforeCSharp` |

**Supported values:**
- `BeforeCSharp` - Run Vite build before `ResolveStaticWebAssetsInputs`
- `AfterCSharp` - Run Vite build after `Build` target completes

**Example:**
```xml
<PropertyGroup>
  <ViteBuildTiming>AfterCSharp</ViteBuildTiming>
</PropertyGroup>
```

## Logging and Diagnostics Properties

### ViteLogLevel

Vite log level (auto-mapped from MSBuild verbosity).

| Property | Type | Default |
|:---------|:-----|:--------|
| `ViteLogLevel` | string | Auto-mapped |

**Auto-mapping:**
- `MSBuildVerbosity=quiet` → `silent`
- `MSBuildVerbosity=minimal` → `warn`
- `MSBuildVerbosity=normal` → `info`
- `MSBuildVerbosity=detailed` → `info`
- `MSBuildVerbosity=diagnostic` → `debug`

**Example:**
```xml
<PropertyGroup>
  <ViteLogLevel>debug</ViteLogLevel>
</PropertyGroup>
```

### ViteEnableColors

Enable ANSI color codes in Vite output.

| Property | Type | Default |
|:---------|:-----|:--------|
| `ViteEnableColors` | boolean | `false` |

**Example:**
```xml
<PropertyGroup>
  <ViteEnableColors>true</ViteEnableColors>
</PropertyGroup>
```

### ViteEnableDiagnostics

Enable diagnostic logging for troubleshooting.

| Property | Type | Default |
|:---------|:-----|:--------|
| `ViteEnableDiagnostics` | boolean | `false` |

**Example:**
```xml
<PropertyGroup>
  <ViteEnableDiagnostics>true</ViteEnableDiagnostics>
</PropertyGroup>
```

### ViteDiagnosticLogPath

Directory for diagnostic log files.

| Property | Type | Default |
|:---------|:-----|:--------|
| `ViteDiagnosticLogPath` | path | `$(LOCALAPPDATA)\ViteKit.MsBuild\diagnostics` |

**Example:**
```xml
<PropertyGroup>
  <ViteDiagnosticLogPath>D:\logs\vitekit</ViteDiagnosticLogPath>
</PropertyGroup>
```

## Advanced Properties

### ViteUseDlxFallback

Use npx/dlx when Vite not found in package.json dependencies.

| Property | Type | Default |
|:---------|:-----|:--------|
| `ViteUseDlxFallback` | boolean | `false` |

**Example:**
```xml
<PropertyGroup>
  <ViteUseDlxFallback>true</ViteUseDlxFallback>
</PropertyGroup>
```

### ViteMissingEnvAction

Behavior when .env files are missing.

| Property | Type | Default |
|:---------|:-----|:--------|
| `ViteMissingEnvAction` | string | `silent` |

**Supported values:** `silent`, `warn`, `error`

**Example:**
```xml
<PropertyGroup>
  <ViteMissingEnvAction>warn</ViteMissingEnvAction>
</PropertyGroup>
```

### ViteWatchIntegration

Add frontend files to `dotnet watch` monitoring.

| Property | Type | Default |
|:---------|:-----|:--------|
| `ViteWatchIntegration` | boolean | `false` |

**Example:**
```xml
<PropertyGroup>
  <ViteWatchIntegration>true</ViteWatchIntegration>
</PropertyGroup>
```

## Computed Properties (Read-Only)

These properties are computed automatically and should not be overridden:

### ViteOutputPath

Absolute path to Vite output directory.

**Formula:** `$(MSBuildProjectDirectory)\$(ViteOutputDir)`

### ViteBuildMarker

Path to build marker file for incremental builds.

**Formula:** `$(IntermediateOutputPath)$(TargetFramework).ViteKit.Msbuild.Build.marker`

### NodeRestoreMarker

Path to npm install marker file.

**Formula:** `$(ViteProjectRoot)obj\ViteKit.Msbuild.NodeRestore.marker`

### ViteInstallCommand

npm install command for current package manager.

**Auto-generated based on `$(PackageManager)`:**
- `npm` → `npm ci`
- `pnpm` → `pnpm install --frozen-lockfile`
- `yarn` → `yarn install --frozen-lockfile`
- `bun` → `bun install --frozen-lockfile`

## Property Priority

Properties are evaluated in this priority order:

1. **User .csproj** - Highest priority
2. **ViteKit.Msbuild.props** - Default values
3. **Auto-detection** - Fallback when not specified

**Example:**
```xml
<!-- User overrides in .csproj (highest priority) -->
<PropertyGroup>
  <ViteMode>staging</ViteMode>
  <PackageManager>pnpm</PackageManager>
</PropertyGroup>

<!-- ViteKit.Msbuild.props provides defaults -->
<!-- Auto-detection kicks in only if not specified -->
```

## Configuration Examples

### Minimal Configuration
```xml
<PackageReference Include="ViteKit.Msbuild" Version="2.0.0" />
<!-- Everything auto-detected -->
```

### Production Configuration
```xml
<PropertyGroup>
  <ViteMode>production</ViteMode>
  <ViteOutputDir>wwwroot\assets</ViteOutputDir>
  <PackageManager>pnpm</PackageManager>
  <ViteEnableColors>false</ViteEnableColors>
</PropertyGroup>
```

### Development Configuration
```xml
<PropertyGroup>
  <ViteMode>development</ViteMode>
  <ViteBuildTiming>AfterCSharp</ViteBuildTiming>
  <ViteWatchIntegration>true</ViteWatchIntegration>
  <ViteLogLevel>debug</ViteLogLevel>
</PropertyGroup>
```

### CI/CD Configuration
```xml
<PropertyGroup>
  <PackageManager>npm</PackageManager>
  <VitePackageManagerConflictAction>error</VitePackageManagerConflictAction>
  <ViteLogLevel>warn</ViteLogLevel>
  <ViteEnableDiagnostics>true</ViteEnableDiagnostics>
</PropertyGroup>
```
