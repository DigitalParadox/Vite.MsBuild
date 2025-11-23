---
layout: default
title: Advanced Scenarios
parent: Examples
nav_order: 10
---

# Advanced Scenarios
{: .no_toc }

Advanced configurations for complex build requirements.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Named Build Scripts

ViteKit's default command depends on your `ViteBuildScript` property setting.

### Default Behavior

By default, `ViteBuildScript` is set to `"build"`:

```xml
<!-- This is the default (you don't need to set this) -->
<ViteBuildScript>build</ViteBuildScript>
```

**If `package.json` has a `build` script:**
```json
{
  "scripts": {
    "build": "vite build"
  }
}
```
ViteKit runs: `npm run build` ✅

**If no `build` script exists:**
ViteKit falls back to: `npx vite build` ✅

{: .note }
To force direct Vite calls and skip script detection, set `<DirectViteBuild>true</DirectViteBuild>` or `<ViteBuildScript></ViteBuildScript>` (empty string)

---

### Custom Script Names

Use `ViteBuildScript` to specify a different script name:

```xml
<!-- MyProject.csproj -->
<PropertyGroup>
  <!-- Run 'npm run build:prod' instead of default 'npm run build' -->
  <ViteBuildScript>build:prod</ViteBuildScript>
</PropertyGroup>
```

```json
// package.json
{
  "scripts": {
    "build": "vite build",
    "build:prod": "vite build --mode production --minify"
  }
}
```

ViteKit will now run: `npm run build:prod`

---

### Complete Command Override

Use `ViteBuildCommand` for complete control over the command:

```xml
<PropertyGroup>
  <!-- Run ANY command - completely overrides ViteKit's command building -->
  <ViteBuildCommand>npm run build:custom -- --sourcemap</ViteBuildCommand>
</PropertyGroup>
```

This runs your exact command, bypassing all ViteKit logic (highest priority).

---

### Force Direct Vite Calls

Use `DirectViteBuild` to bypass package.json scripts entirely:

```xml
<PropertyGroup>
  <!-- Force 'npx vite build' even if package.json has build script -->
  <DirectViteBuild>true</DirectViteBuild>
</PropertyGroup>
```

Useful when you want MSBuild to control all Vite CLI flags directly without wrapper scripts.

---

### Priority Order

ViteKit resolves commands in this order:

1. **`ViteBuildCommand`** (if set) → Use exact command
2. **`DirectViteBuild=true`** → Use `npx vite build`
3. **`ViteBuildScript="" (empty)`** → Use `npx vite build`
4. **`ViteBuildScript="script-name"`** (if script exists in package.json) → Use `npm run script-name`
5. **Default fallback** → Use `npx vite build`

---

## Package Manager Configuration

### Global Package Manager Override

ViteKit auto-detects your package manager from lock files. To override globally:

```xml
<PropertyGroup>
  <!-- Force pnpm for all configurations -->
  <PackageManager>pnpm</PackageManager>
</PropertyGroup>
```

Auto-detection order:
1. `bun.lockb` → bun
2. `pnpm-lock.yaml` → pnpm
3. `yarn.lock` → yarn
4. `package-lock.json` → npm
5. Default → npm

### Per-Configuration Package Manager

You can specify different package managers for each `ViteConfig`:

```xml
<ItemGroup>
  <!-- Admin SPA uses pnpm -->
  <ViteConfig Include="admin/vite.config.ts">
    <PackageManager>pnpm</PackageManager>
    <OutputDir>wwwroot/admin</OutputDir>
  </ViteConfig>
  
  <!-- Customer SPA uses npm -->
  <ViteConfig Include="customer/vite.config.ts">
    <PackageManager>npm</PackageManager>
    <OutputDir>wwwroot/customer</OutputDir>
  </ViteConfig>
  
  <!-- Partner SPA uses yarn -->
  <ViteConfig Include="partner/vite.config.ts">
    <PackageManager>yarn</PackageManager>
    <OutputDir>wwwroot/partner</OutputDir>
  </ViteConfig>
</ItemGroup>
```

If not specified on a config, it falls back to the global `<PackageManager>` property.

---

### Environment-Specific Scripts

Use different scripts per configuration:

```xml
<!-- MyProject.csproj -->
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <ViteBuildScript>build:dev</ViteBuildScript>
</PropertyGroup>

<PropertyGroup Condition="'$(Configuration)' == 'Staging'">
  <ViteBuildScript>build:staging</ViteBuildScript>
</PropertyGroup>

<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <ViteBuildScript>build:prod</ViteBuildScript>
</PropertyGroup>
```

```json
// package.json
{
  "scripts": {
    "build:dev": "vite build --mode development --sourcemap",
    "build:staging": "cross-env NODE_ENV=staging vite build",
    "build:prod": "cross-env NODE_ENV=production vite build --minify"
  }
}
```

