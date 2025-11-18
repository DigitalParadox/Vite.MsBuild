# ViteConfig Per-Instance Build Task Architecture

## 🎯 **Solution: Per-Config Build Task Isolation**

We need to restructure our architecture so that **each ViteConfig creates its own isolated build task** with:

1. **Config-specific package manager detection**
2. **Config-specific input files (including only relevant lock file)**  
3. **Config-specific marker files**
4. **Independent incremental build logic**

---

## 🔧 **Current Architecture Issues**

### **Problem 1: Shared Global Package Manager**
```xml
<!-- Current: Global detection affects all configs -->
<PropertyGroup>
  <PackageManager Condition="'$(PackageManager)' == ''">npm</PackageManager>  <!-- Global -->
</PropertyGroup>

<!-- All configs use the same PackageManager -->
<BuildViteCommand PackageManagerName="$(PackageManager)" />
```

### **Problem 2: Shared Global ViteInputFiles**
```xml
<!-- Current: All configs see ALL lock files -->
<Target Name="_ViteBuildSingleConfig"
        Inputs="@(ViteInputFiles);$(ViteSingleConfigFile)"  <!-- Global inputs -->
        Outputs="$(ViteSingleMarkerFile)">
```

### **Problem 3: No Per-Config Package Manager Override**
```xml
<!-- Missing: Config-specific package manager -->
<ViteConfigs Include="Admin">
  <PackageManager>npm</PackageManager>     <!-- Should override global -->
</ViteConfigs>
<ViteConfigs Include="Customer">  
  <PackageManager>yarn</PackageManager>    <!-- Should be independent -->
</ViteConfigs>
```

---

## ✅ **Proposed Solution Architecture**

### **Phase 1: Per-Config Package Manager Resolution**

```xml
<!-- Enhanced _ViteConfigsToProcess with package manager resolution -->
<ItemGroup>
    <_ViteConfigsToProcess Include="@(ViteConfigs)">
        <MarkerFile>$(IntermediateOutputPath)$(TargetFramework).Vite.%(BuildId).%(ConfigPackageManager).marker</MarkerFile>
        
        <!-- Config-specific package manager resolution hierarchy -->
        <!-- 1. Explicit per-config override -->
        <ConfigPackageManager Condition="'%(ViteConfigs.PackageManager)' != ''">%(ViteConfigs.PackageManager)</ConfigPackageManager>
        
        <!-- 2. Config-specific property (e.g., AdminPackageManager) -->
        <ConfigPackageManager Condition="'%(ViteConfigs.PackageManager)' == '' AND '$(%(ViteConfigs.BuildId)PackageManager)' != ''">$($(%(ViteConfigs.BuildId)PackageManager))</ConfigPackageManager>
        
        <!-- 3. Global PackageManager property -->
        <ConfigPackageManager Condition="'%(ViteConfigs.PackageManager)' == '' AND '$(%(ViteConfigs.BuildId)PackageManager)' == ''">$(PackageManager)</ConfigPackageManager>
        
        <!-- 4. Auto-detect if not specified (handled in target) -->
        <ConfigPackageManager Condition="'%(ViteConfigs.PackageManager)' == '' AND '$(%(ViteConfigs.BuildId)PackageManager)' == '' AND '$(PackageManager)' == ''">auto-detect</ConfigPackageManager>
    </_ViteConfigsToProcess>
</ItemGroup>
```

### **Phase 2: Per-Config Input File Resolution**

