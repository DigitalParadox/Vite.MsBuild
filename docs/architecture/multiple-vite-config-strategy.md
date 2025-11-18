# Multiple Vite Config Strategy Analysis

## Current State (Single Config)
Currently, we support **one Vite config per project**:
```xml
<ViteConfigFile>$(MSBuildProjectDirectory)\vite.config.ts</ViteConfigFile>
```

## Real-World Scenarios Requiring Multiple Configs

### **1. Environment-Specific Configs** 🌍
```
MyProject/
├── vite.config.ts           ← Base config
├── vite.config.dev.ts       ← Development overrides  
├── vite.config.staging.ts   ← Staging environment
├── vite.config.prod.ts      ← Production optimizations
└── vite.config.test.ts      ← Testing configuration
```

### **2. Multi-Target Builds** 🎯
```
WebApp/
├── vite.config.client.ts    ← Client-side SPA
├── vite.config.server.ts    ← SSR build
├── vite.config.widget.ts    ← Embeddable widget
└── vite.config.mobile.ts    ← Mobile PWA build
```

### **3. Monorepo Architecture** 🏗️
```
Enterprise/
├── apps/
│   ├── admin/vite.config.ts     ← Admin dashboard
│   └── customer/vite.config.ts  ← Customer portal
├── packages/
│   ├── ui/vite.config.ts        ← Shared UI library
│   └── widgets/vite.config.ts   ← Widget library
└── vite.config.base.ts          ← Shared base config
```

### **4. Framework Migration** 🔄
```
Migration/
├── vite.config.legacy.ts    ← Legacy Vue 2 code
├── vite.config.modern.ts    ← New Vue 3 code
└── vite.config.shared.ts    ← Shared components
```

## **Strategic Options** 🤔

### **Option 1: Rollup Config (Recommended for Most Cases)** ✅

**Vite supports rollup-style build configuration:**
```typescript
// vite.config.ts
export default defineConfig({
  build: {
    rollupOptions: {
      input: {
        main: 'src/main.ts',
        admin: 'src/admin.ts',
        widget: 'src/widget.ts'
      },
      output: {
        dir: 'dist',
        entryFileNames: '[name]/[name].js',
        chunkFileNames: '[name]/[name]-[hash].js'
      }
    }
  }
})
```

**Benefits:**
- ✅ **Single config file** - simpler MSBuild integration
- ✅ **Official Vite pattern** - well documented and supported
- ✅ **Shared optimizations** - better tree shaking and chunk sharing
- ✅ **Simpler dependency tracking** - one config to monitor

### **Option 2: Multiple Config Support** 🔧

**Could support multiple configs via ItemGroup:**
```xml
<ItemGroup>
  <ViteConfigFiles Include="vite.config.client.ts" ViteMode="production" OutputDir="wwwroot\client" />
  <ViteConfigFiles Include="vite.config.admin.ts" ViteMode="development" OutputDir="wwwroot\admin" />
  <ViteConfigFiles Include="vite.config.widget.ts" ViteMode="production" OutputDir="wwwroot\widgets" />
</ItemGroup>
```

**Benefits:**
- ✅ **Maximum flexibility** - completely separate builds
- ✅ **Independent optimization** - each config optimized differently  
- ✅ **Clear separation** - distinct build artifacts

**Challenges:**
- ❌ **Complex MSBuild logic** - multiple target execution
- ❌ **Build time impact** - sequential config processing
- ❌ **Dependency complexity** - tracking multiple config files
- ❌ **Marker file complexity** - per-config incremental builds

### **Option 3: Hybrid Approach** ⚖️

**Support both patterns:**
```xml
<!-- Simple: Single config (current) -->
<ViteConfigFile>vite.config.ts</ViteConfigFile>

<!-- Advanced: Multiple configs (new) -->
<ItemGroup>
  <ViteConfigFiles Include="configs\*.config.ts" />
</ItemGroup>
```

## **Recommendation: Focus on Rollup Config** 🎯

### **Why Rollup Config is Better for Most Cases:**

1. **🏗️ Architectural Alignment**
   - Vite is built on Rollup - this is the "native" way
   - Better optimization opportunities (shared chunks, tree shaking)
   - Simpler MSBuild integration (one config = one execution)

2. **📚 Documentation & Ecosystem**
   - Extensive Vite documentation for multi-entry builds
   - Community plugins designed for this pattern
   - Better IDE support and tooling

3. **⚡ Performance Benefits**
   - Single build process = faster execution
   - Shared dependency resolution
   - Better chunk optimization

### **When Multiple Configs Make Sense:**

1. **🌍 Completely Different Environments**
   - Different target frameworks (.NET Framework vs .NET Core)
   - Different deployment targets (Azure vs AWS vs on-premise)
   - Completely different technology stacks in same repo

2. **🔧 Different Build Tools**
   - Some parts using Vite, others using Webpack
   - Legacy build system migration scenarios

### **Proposed Documentation Strategy** 📖

```markdown
# Advanced Build Scenarios

## Multi-Entry Builds (Recommended)
For multiple build outputs, use Vite's built-in multi-entry support:

```typescript
// vite.config.ts
export default defineConfig({
  build: {
    rollupOptions: {
      input: {
        main: 'src/main.ts',
        admin: 'src/admin/main.ts',
        widget: 'src/widget/main.ts'
      }
    }
  }
})
```

## Multiple Configs (Advanced)
Only use separate config files when you need completely different build pipelines:

```xml
<PropertyGroup>
  <ViteConfigFile Condition="'$(BuildProfile)' == 'Admin'">vite.config.admin.ts</ViteConfigFile>
  <ViteConfigFile Condition="'$(BuildProfile)' == 'Customer'">vite.config.customer.ts</ViteConfigFile>
</PropertyGroup>
```
```

## **Implementation Priority** 📋

1. **Phase 1**: Enhance documentation for Rollup multi-entry patterns
2. **Phase 2**: Add examples for common scenarios (admin/client, multi-target)  
3. **Phase 3**: Consider multiple config support only if strong user demand

**The Rollup config approach covers 90% of real-world scenarios while maintaining simplicity and performance.**