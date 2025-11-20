---
layout: default
title: Best Practices
parent: Guides
nav_order: 5
---

# Best Practices
{: .fs-9 }

Best practices for configuring and using Vite.MsBuild in your ASP.NET Core projects.
{: .fs-6 .fw-300 }

## MSBuild Configuration

### Use Consistent Property Naming

Always prefix custom properties with `Vite` to avoid conflicts:

```xml
<PropertyGroup>
  <!-- Good: Prefixed with "Vite" -->
  <ViteConfigFile>vite.config.ts</ViteConfigFile>
  <ViteOutputDir>wwwroot/dist</ViteOutputDir>
  <ViteMode>production</ViteMode>
  
  <!-- Bad: Generic names may conflict -->
  <OutputDir>dist</OutputDir>  
  <Mode>production</Mode>
</PropertyGroup>
```

### Centralize Configuration with Directory.Build.props

For multi-project solutions, use `Directory.Build.props` at the solution root:

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <!-- Shared Vite.MsBuild settings for all projects -->
    <PackageManager>pnpm</PackageManager>
    <ViteBuildTiming>BeforeCSharp</ViteBuildTiming>
    <ViteMode Condition="'$(Configuration)' == 'Release'">production</ViteMode>
    <ViteMode Condition="'$(Configuration)' != 'Release'">development</ViteMode>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- Single source of truth for package version -->
    <PackageReference Include="Vite.MsBuild" Version="2.0.0" />
  </ItemGroup>
</Project>
```

### Configuration-Specific Settings

Map MSBuild configurations to Vite modes appropriately:

```xml
<PropertyGroup>
  <!-- Default -->
  <ViteMode Condition="'$(Configuration)' == 'Debug'">development</ViteMode>
  <ViteMode Condition="'$(Configuration)' == 'Release'">production</ViteMode>
  
  <!-- Custom configurations -->
  <ViteMode Condition="'$(Configuration)' == 'Staging'">staging</ViteMode>
  <ViteMode Condition="'$(Configuration)' == 'QA'">qa</ViteMode>
</PropertyGroup>
```

## Build Performance

### Choose Appropriate Build Timing

**BeforeCSharp (Default)** - Best for most scenarios:
```xml
<PropertyGroup>
  <ViteBuildTiming>BeforeCSharp</ViteBuildTiming>
</PropertyGroup>
```

**Benefits:**
- Frontend assets available before C# compilation
- Static web assets resolved correctly
- Better for projects that embed assets in DLLs

**AfterCSharp** - Use when frontend depends on C# outputs:
```xml
<PropertyGroup>
  <ViteBuildTiming>AfterCSharp</ViteBuildTiming>
</PropertyGroup>
```

**Benefits:**
- C# code runs first
- Useful if Vite config reads C# build artifacts
- Better for codegen scenarios

### Leverage Incremental Builds

Vite.MsBuild automatically tracks file changes. Help it by organizing files properly:

```xml
<ItemGroup>
  <!-- Include additional file patterns if needed -->
  <ViteInputFiles Include="ClientApp/**/*.vue" />
  <ViteInputFiles Include="ClientApp/**/*.tsx" />
  
  <!-- Exclude generated files from tracking -->
  <ViteInputFiles Remove="ClientApp/generated/**/*" />
</ItemGroup>
```

### Optimize for CI/CD

**Disable unnecessary features in CI:**

```xml
<PropertyGroup Condition="'$(CI)' == 'true'">
  <!-- Skip if already built -->
  <EnableViteBuild Condition="Exists('$(ViteOutputDir)')">false</EnableViteBuild>
  
  <!-- Use quiet verbosity -->
  <ViteBuildVerbosity>quiet</ViteBuildVerbosity>
</PropertyGroup>
```

**Use caching effectively:**

```yaml
# GitHub Actions example
- name: Cache Vite build
  uses: actions/cache@v3
  with:
    path: |
      wwwroot/dist
      obj/Vite.MsBuild.*.marker
    key: vite-${{ hashFiles('**/package-lock.json', 'vite.config.ts') }}
