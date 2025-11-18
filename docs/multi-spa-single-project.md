# Multi-SPA Single Project Architecture

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
  
  <PackageReference Include="Vite.MsBuild" Version="1.0.0" />
  
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
<ViteBuildMarker>$(IntermediateOutputPath)$(TargetFramework).Vite.MsBuild.Build.marker</ViteBuildMarker>

<!-- Enhanced: Per-SPA markers -->
<ViteBuildMarker>$(IntermediateOutputPath)$(TargetFramework).Vite.MsBuild.%(ViteConfigs.BuildId).marker</ViteBuildMarker>
```

**Results in:**
```
obj/net8.0.Vite.MsBuild.admin.marker      ← Admin SPA build state
obj/net8.0.Vite.MsBuild.customer.marker   ← Customer SPA build state  
obj/net8.0.Vite.MsBuild.partner.marker    ← Partner SPA build state
```

### **Enhanced MSBuild Targets**

```xml
<!-- Multi-config build target -->
<Target Name="ViteBuildMultipleConfigs" 
        Condition="'@(ViteConfigs)' != ''" 
        Inputs="@(ViteConfigs);@(ViteInputFiles)" 
        Outputs="@(ViteConfigs->'$(IntermediateOutputPath)$(TargetFramework).Vite.MsBuild.%(BuildId).marker')">
  
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
  <Touch Files="$(IntermediateOutputPath)$(TargetFramework).Vite.MsBuild.$(ViteSingleBuildId).marker" 
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

This would address the **very real enterprise need** for multiple SPAs within a single ASP.NET Core project! 🎯