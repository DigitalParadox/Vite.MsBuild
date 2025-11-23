---
layout: default
title: API Reference
nav_order: 7
has_children: true
permalink: /api/
---

# API Reference
{: .fs-9 }

Complete reference for MSBuild properties, tasks, and configuration options.
{: .fs-6 .fw-300 }

## Quick Reference

### Essential Properties

| Property | Default | Description |
|:---------|:--------|:------------|
| `EnableViteBuild` | `true` | Enable/disable Vite builds |
| `ViteProjectRoot` | Auto-detected | Root directory for Vite project |
| `ViteOutputDir` | `wwwroot` | Output directory for built assets |
| `PackageManager` | Auto-detected | Package manager to use |
| `ViteMode` | Based on Configuration | Vite build mode |

### Common Configuration

```xml
<PropertyGroup>
  <!-- Disable Vite builds -->
  <EnableViteBuild>false</EnableViteBuild>
  
  <!-- Custom output directory -->
  <ViteOutputDir>wwwroot/assets</ViteOutputDir>
  
  <!-- Force specific package manager -->
  <PackageManager>pnpm</PackageManager>
  
  <!-- Override Vite mode -->
  <ViteMode>staging</ViteMode>
  
  <!-- Control build timing -->
  <ViteBuildTiming>AfterCSharp</ViteBuildTiming>
</PropertyGroup>
```

## Reference Sections

### MSBuild Properties
Complete list of all configurable MSBuild properties.

- Core properties
- Advanced properties  
- Conditional properties
- Environment-specific properties

### Task Reference
Documentation for all MSBuild tasks.

- Task inputs and outputs
- Error conditions
- Performance characteristics
- Usage examples

### Configuration Options
Comprehensive configuration scenarios.

- Single SPA configuration
- Monorepo configuration
- Custom build scripts
- Package manager selection

{: .note }
> This section provides the complete technical reference for all configuration options and MSBuild integration points.

## Common Patterns

### Zero Configuration
```xml
<PackageReference Include="ViteKit.Msbuild" Version="2.0.0" />
```

### Basic Configuration  
```xml
<PropertyGroup>
  <ViteOutputDir>wwwroot/dist</ViteOutputDir>
  <PackageManager>npm</PackageManager>
</PropertyGroup>
```

### Advanced Configuration
```xml
<ItemGroup>
  <!-- Multi-SPA configuration -->
  <ViteConfig Include="vite.config.ts">
    <BuildId>main</BuildId>
    <OutputDir>wwwroot/main</OutputDir>
    <Mode>production</Mode>
  </ViteConfig>
</ItemGroup>

<PropertyGroup>
  <!-- Custom build script -->
  <ViteBuildScript>build:production</ViteBuildScript>
  
  <!-- Direct Vite CLI calls -->
  <DirectViteBuild>true</DirectViteBuild>
  
  <!-- Enable diagnostic logging -->
  <ViteEnableDiagnostics>true</ViteEnableDiagnostics>
</PropertyGroup>
```

## For Developers

The API reference is essential for:

- Understanding all available configuration options
- Implementing custom build scenarios  
- Troubleshooting configuration issues
- Extending ViteKit.Msbuild functionality