```xml
<!-- New target: Resolve config-specific inputs before build -->
<Target Name="_ResolveViteConfigInputs" 
        BeforeTargets="_ViteBuildSingleConfig">

    <!-- Auto-detect package manager if needed -->
    <PropertyGroup Condition="'$(ViteSingleConfigPackageManager)' == 'auto-detect'">
        <ViteSingleConfigPackageManager Condition="Exists('$(ViteProjectRoot)bun.lockb')">bun</ViteSingleConfigPackageManager>
        <ViteSingleConfigPackageManager Condition="'$(ViteSingleConfigPackageManager)' == '' AND Exists('$(ViteProjectRoot)pnpm-lock.yaml')">pnpm</ViteSingleConfigPackageManager>
        <ViteSingleConfigPackageManager Condition="'$(ViteSingleConfigPackageManager)' == '' AND Exists('$(ViteProjectRoot)yarn.lock')">yarn</ViteSingleConfigPackageManager>
        <ViteSingleConfigPackageManager Condition="'$(ViteSingleConfigPackageManager)' == '' AND Exists('$(ViteProjectRoot)package-lock.json')">npm</ViteSingleConfigPackageManager>
        <ViteSingleConfigPackageManager Condition="'$(ViteSingleConfigPackageManager)' == ''">npm</ViteSingleConfigPackageManager>
    </PropertyGroup>

    <!-- Build config-specific input list -->
    <ItemGroup>
        <!-- Start with base ViteInputFiles (source files, assets, etc.) -->
        <_ViteConfigInputFiles Include="@(ViteInputFiles)" 
                               Exclude="$(ViteProjectRoot)*.lock*;$(ViteProjectRoot)package-lock.json;$(ViteProjectRoot)bun.lockb" />
        
        <!-- Add only the relevant lock file for this config's package manager -->
        <_ViteConfigInputFiles Include="$(ViteProjectRoot)package-lock.json" 
                               Condition="'$(ViteSingleConfigPackageManager)' == 'npm' AND Exists('$(ViteProjectRoot)package-lock.json')" />
        <_ViteConfigInputFiles Include="$(ViteProjectRoot)yarn.lock" 
                               Condition="'$(ViteSingleConfigPackageManager)' == 'yarn' AND Exists('$(ViteProjectRoot)yarn.lock')" />
        <_ViteConfigInputFiles Include="$(ViteProjectRoot)pnpm-lock.yaml" 
                               Condition="'$(ViteSingleConfigPackageManager)' == 'pnpm' AND Exists('$(ViteProjectRoot)pnpm-lock.yaml')" />
        <_ViteConfigInputFiles Include="$(ViteProjectRoot)bun.lockb" 
                               Condition="'$(ViteSingleConfigPackageManager)' == 'bun' AND Exists('$(ViteProjectRoot)bun.lockb')" />
        
        <!-- Always include package.json -->
        <_ViteConfigInputFiles Include="$(ViteProjectRoot)package.json" 
                               Condition="Exists('$(ViteProjectRoot)package.json')" />
    </ItemGroup>
</Target>
```

### **Phase 3: Updated Build Task**

```xml
<!-- Updated _ViteBuildSingleConfig with per-config isolation -->
<Target Name="_ViteBuildSingleConfig"
        Inputs="@(_ViteConfigInputFiles);$(ViteSingleConfigFile);$(MSBuildProjectFile)"
        Outputs="$(ViteSingleMarkerFile)"
        DependsOnTargets="_ResolveViteConfigInputs"
        Condition="'$(EnableViteBuild)' == 'true'">

    <!-- Enhanced build with config-specific package manager -->
    <BuildViteCommand 
        ProjectRoot="$(ViteWorkingDirectory)"
        PackageManagerName="$(ViteSingleConfigPackageManager)"  <!-- Config-specific! -->
        CustomCommand="$(ViteBuildCommand)"
        ConfigPath="$(ViteSingleConfigFile)"
        Mode="$(ViteSingleMode)"
        OutputDir="$(ViteSingleOutputDir)"
        LogLevel="$(ViteLogLevel)"
        EnableColors="$(ViteEnableColors)"
        EnvironmentVariables="$(_ViteEnvVars)"
        ValidateOnly="$(ViteValidateCommandOnly)"
        Condition="'$(ViteUseAdvancedTasks)' == 'true'">
        <Output TaskParameter="CommandLine" PropertyName="_ViteCommandLine" />
        <Output TaskParameter="ExecutedSuccessfully" PropertyName="_ViteExecutedSuccessfully" />
        <Output TaskParameter="ExitCode" PropertyName="_ViteExitCode" />
    </BuildViteCommand>
```

### **Phase 4: Enhanced MSBuild Batching Call**

