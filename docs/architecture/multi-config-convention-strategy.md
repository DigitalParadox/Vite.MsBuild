# Multi-Config Convention Strategy

## 🎯 **Revised Convention Hierarchy (No Config Parsing):**

### **Priority 1: Explicit OutputDir** (Always wins)
```xml
<ViteConfig Include="Areas/Admin/vite.admin.config.ts">
  <OutputDir>wwwroot/admin</OutputDir>  <!-- User specified, use this -->
</ViteConfig>
```

### **Priority 2: BuildId Convention** (Areas pattern)
```xml
<ViteConfig Include="Areas/Admin/vite.admin.config.ts">
  <BuildId>admin</BuildId>
  <!-- Auto-convention: wwwroot/{BuildId}/ = wwwroot/admin/ -->
</ViteConfig>
```

### **Priority 3: Smart Path-Based Detection**
```xml
<!-- Areas pattern: Use centralized output -->
<ViteConfig Include="Areas/Admin/vite.admin.config.ts">
  <!-- Auto-detects: "Areas" folder = centralized output = wwwroot/admin/ -->
</ViteConfig>

<!-- Non-Areas pattern: Use local output -->
<ViteConfig Include="ClientApps/main-app/vite.config.ts">
  <!-- Auto-detects: Not "Areas" = local output = ClientApps/main-app/dist/ -->
</ViteConfig>
```

### **Priority 4: Config-Relative Fallback**
```xml
<ViteConfig Include="somewhere/vite.config.ts">
  <!-- Last resort: somewhere/dist/ -->
</ViteConfig>
```

---

## 🔧 **Smart Path-Based Implementation:**

### **Phase 1: Architectural Pattern Detection**

```xml
<Target Name="_DetectViteConfigArchitecture" BeforeTargets="_ResolveViteConfigConventions">
  
  <PropertyGroup>
    <!-- Detect if config is in Areas folder -->
    <_ConfigDirectory>$([System.IO.Path]::GetDirectoryName('$(ViteSingleConfigFile)'))</_ConfigDirectory>
    <_IsAreasPattern>$([System.String]::new('$(_ConfigDirectory)').Contains('\Areas\'))</_IsAreasPattern>
    
    <!-- Extract area name for Areas pattern -->
    <_AreaName Condition="'$(_IsAreasPattern)' == 'true'">$([System.IO.Path]::GetFileName('$(_ConfigDirectory)'))</_AreaName>
  </PropertyGroup>
  
</Target>
```

### **Phase 2: Enhanced Convention Detection**

```xml
<Target Name="_ResolveViteConfigConventions" 
        BeforeTargets="_ViteBuildSingleConfig"
        DependsOnTargets="_DetectViteConfigArchitecture">
  
  <!-- Priority 1: Explicit OutputDir (always wins) -->
  <PropertyGroup Condition="'$(ViteSingleOutputDir)' != ''">
    <_ResolvedOutputDir>$(ViteSingleOutputDir)</_ResolvedOutputDir>
  </PropertyGroup>
  
  <!-- Priority 2: BuildId convention (works for any pattern) -->
  <PropertyGroup Condition="'$(_ResolvedOutputDir)' == '' AND '$(ViteSingleBuildId)' != ''">
    <_ResolvedOutputDir>wwwroot/$(ViteSingleBuildId)</_ResolvedOutputDir>
  </PropertyGroup>
  
  <!-- Priority 3: Smart path-based detection -->
  <PropertyGroup Condition="'$(_ResolvedOutputDir)' == ''">
    
    <!-- Areas pattern: Use centralized wwwroot with area name -->
    <_ResolvedOutputDir Condition="'$(_IsAreasPattern)' == 'true'">wwwroot/$(_AreaName)</_ResolvedOutputDir>
    
    <!-- Non-Areas pattern: Use local dist folder -->
    <_ResolvedOutputDir Condition="'$(_IsAreasPattern)' != 'true'">$(_ConfigDirectory)/dist</_ResolvedOutputDir>
    
  </PropertyGroup>
  
  <!-- Priority 4: Final fallback -->
  <PropertyGroup Condition="'$(_ResolvedOutputDir)' == ''">
    <_ResolvedOutputDir>$(_ConfigDirectory)/dist</_ResolvedOutputDir>
  </PropertyGroup>
  
</Target>
```

### **Phase 3: Smart Input File Detection**