```

## Multi-Config Builds

### Use Clear BuildIds

```xml
<ItemGroup>
  <!-- Good: Descriptive BuildIds -->
  <ViteConfig Include="vite.admin.config.ts">
    <BuildId>admin</BuildId>
    <OutputDir>wwwroot/admin</OutputDir>
  </ViteConfig>
  
  <ViteConfig Include="vite.customer.config.ts">
    <BuildId>customer</BuildId>
    <OutputDir>wwwroot/customer</OutputDir>
  </ViteConfig>
</ItemGroup>
```

### Avoid Output Directory Conflicts

```xml
<ItemGroup>
  <!-- Bad: Same output directory -->
  <ViteConfig Include="vite.config1.ts">
    <OutputDir>wwwroot/dist</OutputDir>
  </ViteConfig>
  <ViteConfig Include="vite.config2.ts">
    <OutputDir>wwwroot/dist</OutputDir>  <!-- CONFLICT! -->
  </ViteConfig>
  
  <!-- Good: Separate output directories -->
  <ViteConfig Include="vite.config1.ts">
    <OutputDir>wwwroot/app1</OutputDir>
  </ViteConfig>
  <ViteConfig Include="vite.config2.ts">
    <OutputDir>wwwroot/app2</OutputDir>
  </ViteConfig>
</ItemGroup>
```

### Manage Dependencies Explicitly

```xml
<ItemGroup>
  <!-- Shared library must build first -->
  <ViteConfig Include="vite.shared.config.ts">
    <BuildId>shared</BuildId>
    <OutputDir>wwwroot/shared</OutputDir>
  </ViteConfig>
  
  <!-- Admin depends on shared -->
  <ViteConfig Include="vite.admin.config.ts">
    <BuildId>admin</BuildId>
    <OutputDir>wwwroot/admin</OutputDir>
    <DependsOn>shared</DependsOn>
  </ViteConfig>
</ItemGroup>
```

## Package Manager Selection

### Let Auto-Detection Work

**Best:** Don't specify, let Vite.MsBuild detect from lock files:

```xml
<PropertyGroup>
  <!-- No PackageManager specified - auto-detects -->
</PropertyGroup>
```

**Good:** Specify once in Directory.Build.props:

```xml
<!-- Directory.Build.props -->
<PropertyGroup>
  <PackageManager>pnpm</PackageManager>
</PropertyGroup>
```

**Avoid:** Inconsistent specifications across projects

### Commit Lock Files

Always commit your package manager's lock file:

```bash
# Commit ONE of these (the one you use):
git add package-lock.json  # npm
git add pnpm-lock.yaml     # pnpm  
git add yarn.lock          # yarn
git add bun.lockb          # bun
```

**Why:** Vite.MsBuild uses lock files to:
1. Auto-detect package manager
2. Ensure consistent builds across environments
3. Enable proper incremental builds

## Version Control

### Recommended .gitignore

```gitignore
# Vite build outputs
wwwroot/dist/
wwwroot/assets/
*.local

# MSBuild markers (incremental build tracking)
obj/Vite.MsBuild.*.marker
obj/NodeRestore.marker

# Node modules
node_modules/

# Package manager store
.pnpm-store/
.yarn/cache/
```

### Always Commit

```bash
# Configuration files
vite.config.ts
vite.*.config.ts
package.json
[package-manager]-lock.[extension]

# MSBuild integration
*.csproj
Directory.Build.props
```

### Never Commit

```bash
# Build artifacts
wwwroot/dist/
node_modules/
obj/
bin/

# Markers
*.marker
```

## Debugging and Diagnostics

### Enable Verbose Logging

```bash
# See detailed build information
dotnet build -v:d

# See everything (including MSBuild internals)
dotnet build -v:diag
```

### Use Diagnostic Mode

```xml
<PropertyGroup>
  <ViteShowDiagnostics>true</ViteShowDiagnostics>
