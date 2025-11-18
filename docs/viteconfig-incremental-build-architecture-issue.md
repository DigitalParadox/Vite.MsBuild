# ViteConfig Incremental Build Architecture Issue

## 🚨 **Problem Identified**

Our current design has a **fundamental architectural flaw** regarding incremental builds and multiple ViteConfigs.

### **Current Flawed Design:**

```xml
<!-- We include ALL lock files globally -->
<ViteInputFiles Include="$(ViteProjectRoot)package-lock.json" />
<ViteInputFiles Include="$(ViteProjectRoot)yarn.lock" />  
<ViteInputFiles Include="$(ViteProjectRoot)pnpm-lock.yaml" />
<ViteInputFiles Include="$(ViteProjectRoot)bun.lockb" />

<!-- But each ViteConfig might use different package managers -->
<ItemGroup>
  <ViteConfigs Include="Admin">
    <PackageManager>npm</PackageManager>
  </ViteConfigs>
  <ViteConfigs Include="Customer">
    <PackageManager>yarn</PackageManager>
  </ViteConfigs>
</ItemGroup>

<!-- Problem: Both configs see ALL lock files as inputs! -->
<Target Name="_ViteBuildSingleConfig"
        Inputs="@(ViteInputFiles);$(ViteSingleConfigFile)"
        Outputs="$(ViteSingleMarkerFile)">
  <!-- This means yarn.lock changes trigger npm config rebuild! -->
</Target>
```

### **Specific Issues:**

**1. Wrong Incremental Build Triggers**
- Admin config (npm) rebuilds when `yarn.lock` changes  
- Customer config (yarn) rebuilds when `package-lock.json` changes
- **Result**: Unnecessary rebuilds, wasted CI time

**2. Inconsistent Package Manager Detection**
- Global lock file detection affects all configs
- Each config should detect its own package manager independently
- **Result**: Config A might use wrong package manager

**3. Shared Marker File Confusion**  
- Different package managers could create conflicting markers
- **Result**: Broken incremental builds across configs

## 🎯 **Correct Architecture Should Be:**

### **Per-Config Package Manager Detection:**

```xml
<Target Name="_ViteBuildSingleConfig"
        Inputs="@(_ViteConfigSpecificInputs);$(ViteSingleConfigFile)"
        Outputs="$(ViteSingleMarkerFile)">

  <!-- Each config detects its own package manager -->
  <PropertyGroup>
    <_ConfigPackageManager>$(%(ViteConfigs.PackageManager))</_ConfigPackageManager>
    <_ConfigPackageManager Condition="'$(_ConfigPackageManager)' == ''">$(PackageManager)</_ConfigPackageManager>
  </PropertyGroup>
  
  <!-- Each config uses only ITS relevant lock file -->
  <ItemGroup>
    <_ViteConfigSpecificInputs Include="@(ViteInputFiles)" />
    <_ViteConfigSpecificInputs Include="$(ViteProjectRoot)package-lock.json" 
                              Condition="'$(_ConfigPackageManager)' == 'npm' AND Exists('$(ViteProjectRoot)package-lock.json')" />
    <_ViteConfigSpecificInputs Include="$(ViteProjectRoot)yarn.lock" 
                              Condition="'$(_ConfigPackageManager)' == 'yarn' AND Exists('$(ViteProjectRoot)yarn.lock')" />
    <!-- etc for pnpm, bun -->
  </ItemGroup>
</Target>
```

### **Per-Config Marker Files:**

```xml
<!-- Different markers per package manager per config -->
<ViteSingleMarkerFile>$(IntermediateOutputPath)Vite.$(ViteSingleBuildId).$(_ConfigPackageManager).marker</ViteSingleMarkerFile>
```

## 🧪 **Test Cases We Need:**

### **🔴 Critical - Per-Config Independence:**

```csharp
[Fact]
public void Multiple_ViteConfigs_Should_Have_Independent_Package_Manager_Detection()
{
    // Given: ViteConfigs with different explicit package managers
    // When: Building each config
    // Expected: Each should use its configured PM, not global detection
}

[Fact]
public void ViteConfig_Should_Only_Use_Relevant_Lock_File_As_Input()
{
    // Given: Admin config uses npm, Customer config uses yarn
    // When: yarn.lock changes  
    // Expected: Only Customer config should rebuild, not Admin config
}

[Fact]
public void Multiple_ViteConfigs_Should_Have_Separate_Incremental_Build_Markers()
{
    // Given: Multiple configs with different package managers
    // When: Building
    // Expected: Each config should have its own marker file
    // Example: obj/Vite.Admin.npm.marker vs obj/Vite.Customer.yarn.marker
}
```

### **🟡 Important - Fallback Behavior:**

```csharp
[Fact]
public void ViteConfig_Without_Explicit_PackageManager_Should_Use_Global_Detection()
{
    // Given: ViteConfig with no PackageManager specified
    // When: Building
    // Expected: Should fall back to global package manager detection
}

[Fact]
public void ViteConfig_Should_Inherit_Global_ViteInputFiles_Plus_Config_Specific_Lock_File()
{
    // Given: Global ViteInputFiles + config-specific package manager
    // When: Building config
    // Expected: Should include global files + only relevant lock file
}
```

## ✅ **Solution Priority:**

**🔴 Immediate Fix Needed:**
1. **Separate lock file inputs per config** based on package manager
2. **Per-config marker files** to prevent cross-config interference  
3. **Independent package manager detection** per ViteConfig

**🟡 Follow-up Improvements:**
1. **Validate ViteConfig package manager settings** match available lock files
2. **Optimize incremental builds** to skip configs when their inputs haven't changed

## 🎯 **Impact Assessment:**

**Without this fix:**
- ❌ Unnecessary rebuilds (performance impact)
- ❌ Inconsistent package manager behavior across configs  
- ❌ Potential build failures from cross-config interference

**With this fix:**
- ✅ True incremental builds per config
- ✅ Independent package manager handling
- ✅ Scalable multi-config architecture

This is a **fundamental architecture issue** that affects the reliability and performance of multi-config scenarios! 🚨