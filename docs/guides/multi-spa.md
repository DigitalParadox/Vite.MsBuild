# Multi-SPA Single Project Architecture

{: .highlight }
> **Fully Implemented**: Multi-SPA configuration via `<ViteConfig>` ItemGroup is production-ready with full test coverage.

## Real Enterprise Scenario 🏢

**Single ASP.NET Core project hosting multiple SPAs:**

```
Enterprise.Web/
├── Enterprise.Web.csproj           ← Single project
├── Areas/
│   ├── Admin/
│   │   ├── Views/                  ← MVC views
│   │   ├── spa/                    ← Vue 3 admin SPA
│   │   └── vite.config.admin.ts    ← Admin-specific config
│   │
│   ├── Customer/
│   │   ├── Views/                  ← MVC views  
│   │   ├── spa/                    ← React customer portal
│   │   └── vite.config.customer.ts ← Customer-specific config
│   │
│   └── Partner/
│       ├── Views/                  ← MVC views
│       ├── spa/                    ← Angular partner dashboard
│       └── vite.config.partner.ts  ← Partner-specific config
│
├── wwwroot/
│   ├── admin/                      ← Admin SPA output
│   ├── customer/                   ← Customer SPA output
│   └── partner/                    ← Partner SPA output
│
├── package.json                    ← Shared dependencies + workspace
└── Controllers/                    ← Shared backend controllers
```

## Current Challenge: Single ViteConfigFile 🚫

**Our current system only supports one config per project:**
```xml
<ViteConfigFile>vite.config.ts</ViteConfigFile>  <!-- Only ONE config -->
```

## Proposed Solution: Multi-Config Support 🔧

### **Option 1: ItemGroup-Based Multi-Config (Recommended)**

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  
  <PackageReference Include="ViteKit.Msbuild" Version="1.0.0" />
  
  <!-- Multiple Vite configurations -->
  <ItemGroup>
    <ViteConfigs Include="Areas\Admin\vite.config.admin.ts">
      <OutputPath>wwwroot\admin</OutputPath>
      <Mode>development</Mode>
      <BuildId>admin</BuildId>
    </ViteConfigs>
    
    <ViteConfigs Include="Areas\Customer\vite.config.customer.ts">
      <OutputPath>wwwroot\customer</OutputPath>
      <Mode>production</Mode>
      <BuildId>customer</BuildId>
    </ViteConfigs>
    
    <ViteConfigs Include="Areas\Partner\vite.config.partner.ts">
      <OutputPath>wwwroot\partner</OutputPath>
      <Mode>development</Mode>
      <BuildId>partner</BuildId>
    </ViteConfigs>
  </ItemGroup>
</Project>
```

### **Option 2: Configuration-Based Multi-Config**

```xml
<PropertyGroup>
  <!-- Enable multi-config mode -->
  <ViteMultiConfigMode>true</ViteMultiConfigMode>
</PropertyGroup>

<ItemGroup>
  <!-- Define SPA configurations -->
  <ViteSpaConfig Include="Admin">
    <ConfigFile>Areas\Admin\vite.config.admin.ts</ConfigFile>
    <OutputDir>wwwroot\admin</OutputDir>
    <Mode>development</Mode>
    <Framework>Vue</Framework>
  </ViteSpaConfig>
  
  <ViteSpaConfig Include="Customer">
    <ConfigFile>Areas\Customer\vite.config.customer.ts</ConfigFile>
    <OutputDir>wwwroot\customer</OutputDir>
    <Mode>production</Mode>
    <Framework>React</Framework>
  </ViteSpaConfig>
</ItemGroup>
```

## Implementation Strategy 🛠️

### **Enhanced Marker File System**

Each SPA gets its own marker file:
```xml
<!-- Current single marker -->
<ViteBuildMarker>$(IntermediateOutputPath)$(TargetFramework).ViteKit.Msbuild.Build.marker</ViteBuildMarker>

