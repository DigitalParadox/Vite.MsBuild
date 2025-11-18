# 🔗 **LinkDependencies Feature - Complete Implementation**

## ✅ **New Feature: Automatic npm Package Linking**

### **Problem Solved:**
When building multiple Vite configurations where one consumes another as an npm package, manually linking the packages was complex and error-prone.

### **Solution:**
Added `LinkDependencies="true"` opt-in feature that automatically links dependency packages using the `file:` protocol.

## 🎯 **Usage Examples**

### **Basic Package Linking**
```xml
<!-- Build shared component library -->
<ViteConfig 
    Name="shared-ui"
    ConfigFile="packages/shared-ui/vite.config.ts"
    OutputDir="dist/shared-ui"
    Mode="production" />

<!-- Main app consumes the shared library -->
<ViteConfig 
    Name="main-app"
    ConfigFile="apps/main/vite.config.ts"
    OutputDir="wwwroot/main"
    DependsOn="shared-ui"
    LinkDependencies="true" />
```

### **ItemGroup Syntax**
```xml
<ItemGroup>
  <ViteConfig Include="apps/mobile/vite.config.ts">
    <BuildId>mobile</BuildId>
    <DependsOn>shared-ui,shared-utils</DependsOn>
    <LinkDependencies>true</LinkDependencies>
  </ViteConfig>
</ItemGroup>
```

### **Complex Multi-Package Scenario**
```xml
<!-- Core packages -->
<ViteConfig Name="core" ConfigFile="packages/core/vite.config.ts" OutputDir="dist/core" />
<ViteConfig Name="utils" ConfigFile="packages/utils/vite.config.ts" OutputDir="dist/utils" />

<!-- Component library depends on core and utils -->
<ViteConfig 
    Name="components"
    ConfigFile="packages/components/vite.config.ts"
    OutputDir="dist/components"
    DependsOn="core,utils"
    LinkDependencies="true" />

<!-- Main application uses all packages -->
<ViteConfig 
    Name="main-app"
    ConfigFile="apps/main/vite.config.ts"
    OutputDir="wwwroot/main"
    DependsOn="core,utils,components"
    LinkDependencies="true" />
```

## 🔧 **How It Works**

### **1. Dependency Detection**
```csharp
// For each dependency in DependsOn
var depOutputPath = Path.GetFullPath(Path.Combine(ViteProjectRoot, depConfig.OutputDir));
var depPackageJsonPath = Path.Combine(depOutputPath, "package.json");

if (File.Exists(depPackageJsonPath))
{
    var packageName = GetPackageNameFromJson(depPackageJsonPath);
    // Link the package...
}
```

### **2. Package.json Manipulation**
```json
// Before build (temporary modification)
{
  "dependencies": {
    "@company/shared-ui": "file:../dist/shared-ui",
    "@company/shared-utils": "file:../dist/utils"
  }
}

// After build (restored to original)
{
  "dependencies": {
    "@company/shared-ui": "^1.0.0",
    "@company/shared-utils": "^1.0.0"
  }
}
```

### **3. Build Flow**
1. **Dependency builds complete** → packages output to their directories
2. **Before main build** → backup package.json, link dependencies using `file:` protocol
3. **Execute main build** → Vite/npm resolves dependencies from local file paths
4. **After main build** → restore original package.json from backup

## ✅ **Safety Features**

### **1. Non-Destructive**
- ✅ **Backup created** before any modifications
- ✅ **Original package.json restored** after build
- ✅ **Graceful failures** - build continues if linking fails

### **2. Conflict-Free**
- ✅ **No global state** - uses `file:` protocol instead of `npm link`
- ✅ **Project isolation** - each build has its own temporary links
- ✅ **No cleanup required** - file protocol is self-contained

### **3. Opt-in Behavior**
- ✅ **Disabled by default** - only activates when `LinkDependencies="true"`
- ✅ **Dependency auto-detection** - skips dependencies that don't produce packages
- ✅ **Backward compatible** - existing builds unaffected

## 📊 **Build Output Examples**

### **Successful Linking**
```
🔗 Linking dependency packages for 'main-app'
✅ Linked package '@company/shared-ui' -> file:../../dist/shared-ui
✅ Linked package '@company/utils' -> file:../../dist/utils
🔗 Successfully linked 2 dependency package(s) for 'main-app'
🔧 Building configuration: main-app
✅ Successfully completed build: main-app
```

### **Auto-Skip Non-Packages**
```
🔗 Linking dependency packages for 'main-app'
Dependency 'static-assets' does not produce npm package, skipping
✅ Linked package '@company/shared-ui' -> file:../../dist/shared-ui
🔗 Successfully linked 1 dependency package(s) for 'main-app'
```

### **Graceful Failure**
```
🔗 Linking dependency packages for 'main-app'
warning : Cannot determine package name for dependency 'malformed-dep'
✅ Linked package '@company/shared-ui' -> file:../../dist/shared-ui
🔗 Successfully linked 1 dependency package(s) for 'main-app'
```

## 🎯 **Use Cases**

### **1. Monorepo Development**
- Build shared packages first, then consuming applications
- No need for manual `npm link` or `yarn link` commands
- Clean, repeatable builds in CI/CD

### **2. Micro-Frontend Architecture**
- Build shared shell and components as packages
- Consuming micro-frontends automatically link to latest builds
- Consistent dependency versions across all micro-frontends

### **3. Component Library Development**
- Build design system/component library
- Demo applications automatically use latest component builds
- No version management during development

### **4. Multi-Tenant Applications**
- Build shared core functionality as packages
- Tenant-specific applications link to shared packages
- Isolated customization without duplication

## 🚀 **Benefits**

| **Aspect** | **Before** | **After** |
|------------|------------|-----------|
| **Setup** | Manual `npm link` commands | ✅ Automatic via `LinkDependencies="true"` |
| **CI/CD** | Complex linking scripts | ✅ Works out of the box |
| **Cleanup** | Manual unlink required | ✅ Automatic restoration |
| **Reliability** | Global state conflicts | ✅ Isolated per build |
| **Debugging** | Opaque linking errors | ✅ Clear logging and fallbacks |

This feature provides **enterprise-grade package linking** for complex Vite multi-configuration scenarios with zero manual intervention! 🎉