Usage:
```bash
dotnet build                # Runs 'npm run build:dev'
dotnet build -c Staging     # Runs 'npm run build:staging'
dotnet build -c Release     # Runs 'npm run build:prod'
```

---

### Advanced: Conditional Script Selection

Combine conditions for complex scenarios:

```xml
<PropertyGroup>
  <!-- Default to build script -->
  <ViteBuildScript>build</ViteBuildScript>
  
  <!-- Override for CI/CD -->
  <ViteBuildScript Condition="'$(CI)' == 'true'">build:ci</ViteBuildScript>
  
  <!-- Override for production release -->
  <ViteBuildScript Condition="'$(Configuration)' == 'Release' AND '$(PublishProfile)' != ''">
    build:publish
  </ViteBuildScript>
</PropertyGroup>
```

{: .note }
**Command Resolution Precedence** (highest → lowest):
1. `ViteBuildCommand` (explicit override) → run exactly what you specify
2. `DirectViteBuild=true` → force direct `npx vite build` (ignores scripts)
3. `ViteBuildScript=""` (empty string) → explicit opt-out of scripts → direct `npx vite build`
4. `ViteBuildScript="name"` AND script exists → run package manager script (`npm run name` / `pnpm name` / etc.)
5. Fallback (no script found) → direct `npx vite build`

This matches internal `ViteCommandBuilder.DetermineCommandType()` logic.

---

## Custom Commands and Preprocessing

### Run Commands Before Vite Build

Execute custom commands before ViteKit's build:

```xml
<Target Name="PreprocessAssets" BeforeTargets="ViteBuild">
  <Message Text="Running preprocessing..." Importance="high" />
  <Exec Command="npm run generate-icons" WorkingDirectory="$(ViteProjectRoot)" />
  <Exec Command="npm run optimize-images" WorkingDirectory="$(ViteProjectRoot)" />
</Target>
```

### Run Commands After Vite Build

Process Vite outputs after build completes:

```xml
<Target Name="PostProcessAssets" AfterTargets="ViteBuild">
  <Message Text="Running post-processing..." Importance="high" />
  <Exec Command="npm run compress" WorkingDirectory="$(ViteProjectRoot)" />
  <Exec Command="npm run generate-sw" WorkingDirectory="$(ViteProjectRoot)" />
</Target>
```

### Using ViteBuildCommand for Complex Scenarios

For complete control, use `ViteBuildCommand` with custom scripts:

```xml
<PropertyGroup>
  <!-- Run a custom script that does preprocessing + build -->
  <ViteBuildCommand>npm run build:full</ViteBuildCommand>
</PropertyGroup>
```

```json
// package.json
{
  "scripts": {
    "build:full": "npm run generate && npm run build && npm run compress"
  }
}
```

---

## Custom Clean Targets

ViteKit provides a `ViteClean` target that runs after `Clean`. By default, it only removes:
- ViteKit marker files (`$(IntermediateOutputPath)ViteKit.Build.marker`)
- Vite cache directories (`.vite` and `node_modules/.vite`)

**It does NOT remove build outputs** - you must add custom targets for that.

### Clean Build Outputs

Add a custom target to remove Vite build outputs:

```xml
<Target Name="CleanViteOutputs" AfterTargets="ViteClean">
  <Message Text="Cleaning Vite build outputs..." Importance="high" />
  <RemoveDir Directories="$(ViteOutputDir)" />
  <Message Text="Cleaned: $(ViteOutputDir)" Importance="high" />
</Target>
```

Usage:
```bash
dotnet clean
```

### Clean Multiple Output Directories

For multi-configuration setups:

```xml
<Target Name="CleanAllViteOutputs" AfterTargets="ViteClean">
  <ItemGroup>
    <_ViteOutputDirs Include="wwwroot/main" />
    <_ViteOutputDirs Include="wwwroot/admin" />
    <_ViteOutputDirs Include="wwwroot/embed" />
  </ItemGroup>
  
  <RemoveDir Directories="@(_ViteOutputDirs)" />
  <Message Text="Cleaned all Vite output directories" Importance="high" />
</Target>
```

### Deep Clean (Including node_modules)

```xml
<Target Name="DeepClean" AfterTargets="ViteClean">
  <Message Text="Running deep clean..." Importance="high" />
  
  <!-- Remove build outputs -->
  <RemoveDir Directories="$(ViteOutputDir)" />
  
  <!-- Remove node_modules (careful - this forces full restore) -->
  <RemoveDir Directories="$(ViteProjectRoot)node_modules" />
  
  <Message Text="Deep clean completed - run 'dotnet build' to restore" Importance="high" />
</Target>
```

{: .warning }
Removing `node_modules` forces a full package restore on next build, which can be slow.

### Run Clean Script from package.json

```xml
<Target Name="RunCleanScript" BeforeTargets="ViteClean">
  <Exec Command="npm run clean" WorkingDirectory="$(ViteProjectRoot)" 
        ContinueOnError="true" />
</Target>
```