```xml
<Target Name="_ResolveViteConfigInputConventions" BeforeTargets="_ViteBuildSingleConfig">
  
  <PropertyGroup>
    <_ConfigDirectory>$([System.IO.Path]::GetDirectoryName('$(ViteSingleConfigFile)'))</_ConfigDirectory>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- Always include the config file itself -->
    <_ViteConfigInputFiles Include="$(ViteSingleConfigFile)" />
    
    <!-- Areas pattern: Look for area-specific sources + global wwwroot -->
    <ItemGroup Condition="'$(_IsAreasPattern)' == 'true'">
      <!-- Area-specific sources -->
      <_ViteConfigInputFiles Include="$(_ConfigDirectory)/src/**/*.ts;$(_ConfigDirectory)/src/**/*.tsx" />
      <_ViteConfigInputFiles Include="$(_ConfigDirectory)/src/**/*.vue;$(_ConfigDirectory)/src/**/*.svelte" />
      <_ViteConfigInputFiles Include="$(_ConfigDirectory)/src/**/*.css;$(_ConfigDirectory)/src/**/*.scss" />
      <_ViteConfigInputFiles Include="$(_ConfigDirectory)/assets/**/*" />
      
      <!-- Global wwwroot (shared resources) -->
      <_ViteConfigInputFiles Include="$(MSBuildProjectDirectory)/wwwroot/**/*.ts;$(MSBuildProjectDirectory)/wwwroot/**/*.tsx" />
      <_ViteConfigInputFiles Include="$(MSBuildProjectDirectory)/wwwroot/**/*.vue;$(MSBuildProjectDirectory)/wwwroot/**/*.svelte" />
      <_ViteConfigInputFiles Include="$(MSBuildProjectDirectory)/wwwroot/**/*.css;$(MSBuildProjectDirectory)/wwwroot/**/*.scss" />
    </ItemGroup>
    
    <!-- Non-Areas pattern: Focus on local sources only -->
    <ItemGroup Condition="'$(_IsAreasPattern)' != 'true'">
      <!-- Local sources relative to config -->
      <_ViteConfigInputFiles Include="$(_ConfigDirectory)/src/**/*.ts;$(_ConfigDirectory)/src/**/*.tsx" />
      <_ViteConfigInputFiles Include="$(_ConfigDirectory)/src/**/*.vue;$(_ConfigDirectory)/src/**/*.svelte" />
      <_ViteConfigInputFiles Include="$(_ConfigDirectory)/src/**/*.css;$(_ConfigDirectory)/src/**/*.scss" />
      <_ViteConfigInputFiles Include="$(_ConfigDirectory)/assets/**/*;$(_ConfigDirectory)/public/**/*" />
      
      <!-- Also check parent directories for common SPA patterns -->
      <_ViteConfigInputFiles Include="$(_ConfigDirectory)/../src/**/*.ts" />
      <_ViteConfigInputFiles Include="$(_ConfigDirectory)/../assets/**/*" />
    </ItemGroup>
    
    <!-- Always include package manager files -->
    <_ViteConfigInputFiles Include="$(ViteProjectRoot)package.json" />
    <!-- Package manager specific lock file added in _ResolveViteConfigInputs -->
    
  </ItemGroup>
  
</Target>
```

---

## 📁 **Supported Directory Structures with Smart Conventions:**

### **Areas Pattern (ASP.NET MVC Areas):**
```
MyProject/
├── Areas/
│   ├── Admin/
│   │   ├── src/              ← Area-specific sources
│   │   │   ├── main.ts
│   │   │   └── AdminApp.vue
│   │   ├── assets/           ← Area-specific assets
│   │   └── vite.admin.config.ts
│   └── Customer/
│       ├── src/
│       └── vite.customer.config.ts
├── wwwroot/
│   ├── admin/               ← Smart convention: wwwroot/{area-name}/
│   └── customer/
└── package.json             ← Shared dependencies
```

**Configuration (minimal):**
```xml
<ViteConfig Include="Areas/Admin/vite.admin.config.ts" />
<!-- Auto-detects: Areas pattern → wwwroot/Admin/ output -->
<!-- Auto-detects: Areas/Admin/src/ + wwwroot/ inputs -->
```

### **SPA Subfolder Pattern (Independent Apps):**
```
MyProject/
├── ClientApps/
│   ├── main-app/
│   │   ├── src/              ← App-specific sources
│   │   ├── dist/             ← Smart convention: local dist/
│   │   ├── package.json      ← Independent dependencies
│   │   └── vite.config.ts
│   └── admin-portal/
│       ├── src/
│       ├── dist/             ← Smart convention: local dist/
│       ├── package.json
│       └── vite.config.ts
```

**Configuration (minimal):**
```xml
<ViteConfig Include="ClientApps/main-app/vite.config.ts" />
<!-- Auto-detects: Non-Areas pattern → ClientApps/main-app/dist/ output -->
<!-- Auto-detects: ClientApps/main-app/src/ inputs only -->
```

### **Mixed Pattern (Flexibility):**
```
MyProject/
├── Areas/Admin/vite.config.ts           ← Areas pattern
├── ClientApps/spa/vite.config.ts        ← SPA pattern  
└── wwwroot/simple/vite.config.ts        ← Explicit pattern
```

**Configuration:**
```xml
<ViteConfig Include="Areas/Admin/vite.config.ts" />
<!-- Smart: wwwroot/Admin/ -->

<ViteConfig Include="ClientApps/spa/vite.config.ts" />  
<!-- Smart: ClientApps/spa/dist/ -->

<ViteConfig Include="wwwroot/simple/vite.config.ts">
  <BuildId>simple</BuildId>
</ViteConfig>
<!-- Explicit: wwwroot/simple/ (BuildId overrides path detection) -->
```

