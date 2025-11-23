---
layout: default
title: Configuration
nav_order: 3
---

# Configuration
{: .no_toc }

Comprehensive guide to configuring ViteKit.Msbuild.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## MSBuild Properties

Configure ViteKit by adding properties to your `.csproj` file:

```xml
<PropertyGroup>
  <EnableViteBuild>true</EnableViteBuild>
  <ViteOutputDir>wwwroot/assets</ViteOutputDir>
</PropertyGroup>
```

---

## Core Properties

### EnableViteBuild

Controls whether Vite builds run during MSBuild.

```xml
<EnableViteBuild>true</EnableViteBuild>
```

- **Type**: `bool`
- **Default**: `true` (when package is installed)
- **Use case**: Temporarily disable Vite builds

---

### ViteProjectRoot

Location of your Vite project (contains `package.json`).

```xml
<ViteProjectRoot>$(MSBuildProjectDirectory)</ViteProjectRoot>
```

- **Type**: `string` (path)
- **Default**: Auto-detected (project dir or solution root)
- **Use case**: Custom project layout

---

### ViteOutputDir

Overrides where Vite outputs built assets.
Passes `--outDir` to Vite.

```xml
<ViteOutputDir>wwwroot/dist</ViteOutputDir>
```

- **Type**: `string` (relative path)
- **Default**: `wwwroot`
- **Use case**: Custom output location

---

### ViteMode

Vite build mode (`development`, `production`, `staging`, etc.).

```xml
<ViteMode>staging</ViteMode>
```

- **Type**: `string`
- **Default**: `development` (Debug), `production` (Release)
- **Use case**: Custom build modes

---

### ViteConfigFile

Path to your Vite configuration file.

```xml
<ViteConfigFile>vite.config.ts</ViteConfigFile>
```

- **Type**: `string` (path)
- **Default**: Auto-detected (`vite.config.{ts,js,mjs}`)
- **Use case**: Custom config file name

---

### PackageManager

Force a specific package manager.

```xml
<PackageManager>pnpm</PackageManager>
```

- **Type**: `string` (`npm` | `yarn` | `pnpm` | `bun`)
- **Default**: Auto-detected from lock files
- **Use case**: Override auto-detection

---

### Command Resolution Precedence

ViteKit selects how to invoke Vite based on these properties (top wins):

| Order | Condition | Result |
|-------|-----------|--------|
| 1 | `ViteBuildCommand` set (non-empty) | Run exact command string (no further logic) |
| 2 | `DirectViteBuild` == `true` | Invoke direct CLI (`npx vite build`) ignoring scripts |
| 3 | `ViteBuildScript` explicitly set to empty (`""`) | Direct CLI (opt-out of scripts) |
| 4 | `ViteBuildScript` has value AND script exists in `package.json` | Run package manager script (`npm run <script>`) |
| 5 | Fallback (no script found) | Direct CLI (`npx vite build`) |

Example:
```xml
<PropertyGroup>
  <!-- Highest priority: overrides everything -->
  <ViteBuildCommand>npm run build:custom -- --sourcemap</ViteBuildCommand>
</PropertyGroup>
```

To force direct Vite while keeping other overrides:
```xml
<PropertyGroup>
  <DirectViteBuild>true</DirectViteBuild>
</PropertyGroup>
```

Explicit opt-out of scripts without enabling diagnostics:
```xml
<PropertyGroup>
  <ViteBuildScript></ViteBuildScript> <!-- empty -->
</PropertyGroup>
```

Normal script-based usage:
```xml
<PropertyGroup>
  <ViteBuildScript>build:prod</ViteBuildScript>
</PropertyGroup>
```

If `build:prod` does not exist, ViteKit falls back to direct CLI.

---

---

### ViteBuildTiming

When to run Vite builds relative to C# compilation.

```xml
<ViteBuildTiming>AfterCSharp</ViteBuildTiming>
```

- **Type**: `string` (`BeforeCSharp` | `AfterCSharp`)
- **Default**: `BeforeCSharp`
- **Use case**: Resolve build order issues

---

### ShowWelcomeMessage

Show ViteKit welcome message on first build.

```xml
<ShowWelcomeMessage>false</ShowWelcomeMessage>
```

- **Type**: `bool`
- **Default**: `true`
- **Use case**: Suppress informational messages

---

## Multi-Configuration Builds

Build multiple Vite configurations in one project:

```xml
<ItemGroup>
  <ViteConfiguration Include="vite.client.config.ts">
    <Mode>production</Mode>
    <OutputDir>wwwroot/client</OutputDir>
  </ViteConfiguration>
  
  <ViteConfiguration Include="vite.admin.config.ts">
    <Mode>production</Mode>
    <OutputDir>wwwroot/admin</OutputDir>
  </ViteConfiguration>
</ItemGroup>
```

Each configuration builds independently with its own settings.

---

## File Tracking

Customize which files trigger rebuilds:

### Add Files
```xml
<ItemGroup>
  <ViteInputFiles Include="custom/**/*.ts" />
  <ViteInputFiles Include="shared/**/*.vue" />
</ItemGroup>
```

### Remove Files
```xml
<ItemGroup>
  <ViteInputFiles Remove="legacy/**/*" />
  <ViteInputFiles Remove="temp/**/*" />
</ItemGroup>
```

---

## Environment-Specific Configuration

Use MSBuild conditions for environment-specific settings:

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <ViteMode>development</ViteMode>
  <ViteOutputDir>wwwroot</ViteOutputDir>
</PropertyGroup>

<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <ViteMode>production</ViteMode>
  <ViteOutputDir>wwwroot/dist</ViteOutputDir>
</PropertyGroup>
```

---

## Verbosity Control

MSBuild verbosity automatically maps to Vite log levels:

| MSBuild | Vite |
|---------|------|
| `quiet` | `silent` |
| `minimal` | `warn` |
| `normal` | `info` |
| `detailed` | `info` + diagnostics |
| `diagnostic` | `debug` |

Example:
```bash
dotnet build -v:detailed
```

---

## Advanced: Custom Targets

Hook into ViteKit's build pipeline:

```xml
<Target Name="BeforeViteBuild" BeforeTargets="ViteBuildAssets">
  <Message Text="Running before Vite build..." />
</Target>

<Target Name="AfterViteBuild" AfterTargets="ViteBuildAssets">
  <Message Text="Vite build completed!" />
</Target>
```

---

## Example Configurations

### Minimal
```xml
<ItemGroup>
  <PackageReference Include="ViteKit.Msbuild" Version="*" />
</ItemGroup>
```
Uses all defaults. Perfect for standard projects.

### Customized
```xml
<PropertyGroup>
  <ViteOutputDir>wwwroot/assets</ViteOutputDir>
  <ViteMode>staging</ViteMode>
  <PackageManager>pnpm</PackageManager>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="ViteKit.Msbuild" Version="*" />
</ItemGroup>
```

### Multi-SPA
```xml
<ItemGroup>
  <ViteConfiguration Include="vite.main.config.ts">
    <OutputDir>wwwroot/main</OutputDir>
  </ViteConfiguration>
  <ViteConfiguration Include="vite.embed.config.ts">
    <OutputDir>wwwroot/embed</OutputDir>
  </ViteConfiguration>
</ItemGroup>
```

---

## Next Steps

- [Examples](examples) - See real-world configurations
- [Troubleshooting](troubleshooting) - Common configuration issues
- [API Reference](api-reference) - Complete property reference