```json
// package.json
{
  "scripts": {
    "clean": "rimraf dist .vite"
  }
}
```

---

## Multi-Stage Build Pipelines

### Build Pipeline with Validation

```xml
<Target Name="PreBuild" BeforeTargets="ViteBuild">
  <Message Text="Running pre-build validation..." Importance="high" />
  
  <!-- Stage 1: Code generation -->
  <Exec Command="npm run codegen" WorkingDirectory="$(ViteProjectRoot)" />
  
  <!-- Stage 2: Linting -->
  <Exec Command="npm run lint" WorkingDirectory="$(ViteProjectRoot)" />
  
  <!-- Stage 3: Type checking -->
  <Exec Command="npm run type-check" WorkingDirectory="$(ViteProjectRoot)" />
</Target>

<Target Name="PostBuild" AfterTargets="ViteBuild">
  <Message Text="Running post-build tasks..." Importance="high" />
  
  <!-- Generate reports (release only) -->
  <Exec Command="npm run report" WorkingDirectory="$(ViteProjectRoot)" 
        Condition="'$(Configuration)' == 'Release'" />
</Target>
```

### Fail Build on Errors

```xml
<Target Name="LintStrict" BeforeTargets="ViteBuild">
  <Exec Command="npm run lint" 
        WorkingDirectory="$(ViteProjectRoot)"
        IgnoreExitCode="false" />
</Target>
```

### Continue Despite Errors

```xml
<Target Name="OptionalOptimization" AfterTargets="ViteBuild">
  <Exec Command="npm run optimize" 
        WorkingDirectory="$(ViteProjectRoot)"
        ContinueOnError="true" />
  
  <Warning Text="Optimization failed, continuing..." 
           Condition="'$(MSBuildLastTaskResult)' == 'false'" />
</Target>
```

---

## Passing Environment Variables

Pass MSBuild properties to your build scripts:

```xml
<PropertyGroup>
  <AppVersion>1.2.3</AppVersion>
</PropertyGroup>

<Target Name="SetBuildVersion" BeforeTargets="ViteBuild">
  <Exec Command="npm run build" 
        WorkingDirectory="$(ViteProjectRoot)"
        EnvironmentVariables="VITE_VERSION=$(AppVersion);VITE_BUILD_TIME=$([System.DateTime]::UtcNow.ToString('o'))" />
</Target>
```

Access in your code:
```typescript
console.log(import.meta.env.VITE_VERSION)
console.log(import.meta.env.VITE_BUILD_TIME)
```

---

## Complete Advanced Example

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <AppVersion>1.2.3</AppVersion>
  </PropertyGroup>

  <!-- ViteKit package -->
  <ItemGroup>
    <PackageReference Include="ViteKit.Msbuild" Version="*" />
  </ItemGroup>

  <!-- Environment-specific build scripts -->
  <PropertyGroup Condition="'$(Configuration)' == 'Debug'">
    <ViteBuildScript>build:dev</ViteBuildScript>
  </PropertyGroup>

  <PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <ViteBuildScript>build:prod</ViteBuildScript>
  </PropertyGroup>

  <!-- Pre-build validation -->
  <Target Name="PreBuild" BeforeTargets="ViteBuild">
    <Message Text="Running pre-build checks..." Importance="high" />
    <Exec Command="npm run type-check" WorkingDirectory="$(ViteProjectRoot)" />
    <Exec Command="npm run lint" WorkingDirectory="$(ViteProjectRoot)" />
  </Target>

  <!-- Post-build processing (release only) -->
  <Target Name="PostBuild" AfterTargets="ViteBuild" Condition="'$(Configuration)' == 'Release'">
    <Message Text="Running post-build optimization..." Importance="high" />
    <Exec Command="npm run compress" WorkingDirectory="$(ViteProjectRoot)" />
  </Target>

  <!-- Custom clean: remove build outputs -->
  <Target Name="CleanOutputs" AfterTargets="ViteClean">
    <Message Text="Cleaning Vite build outputs..." Importance="high" />
    <RemoveDir Directories="$(ViteOutputDir)" />
  </Target>
</Project>
```

With corresponding `package.json`:

```json
{
  "scripts": {
    "build": "vite build",
    "build:dev": "vite build --mode development --sourcemap",
    "build:prod": "vite build --mode production --minify",
    "type-check": "tsc --noEmit",
    "lint": "eslint .",
    "compress": "gzip-all dist"
  }
}
```

Usage:
```bash
# Debug build (runs build:dev)
dotnet build

# Release build (runs build:prod + compression)
dotnet build -c Release

# Clean (removes outputs + cache)
dotnet clean
```

---

## See Also

- [Configuration Reference](../configuration) - All available properties
- [API Reference](../api-reference) - Complete target reference
- [Troubleshooting](../troubleshooting) - Common issues