---

## 🧪 **Test Cases for Smart Path-Based Conventions:**

### **🔴 Critical - Architectural Pattern Detection:**

```csharp
[Fact]
public void Areas_Pattern_Should_Use_Centralized_wwwroot_Output()
{
    // Given: ViteConfig at "Areas/Admin/vite.admin.config.ts"
    // When: No explicit OutputDir or BuildId
    // Expected: Should auto-detect Areas pattern and use "wwwroot/Admin/"
}

[Fact]
public void Non_Areas_Pattern_Should_Use_Local_Dist_Output()
{
    // Given: ViteConfig at "ClientApps/main-app/vite.config.ts"  
    // When: No explicit OutputDir or BuildId
    // Expected: Should auto-detect SPA pattern and use "ClientApps/main-app/dist/"
}

[Fact]
public void Explicit_OutputDir_Should_Override_All_Path_Detection()
{
    // Given: ViteConfig in Areas/ + explicit OutputDir
    // When: Building
    // Expected: Should use explicit OutputDir, ignore Areas convention
}
```

### **🔴 Critical - Convention Priority:**

```csharp
[Theory]
[InlineData("Areas/Admin/vite.config.ts", "", "wwwroot/Admin")]           // Areas convention
[InlineData("ClientApps/spa/vite.config.ts", "", "ClientApps/spa/dist")] // SPA convention  
[InlineData("Areas/Admin/vite.config.ts", "admin", "wwwroot/admin")]     // BuildId override
[InlineData("Areas/Admin/vite.config.ts", "custom/output", "custom/output")] // Explicit override
public void Should_Apply_Convention_Priority_Correctly(string configPath, string override, string expectedOutput)
{
    // Test the full convention hierarchy priority
}
```

### **🟡 Important - Input File Detection:**

```csharp
[Fact]
public void Areas_Pattern_Should_Include_Area_Sources_Plus_Global_wwwroot()
{
    // Given: Areas/Admin/vite.config.ts
    // Expected inputs: 
    // - Areas/Admin/src/**/*
    // - wwwroot/**/* (shared resources)
    // - Relevant package manager lock file
}

[Fact]
public void SPA_Pattern_Should_Include_Only_Local_Sources()
{
    // Given: ClientApps/main-app/vite.config.ts
    // Expected inputs:
    // - ClientApps/main-app/src/**/*
    // - ClientApps/main-app/assets/**/*
    // - NO wwwroot/**/* (independent app)
}
```

### **🟢 Nice to Have - Edge Cases:**

```csharp
[Fact]
public void Should_Handle_Deeply_Nested_Areas_Structure()
{
    // Given: Areas/Admin/SubModule/vite.config.ts
    // Expected: Should still detect Areas pattern and use SubModule as area name
}

[Fact]
public void Should_Handle_Case_Insensitive_Areas_Detection()
{
    // Given: areas/admin/vite.config.ts (lowercase)
    // Expected: Should still detect Areas pattern
}
```

---

## ✅ **Benefits of Smart Path-Based Conventions:**

### **🎯 Simplicity**
```xml
<!-- Before: Explicit everything -->
<ViteConfig Include="Areas/Admin/vite.admin.config.ts">
  <BuildId>admin</BuildId>
  <OutputDir>wwwroot/admin</OutputDir>
  <InputPattern>Areas/Admin/src</InputPattern>
</ViteConfig>

<!-- After: Smart conventions -->
<ViteConfig Include="Areas/Admin/vite.admin.config.ts" />
<!-- Auto-detects everything! -->
```

### **🏗️ Architectural Awareness**
- ✅ **Understands ASP.NET Areas** - Centralized outputs for serving
- ✅ **Understands SPA patterns** - Independent local outputs  
- ✅ **Flexible override** - Explicit settings always win

### **🚀 Developer Experience**
- ✅ **Zero configuration** for common patterns
- ✅ **Predictable behavior** based on folder structure
- ✅ **Easy migration** from manual Vite setups

### **🔧 Maintainability**
- ✅ **No config file parsing** - Simple path-based logic
- ✅ **No brittle heuristics** - Clear architectural patterns
- ✅ **Easy to test** - Deterministic based on file paths

## 🎯 **Final Recommendation:**

**Default Experience (Zero Config):**
```xml
<!-- Areas: Centralized output -->
<ViteConfig Include="Areas/Admin/vite.admin.config.ts" />

<!-- SPAs: Local output -->  
<ViteConfig Include="ClientApps/main-app/vite.config.ts" />
```

**Power User Experience (Full Control):**
```xml
<ViteConfig Include="Areas/Admin/vite.admin.config.ts">
  <BuildId>admin-portal</BuildId>
  <OutputDir>wwwroot/admin-portal</OutputDir>
  <PackageManager>yarn</PackageManager>
</ViteConfig>
```

This gives both **zero-config simplicity** AND **full customization power** based on well-understood architectural patterns! 🎯