<!-- Enhanced: Per-SPA markers -->
<ViteBuildMarker>$(IntermediateOutputPath)$(TargetFramework).ViteKit.Msbuild.%(ViteConfigs.BuildId).marker</ViteBuildMarker>
```

**Results in:**
```
obj/net8.0.ViteKit.Msbuild.admin.marker      ← Admin SPA build state
obj/net8.0.ViteKit.Msbuild.customer.marker   ← Customer SPA build state  
obj/net8.0.ViteKit.Msbuild.partner.marker    ← Partner SPA build state
```

### **Enhanced MSBuild Targets**

```xml
<!-- Multi-config build target -->
<Target Name="ViteBuildMultipleConfigs" 
        Condition="'@(ViteConfigs)' != ''" 
        Inputs="@(ViteConfigs);@(ViteInputFiles)" 
        Outputs="@(ViteConfigs->'$(IntermediateOutputPath)$(TargetFramework).ViteKit.Msbuild.%(BuildId).marker')">
  
  <!-- Build each config sequentially or in parallel -->
  <MSBuild Projects="$(MSBuildProjectFile)" 
           Targets="_ViteBuildSingleConfig"
           Properties="ViteSingleConfigFile=%(ViteConfigs.Identity);ViteSingleOutputPath=%(ViteConfigs.OutputPath);ViteSingleMode=%(ViteConfigs.Mode);ViteSingleBuildId=%(ViteConfigs.BuildId)"
           BuildInParallel="true" />
</Target>

<Target Name="_ViteBuildSingleConfig">
  <!-- Build logic for a single Vite config -->
  <Exec Command="$(PackageManager) run build -- --config &quot;$(ViteSingleConfigFile)&quot; --mode $(ViteSingleMode) --outDir &quot;$(ViteSingleOutputPath)&quot;" 
        WorkingDirectory="$(ViteProjectRoot)" />
  
  <!-- Write marker file -->
  <Touch Files="$(IntermediateOutputPath)$(TargetFramework).ViteKit.Msbuild.$(ViteSingleBuildId).marker" 
         AlwaysCreate="true" />
</Target>
```

### **Incremental Build Logic**

Each SPA gets independent incremental builds:

```xml
<!-- Collect inputs per config -->
<Target Name="_CollectViteInputsPerConfig">
  <ItemGroup>
    <ViteInputFiles Include="Areas\Admin\spa\**\*" Condition="'$(ViteSingleBuildId)' == 'admin'" />
    <ViteInputFiles Include="Areas\Customer\spa\**\*" Condition="'$(ViteSingleBuildId)' == 'customer'" />
    <ViteInputFiles Include="Areas\Partner\spa\**\*" Condition="'$(ViteSingleBuildId)' == 'partner'" />
  </ItemGroup>
</Target>
```

## Benefits of Multi-SPA Support ✅

### **1. Independent Technology Stacks** 🎯
```typescript
// vite.config.admin.ts (Vue 3)
export default defineConfig({
  plugins: [vue()],
  build: { outDir: 'wwwroot/admin' }
})

// vite.config.customer.ts (React)  
export default defineConfig({
  plugins: [react()],
  build: { outDir: 'wwwroot/customer' }
})

// vite.config.partner.ts (Angular-like setup)
export default defineConfig({
  plugins: [/* custom Angular setup */],
  build: { outDir: 'wwwroot/partner' }
})
```

### **2. Independent Build Optimization** ⚡
- Admin SPA: Development mode (fast builds, debugging)
- Customer SPA: Production mode (optimized, minified)
- Partner SPA: Staging mode (source maps + optimization)

### **3. Selective Building** 🔧
```bash
# Build only admin SPA
dotnet build -p:ViteBuildFilter=admin

