# Vite.MsBuild Naming Refactor Plan

## 🎯 Problem: Confusing Similar Names

Current naming is too similar and causes confusion:
- `DefineViteConfig` (Task)
- `ViteConfig` (ItemGroup) 
- `ValidateViteConfig` (Task)
- `ConfigureViteConfig` (Task)
- `ViteConfigFile` (Property)

## 💡 Proposed Clear Naming Convention

### **Pattern: Purpose + Noun + Type**

| Current Name | New Name | Type | Purpose |
|--------------|----------|------|---------|
| `DefineViteConfig` | `CreateViteBuild` | Task | Creates build definitions |
| `ViteConfig` | `ViteBuild` | ItemGroup | Build configuration data |
| `ValidateViteConfig` | `ValidateViteConfigFile` | Task | Validates vite.config.ts files |
| `ConfigureViteConfig` | `ProcessViteBuilds` | Task | Processes multiple builds |
| `ViteConfigFile` | `ViteConfigFile` | Property | ✅ Already clear |

### **Naming Rules:**

1. **Tasks** (C# executables): `Verb + Noun`
   - `CreateViteBuild` - Creates build configurations
   - `ValidateViteConfigFile` - Validates config files
   - `ProcessViteBuilds` - Processes multiple builds

2. **ItemGroups** (Data containers): `Noun` (singular)
   - `ViteBuild` - Individual build configuration
   - `ViteInputFile` - Input file for builds

3. **Properties** (Variables): `Noun + Qualifier`
   - `ViteConfigFile` - Path to vite.config.ts
   - `ViteProjectRoot` - Root directory

4. **Files** (Filesystem): `Descriptive name`
   - `vite.config.ts` - Actual Vite configuration file
   - `package.json` - Node.js package file

## 🔄 Migration Examples

### Before (Confusing):
```xml
<DefineViteConfig Name="admin" Config="vite.config.ts" />

<ItemGroup>
  <ViteConfig Include="Areas/Admin/vite.config.ts">
    <BuildId>admin</BuildId>
  </ViteConfig>
</ItemGroup>

<ValidateViteConfig ConfigPath="$(ViteConfigFile)" />
```

### After (Clear):
```xml
<CreateViteBuild Name="admin" ConfigFile="vite.config.ts" />

<ItemGroup>
  <ViteBuild Include="Areas/Admin/vite.config.ts">
    <BuildId>admin</BuildId>
  </ViteBuild>
</ItemGroup>

<ValidateViteConfigFile ConfigPath="$(ViteConfigFile)" />
```

## 📊 Benefits

| Aspect | Before | After |
|--------|--------|-------|
| **Clarity** | ❌ Confusing | ✅ Self-documenting |
| **Purpose** | ❌ Unclear | ✅ Obvious from name |
| **Type** | ❌ Mixed patterns | ✅ Consistent rules |
| **IntelliSense** | ❌ Hard to find | ✅ Logical grouping |

## 🎯 Implementation Plan

1. **Create new classes** with clear names
2. **Update MSBuild targets** to use new names
3. **Add aliases** for backward compatibility
4. **Update documentation** with new examples
5. **Deprecation warnings** for old names

## ✅ Backward Compatibility

Keep old names as aliases with deprecation warnings:
```csharp
[Obsolete("Use CreateViteBuild instead")]
public class DefineViteConfig : CreateViteBuild { }
```

```xml
<!-- Old name still works but shows warning -->
<DefineViteConfig Name="admin" Config="vite.config.ts" />
<!-- New recommended name -->
<CreateViteBuild Name="admin" ConfigFile="vite.config.ts" />
```