</PropertyGroup>
```

Then build:
```bash
dotnet build
```

Output will show:
- Detected configurations
- Build order
- Dependency graph
- Effective modes
- File tracking details

### Check Incremental Build Status

MSBuild will show:
```
[SKIP] Configuration 'admin' (up to date)
[BUILD] Configuration 'customer' (files changed)
```

## Clean Builds

### Understanding Clean Behavior

```bash
# Removes Vite outputs and markers
dotnet clean

# Full rebuild
dotnet clean
dotnet build
```

### Custom Clean Targets

```xml
<Target Name="CleanViteCache" AfterTargets="Clean">
  <RemoveDir Directories="$(ViteProjectRoot)node_modules/.vite" />
  <Message Text="Cleared Vite cache" Importance="high" />
</Target>
```

## Common Pitfalls

### ❌ Don't Override EnableViteBuild Without Conditions

**Bad:**
```xml
<PropertyGroup>
  <EnableViteBuild>false</EnableViteBuild>
</PropertyGroup>
```

**Good:**
```xml
<PropertyGroup>
  <!-- Only disable for specific scenarios -->
  <EnableViteBuild Condition="'$(SkipFrontend)' == 'true'">false</EnableViteBuild>
</PropertyGroup>
```

### ❌ Don't Mix Build Timings Without Reason

**Bad:**
```xml
<!-- Project1.csproj -->
<ViteBuildTiming>BeforeCSharp</ViteBuildTiming>

<!-- Project2.csproj -->
<ViteBuildTiming>AfterCSharp</ViteBuildTiming>
```

Use consistent timing unless you have a specific reason.

### ❌ Don't Specify Absolute Paths

**Bad:**
```xml
<ViteProjectRoot>C:\Users\MyName\Projects\MyApp</ViteProjectRoot>
```

**Good:**
```xml
<!-- Let auto-detection work, or use relative paths -->
<ViteProjectRoot>$(MSBuildProjectDirectory)</ViteProjectRoot>
```

### ❌ Don't Ignore Build Warnings

Pay attention to messages like:
- "Configuration X has missing dependency Y"
- "Multiple configs writing to same output directory"
- "Package manager mismatch between project and detected"

## Performance Tuning

### Profile Your Builds

```bash
# See timing breakdown
dotnet build -bl:build.binlog

# View with Binary Log Viewer
# https://msbuildlog.com/
```

### Optimize Node Dependencies

```xml
<Target Name="OptimizeNodeModules" BeforeTargets="EnsureNodeDependencies">
  <Exec Command="$(PackageManager) prune" WorkingDirectory="$(ViteProjectRoot)" />
</Target>
```

### Parallel Builds

Vite.MsBuild is parallel-build safe. Use MSBuild parallelism:

```bash
# Build with maximum parallelism
dotnet build -m

# Specify number of parallel builds
dotnet build -m:4
```

## Migration from Other Systems

### From Custom Targets

If you have custom Vite targets, you can disable Vite.MsBuild selectively:

```xml
<PropertyGroup>
  <EnableViteBuild>false</EnableViteBuild>
</PropertyGroup>

<!-- Keep your custom targets -->
<Target Name="MyCustomViteBuild">
  <!-- Your logic -->
</Target>
```

### From NPM Scripts

Vite.MsBuild can coexist with npm scripts:

```json
{
  "scripts": {
    "dev": "vite",
    "build": "vite build",
    "preview": "vite preview"
  }
}
```

The package uses the `build` script by default, but you can override:

```xml
<PropertyGroup>
  <ViteBuildCommand>custom-build</ViteBuildCommand>
</PropertyGroup>
```

## Additional Resources

- [Multi-SPA Guide](multi-spa.md)
- [Monorepo Guide](monorepos.md)
- [Package Managers Guide](package-managers.md)
- [Advanced Scenarios](advanced-scenarios.md)