```xml
<!-- Updated batching call with config-specific package manager -->
<MSBuild Projects="$(MSBuildProjectFile)" 
         Targets="_ViteBuildSingleConfig"
         Properties="ViteSingleConfigFile=%(ViteConfigs.Identity);ViteSingleOutputDir=%(_ViteConfigsToProcess.EffectiveOutputDir);ViteSingleMode=%(_ViteConfigsToProcess.EffectiveMode);ViteSingleBuildId=%(ViteConfigs.BuildId);ViteSingleMarkerFile=%(_ViteConfigsToProcess.MarkerFile);ViteSingleConfigPackageManager=%(_ViteConfigsToProcess.ConfigPackageManager)"
         BuildInParallel="false">
    <Output TaskParameter="TargetOutputs" ItemName="_ViteConfigResults" />
</MSBuild>
```

---

## 🧪 **Test Cases for Per-Config Architecture**

### **🔴 Critical - Independent Package Manager Detection**

```csharp
[Fact]
public void Each_ViteConfig_Should_Detect_Its_Own_Package_Manager()
{
    // Given: Multiple ViteConfigs with different PackageManager metadata
    var project = """
        <ItemGroup>
          <ViteConfigs Include="Admin">
            <PackageManager>npm</PackageManager>
          </ViteConfigs>
          <ViteConfigs Include="Customer">
            <PackageManager>yarn</PackageManager>
          </ViteConfigs>
        </ItemGroup>
        """;
    
    // When: Building both configs
    // Expected: Admin uses npm commands, Customer uses yarn commands
    // Expected: Independent of which lock files exist globally
}

[Fact]
public void ViteConfig_Should_Auto_Detect_Package_Manager_When_Not_Specified()
{
    // Given: ViteConfig with no PackageManager + multiple lock files exist
    // When: Building config
    // Expected: Should auto-detect using standard priority (bun > pnpm > yarn > npm)
}

[Fact] 
public void ViteConfig_PackageManager_Override_Should_Override_Global_Setting()
{
    // Given: Global <PackageManager>npm</PackageManager> + ViteConfig with yarn
    // When: Building the ViteConfig
    // Expected: Should use yarn, not npm
}
```

### **🔴 Critical - Independent Input Files**

```csharp
[Fact]
public void ViteConfig_Should_Only_Include_Its_Relevant_Lock_File_As_Input()
{
    // Given: Admin config (npm) + Customer config (yarn) + both lock files exist
    // When: yarn.lock changes
    // Expected: Only Customer config should rebuild, Admin should use cache
}

[Fact]
public void ViteConfig_Should_Include_All_Source_Files_But_Only_Relevant_Lock_File()
{
    // Given: ViteConfig with specific package manager
    // When: Building
    // Expected: Should include JS/CSS/assets + package.json + only relevant lock file
}
```

### **🔴 Critical - Separate Marker Files**

```csharp
[Fact]
public void Each_ViteConfig_Should_Have_Package_Manager_Specific_Marker_File()
{
    // Given: Multiple ViteConfigs with different package managers
    // When: Building
    // Expected: Separate marker files like:
    // - obj/Vite.Admin.npm.marker
    // - obj/Vite.Customer.yarn.marker
}

[Fact]
public void Changing_ViteConfig_PackageManager_Should_Invalidate_Previous_Marker()
{
    // Given: ViteConfig built with npm (creates npm marker)
    // When: Change PackageManager to yarn
    // Expected: Should rebuild (npm marker doesn't match yarn build)
}
```

---

## ✅ **Implementation Benefits**

### **🚀 Performance**
- ✅ **No unnecessary rebuilds** - Only relevant lock file changes trigger rebuilds
- ✅ **Parallel config builds** - Different configs can build independently
- ✅ **Efficient caching** - Each config has its own incremental build state

### **🔧 Reliability**  
- ✅ **Independent package managers** - No cross-config interference
- ✅ **Predictable behavior** - Each config is fully isolated
- ✅ **Correct marker files** - No shared state corruption

### **📈 Scalability**
- ✅ **Multiple configs per project** - Each with different package managers
- ✅ **Monorepo support** - Different areas can use different package managers
- ✅ **Team flexibility** - Frontend teams can choose their preferred tools

This architecture ensures that **each ViteConfig truly acts as an independent build task instance** with complete isolation! 🎯