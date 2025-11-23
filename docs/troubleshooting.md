---
layout: default
title: Troubleshooting
nav_order: 5
---

# Troubleshooting
{: .no_toc }

Solutions to common issues with ViteKit.Msbuild.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Build Issues

### Vite Not Running

**Symptom**: No Vite output after `dotnet build`

**Solutions**:

1. Verify `EnableViteBuild` is `true`:
   ```xml
   <PropertyGroup>
     <EnableViteBuild>true</EnableViteBuild>
   </PropertyGroup>
   ```

2. Check for `package.json` in project root:
   ```bash
   ls package.json  # Should exist
   ```

3. Increase verbosity to see details:
   ```bash
   dotnet build -v:detailed
   ```

---

### Dependencies Not Installing

**Symptom**: `node_modules not found` error

**Solutions**:

1. Manually install once:
   ```bash
   npm install
   ```

2. Check lock file exists (npm, yarn, pnpm, or bun)

3. Verify package manager is in PATH:
   ```bash
   npm --version
   yarn --version
   pnpm --version
   bun --version
   ```

---

### Incremental Builds Not Working

**Symptom**: Vite rebuilds every time even without changes

**Solutions**:

1. Check `obj/` directory permissions (builds store markers here)

2. Clean and rebuild:
   ```bash
   dotnet clean
   dotnet build
   ```

3. Verify file tracking:
   ```bash
   dotnet build -v:detailed | grep ViteInputFiles
   ```

---

## Configuration Issues

### Wrong Package Manager Used

**Symptom**: ViteKit uses npm but you want pnpm

**Solution**: Force the package manager:
```xml
<PropertyGroup>
  <PackageManager>pnpm</PackageManager>
</PropertyGroup>
```

---

### Custom Vite Config Not Found

**Symptom**: `Could not find vite.config.ts`

**Solutions**:

1. Specify the config file explicitly:
   ```xml
   <PropertyGroup>
     <ViteConfigFile>config/vite.config.ts</ViteConfigFile>
   </PropertyGroup>
   ```

2. Check file extension (`.ts`, `.js`, `.mjs` are all supported)

---

### Output Directory Issues

**Symptom**: Assets not appearing in `wwwroot`

**Solutions**:

1. Check Vite output matches ViteKit expectation:
   ```typescript
   // vite.config.ts
   export default defineConfig({
     build: {
       outDir: 'wwwroot',  // Must match ViteOutputDir
       emptyOutDir: true
     }
   })
   ```

2. Verify `ViteOutputDir` setting:
   ```xml
   <PropertyGroup>
     <ViteOutputDir>wwwroot</ViteOutputDir>
   </PropertyGroup>
   ```

---

## Multi-Configuration Issues

### Configs Building Out of Order

**Symptom**: Dependencies between configs not respected

**Solution**: ViteKit auto-detects dependencies. Ensure configs are properly named:
```xml
<ItemGroup>
  <!-- This builds first (no dependencies) -->
  <ViteConfiguration Include="vite.shared.config.ts" />
  
  <!-- This builds second (depends on shared) -->
  <ViteConfiguration Include="vite.app.config.ts" />
</ItemGroup>
```

---

### Duplicate Outputs

**Symptom**: Multiple configs overwrite each other

**Solution**: Use distinct output directories:
```xml
<ItemGroup>
  <ViteConfiguration Include="vite.main.config.ts">
    <OutputDir>wwwroot/main</OutputDir>
  </ViteConfiguration>
  
  <ViteConfiguration Include="vite.admin.config.ts">
    <OutputDir>wwwroot/admin</OutputDir>
  </ViteConfiguration>
</ItemGroup>
```

---

## Performance Issues

### Slow Builds

**Symptoms**: Builds take longer than expected

**Solutions**:

1. **Disable source maps in production**:
   ```typescript
   export default defineConfig({
     build: {
       sourcemap: false  // Or 'hidden'
     }
   })
   ```

2. **Use faster package manager**:
   ```xml
   <PackageManager>pnpm</PackageManager>  <!-- or bun -->
   ```

3. **Exclude unnecessary files**:
   ```xml
   <ItemGroup>
     <ViteInputFiles Remove="legacy/**/*" />
     <ViteInputFiles Remove="test/**/*" />
   </ItemGroup>
   ```

---

### Parallel Build Conflicts

**Symptom**: `npm install` errors in parallel builds

**Solution**: ViteKit handles this automatically with marker files. If issues persist:

```xml
<PropertyGroup>
  <DisableParallelProjectBuilds>true</DisableParallelProjectBuilds>
</PropertyGroup>
```

---

## Monorepo Issues

### Can't Find Dependencies

**Symptom**: Imports fail even though packages are installed

**Solution**: Ensure workspace setup is correct:

```json
// Root package.json
{
  "workspaces": [
    "src/*"
  ]
}
```

Or for pnpm:
```yaml
# pnpm-workspace.yaml
packages:
  - 'src/*'
```

---

## Diagnostic Commands

### Get Full Build Logs

```bash
dotnet build -v:diagnostic > build.log 2>&1
```

### Show ViteKit Configuration

```bash
dotnet build -v:detailed | grep -i vite
```

### Verify Package Installation

```bash
dotnet list package | grep ViteKit
```

---

## Still Need Help?

1. **Check Examples**: See if your scenario is covered in [Examples](examples)
2. **GitHub Issues**: [Report a bug](https://github.com/DigitalParadox/ViteKit/issues)
3. **Discussions**: [Ask the community](https://github.com/DigitalParadox/ViteKit/discussions)

When reporting issues, include:
- ViteKit.Msbuild version
- .NET version (`dotnet --version`)
- Node.js version (`node --version`)
- Package manager and version
- Full build log (`dotnet build -v:diagnostic`)