# Build only customer + partner SPAs
dotnet build -p:ViteBuildFilter="customer;partner"
```

### **4. Independent Incremental Builds** 📊
- Admin files change → Only admin SPA rebuilds
- Customer files change → Only customer SPA rebuilds
- Shared dependencies change → All SPAs rebuild

## Real-World Usage Examples 🌍

### **Enterprise Portal Architecture**
```xml
<ViteSpaConfig Include="AdminDashboard">
  <ConfigFile>Areas\Admin\vite.config.ts</ConfigFile>
  <OutputDir>wwwroot\admin</OutputDir>
  <Mode>development</Mode>
  <Features>Vue3,TypeScript,Vite4</Features>
</ViteSpaConfig>

<ViteSpaConfig Include="CustomerPortal">
  <ConfigFile>Areas\Customer\vite.config.ts</ConfigFile>
  <OutputDir>wwwroot\customer</OutputDir>
  <Mode>production</Mode>
  <Features>React18,JavaScript,PWA</Features>
</ViteSpaConfig>

<ViteSpaConfig Include="PartnerApi">
  <ConfigFile>Areas\Partner\vite.config.ts</ConfigFile>
  <OutputDir>wwwroot\partner</OutputDir>
  <Mode>production</Mode>
  <Features>Vanilla,TypeScript,WebComponents</Features>
</ViteSpaConfig>
```

### **Micro-Frontend Architecture**
```xml
<ViteSpaConfig Include="Shell">
  <ConfigFile>MicroFrontends\Shell\vite.config.ts</ConfigFile>
  <OutputDir>wwwroot\shell</OutputDir>
  <Mode>development</Mode>
  <Type>ModuleFederation</Type>
</ViteSpaConfig>

<ViteSpaConfig Include="ProductCatalog">
  <ConfigFile>MicroFrontends\Products\vite.config.ts</ConfigFile>
  <OutputDir>wwwroot\mf\products</OutputDir>
  <Mode>production</Mode>
  <Type>Remote</Type>
</ViteSpaConfig>
```

## Implementation Priority 📋

**Phase 1: Basic Multi-Config Support**
- ItemGroup-based configuration
- Sequential build execution
- Per-config marker files

**Phase 2: Advanced Features**
- Parallel build execution
- Selective building by filter
- Cross-SPA dependency detection

**Phase 3: Enterprise Features**
- Micro-frontend support
- Hot reload for multi-SPA development
- Advanced caching strategies

This addresses the **very real enterprise need** for multiple SPAs within a single ASP.NET Core project! 🎯

---

## Implementation Status

✅ **Fully Implemented and Tested**

The multi-SPA feature is production-ready with:
- ✅ ItemGroup-based configuration via `<ViteConfig Include="...">`
- ✅ Per-config metadata (BuildId, OutputDir, Mode, DependsOn)
- ✅ Dependency resolution and topological sorting
- ✅ Per-config incremental builds with marker files
- ✅ Comprehensive test coverage in E2E tests
- ✅ Mode override hierarchy (config > global > default)

### Current Capabilities

**Basic Multi-Config**
```xml
<ItemGroup>
  <ViteConfig Include="vite.admin.config.ts" BuildId="admin" />
  <ViteConfig Include="vite.customer.config.ts" BuildId="customer" />
</ItemGroup>
```

**With Dependencies**
```xml
<ItemGroup>
  <ViteConfig Include="vite.shared.config.ts" BuildId="shared" />
  <ViteConfig Include="vite.app.config.ts" BuildId="app" DependsOn="shared" />
</ItemGroup>
```

**With Custom Outputs and Modes**
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

### Real-World Usage

See the E2E test projects for working examples:
- `tests/ViteKit.MsBuild.E2ETests/TestProjects/MultiSpaReact/` - Multi-SPA with React
- `tests/ViteKit.MsBuild.E2ETests/TestProjects/MonorepoShared/` - Shared dependencies
- `tests/ViteKit.MsBuild.E2ETests/TestProjects/OutputDirOverride/` - Custom outputs