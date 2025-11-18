# Package Manager RTFM Research - Vite-Specific Features & Edge Cases

## 📋 Purpose

Research official documentation for each package manager to identify **missing features and edge cases specifically related to running Vite** that we should be testing but aren't currently covering.

**Scope**: Only features that **directly affect Vite execution, build process, or environment setup**.

---

## 📦 NPM - Vite-Relevant Features We're Missing

### **NPM Script Environment Variables (Affects Vite Config)**

From [npm-run-script docs](https://docs.npmjs.com/cli/v9/commands/npm-run-script):

#### **Vite-Relevant Missing Test Cases:**

**1. NPM Package Info for Vite Config (Missing)**
```csharp
[Fact]
public void NPM_Should_Expose_Package_Info_To_Vite_Config()
{
    // Vite configs often read package.json values via process.env
    // Expected Environment: {
    //   "npm_package_name": "my-app",           // Used in vite.config.js for build.lib.name
    //   "npm_package_version": "1.0.0",        // Used for version injection in builds
    //   "npm_lifecycle_event": "build"         // Vite can detect which script is running
    // }
}
```

**2. NPM Node Path for Vite Plugins (Missing)**
```csharp
[Fact]
public void NPM_Should_Set_Node_Paths_For_Vite_Plugin_Resolution()
{
    // Vite needs to resolve plugins and their dependencies
    // Expected Environment: {
    //   "npm_node_execpath": "/path/to/node",  // Vite uses for subprocess spawning
    //   "NODE_PATH": "node_modules/.bin"       // Plugin resolution paths
    // }
}
```

**3. NPM Color Config Affecting Vite (Missing)**
```csharp
[Fact]
public void NPM_Should_Pass_Color_Config_To_Vite()
{
    // Given: .npmrc contains "color=false"
    // Expected Environment: {
    //   "npm_config_color": "false"    // Vite can read this for color decisions
    // }
    // Expected: Should also set NO_COLOR=1 for consistency
}
```

---

## 🧶 Yarn - Vite-Relevant Features We're Missing

### **Yarn PnP and Vite Module Resolution**

From [Yarn PnP docs](https://yarnpkg.com/features/pnp):

#### **Vite-Relevant Missing Test Cases:**

**1. Yarn PnP for Vite Plugin Resolution (Critical)**
```csharp
[Fact]
public void Yarn_PnP_Should_Configure_Vite_Module_Resolution()
{
    // Given: .yarnrc.yml contains "nodeLinker: pnp"
    // When: Building with Vite (which needs to resolve plugins)
    // Expected Environment: {
    //   "NODE_OPTIONS": "--require ./.pnp.cjs"  // Required for Vite to resolve modules
    // }
    // Expected: Vite should be able to find @vitejs/plugin-react, etc.
}
```

**2. Yarn Workspace Dependencies for Vite (Missing)**
```csharp
[Fact]
public void Yarn_Should_Resolve_Workspace_Dependencies_For_Vite()
{
    // Given: package.json contains "vite": "workspace:*"
    // When: Running vite build
    // Expected: Should use local workspace version of Vite
    // Critical: Affects which Vite version actually executes
}
```

**3. Yarn Berry Cache for Vite Assets (Missing)**
```csharp
[Fact]
public void Yarn_Should_Handle_Zero_Install_With_Vite_Dependencies()
{
    // Given: .yarn/cache contains Vite and plugins + .yarnrc.yml enableGlobalCache: false
    // When: Running vite build without yarn install
    // Expected: Vite should run using cached dependencies
}
```

---

## 📦 PNPM - Vite-Relevant Features We're Missing

### **PNPM Store and Vite Dependency Resolution**

From [PNPM docs](https://pnpm.io/):

#### **Vite-Relevant Missing Test Cases:**

**1. PNPM Store Path for Vite (Missing)**
```csharp
[Fact]
public void PNPM_Should_Set_Store_Path_For_Vite_Resolution()
{
    // Vite needs to resolve dependencies from PNPM's content-addressable store
    // Expected Environment: {
    //   "PNPM_STORE_PATH": "/Users/user/.local/share/pnpm/store/v3"
    // }
    // Critical: Affects how Vite resolves node_modules
}
```

**2. PNPM Shamefully-Hoist for Vite Plugins (Critical)**
```csharp
[Fact]
public void PNPM_Should_Handle_Vite_Plugin_Hoisting()
{
    // Given: .pnpmrc contains "shamefully-hoist=true" (for Vite plugin compatibility)
    // When: Vite tries to resolve plugins
    // Expected: Plugins should be findable by Vite
    // Note: Many Vite plugins require hoisting due to peer deps
}
```

**3. PNPM Workspace Filtering for Multi-App Vite Builds (Missing)**
```csharp
[Fact]
public void PNPM_Workspace_Filter_Should_Work_With_Custom_Vite_Commands()
{
    // Given: pnpm workspace with multiple Vite apps
    // When: ViteBuildCommand="pnpm --filter web run build"
    // Expected: Should build only the 'web' workspace package
    // Critical: Common pattern in monorepos
}
```

---

## 🟠 Bun - Vite-Relevant Features We're Missing

### **Bun Runtime and Vite Execution**

From [Bun docs](https://bun.sh/):

#### **Vite-Relevant Missing Test Cases:**

**1. Bun Native TypeScript for Vite Config (Major)**
```csharp
[Theory]
[InlineData("vite.config.ts", "Should run TS config natively")]
[InlineData("vite.config.mjs", "Should run ES modules natively")]
public void Bun_Should_Execute_Vite_Config_Files_Natively(string configFile, string expectedBehavior)
{
    // Bun can run TypeScript and ESM files without transpilation
    // Given: vite.config.ts (no build step needed)
    // When: bunx vite build --config vite.config.ts
    // Expected: Should run TypeScript config directly
}
```

**2. Bun Runtime Selection for Vite Performance (Missing)**
```csharp
[Theory]
[InlineData("bunx vite build", "Uses Bun runtime (faster)")]
[InlineData("bunx --node vite build", "Uses Node.js runtime (compatibility)")]
public void Bun_Should_Allow_Runtime_Selection_For_Vite(string command, string expectedRuntime)
{
    // Some Vite plugins may require Node.js runtime
    // Given: Vite build with potential compatibility issues
    // When: Using --node flag
    // Expected: Should use Node.js runtime for compatibility
}
```

**3. Bun Package Resolution Speed for Vite (Missing)**
```csharp
[Fact]
public void Bun_Should_Set_Fast_Resolution_Environment_For_Vite()
{
    // Bun's fast package resolution affects Vite startup time
    // Expected Environment: {
    //   "BUN_RUNTIME": "bun",           // Vite can detect Bun runtime
    //   "BUN_INSTALL_CACHE_DIR": "..."  // Affects dependency resolution speed
    // }
}
```

---

## 🎯 Vite-Specific Cross-Package Manager Features

### **Features That Directly Affect Vite Execution**

**1. Node.js Version Compatibility for Vite (Missing)**
```csharp
[Theory]
[InlineData("18.0.0", "Should work with Vite 5+")]
[InlineData("16.14.0", "Should work with Vite 4")]
[InlineData("14.18.0", "Should warn about old Node version")]
public void All_Package_Managers_Should_Check_Node_Version_For_Vite(string nodeVersion, string expectedBehavior)
{
    // Vite has specific Node.js version requirements
    // Given: Specific Node.js version
    // When: Running vite build
    // Expected: Should validate Node version compatibility
}
```

**2. CI Environment Detection for Vite Builds (Missing)**
```csharp
[Fact]
public void All_Package_Managers_Should_Set_CI_Environment_For_Vite()
{
    // Vite behaves differently in CI (no TTY, different caching, etc.)
    // Given: CI environment (CI=true)
    // Expected Environment: {
    //   "CI": "true",
    //   "NODE_ENV": "production",  // Vite defaults to production in CI
    //   "NO_COLOR": "1"            // No colored output in CI
    // }
}
```

**3. Vite Plugin Resolution Across Package Managers (Missing)**
```csharp
[Theory]
[InlineData(PackageManager.Npm, "node_modules/@vitejs/plugin-react")]
[InlineData(PackageManager.Yarn, ".yarn/cache/@vitejs-plugin-react-*")]
[InlineData(PackageManager.Pnpm, "node_modules/.pnpm/@vitejs+plugin-react@*/node_modules/@vitejs/plugin-react")]
[InlineData(PackageManager.Bun, "node_modules/@vitejs/plugin-react")]
public void Package_Managers_Should_Resolve_Vite_Plugins_Correctly(PackageManager pm, string expectedPath)
{
    // Each package manager stores dependencies differently
    // Given: @vitejs/plugin-react installed
    // When: Vite tries to resolve the plugin
    // Expected: Should find plugin regardless of package manager
}
```

---

## 🔧 Vite Configuration and Build-Specific Features

### **Features That Affect Vite Config and Build Process**

**1. Environment Variable Injection for Vite (Missing)**
```csharp
[Fact]
public void Package_Managers_Should_Provide_Build_Context_To_Vite()
{
    // Vite configs often need to know build context
    // Expected Environment: {
    //   "VITE_PACKAGE_MANAGER": "npm|yarn|pnpm|bun",  // For conditional config
    //   "VITE_BUILD_TOOL": "vite.msbuild",            // Identifies MSBuild integration
    //   "VITE_PROJECT_ROOT": "/path/to/project"        // For relative path resolution
    // }
}
```

**2. Vite Asset Resolution for Different Package Managers (Missing)**
```csharp
[Fact]
public void Package_Managers_Should_Handle_Vite_Asset_Imports_Correctly()
{
    // Vite resolves assets differently based on package manager
    // Given: import logo from './assets/logo.svg'
    // When: Building with different package managers
    // Expected: All should resolve asset paths correctly
}
```

**3. Vite Plugin Hot Reloading Environment (Missing)**
```csharp
[Fact]
public void Package_Managers_Should_Set_Development_Environment_For_Vite_HMR()
{
    // Vite's HMR (Hot Module Replacement) needs proper environment
    // Given: Development mode build
    // Expected Environment: {
    //   "NODE_ENV": "development",     // Required for HMR
    //   "VITE_HMR_PORT": "auto"       // Let Vite choose HMR port
    // }
}
```

---

## 🧶 Yarn Official Documentation Research

### **Yarn Modern (v2+) vs Classic (v1) - Official Differences**

From [Yarn docs](https://yarnpkg.com/):

#### **Missing Test Cases We Should Add:**

**1. Yarn PnP (Plug'n'Play) Detection (We're Missing This)**
```csharp
[Fact]
public void Yarn_Should_Detect_PnP_Mode_And_Set_Environment()
{
    // Given: .yarnrc.yml contains "nodeLinker: pnp"
    // When: Building with yarn
    // Expected Environment: {
    //   "YARN_PNP": "true",
    //   "NODE_OPTIONS": "--require ./.pnp.cjs"  // or .pnp.js
    // }
    // Expected Behavior: Commands should work with PnP resolution
}
```

**2. Yarn Berry .yarnrc.yml Configuration Support (Missing)**
```csharp
[Theory]
[InlineData("nodeLinker: node-modules", "yarn run build")]
[InlineData("nodeLinker: pnp", "yarn run build")]
[InlineData("enableGlobalCache: true", "yarn run build")]
public void Yarn_Should_Respect_YarnRc_Configuration(string yarnrcContent, string expectedBehavior)
{
    // Given: .yarnrc.yml with specific configuration
    // When: Building
    // Expected: Commands should respect yarn configuration
}
```

**3. Yarn Workspace Protocol Support (Missing)**
```csharp
[Fact]
public void Yarn_Should_Support_Workspace_Protocol_Dependencies()
{
    // Given: package.json contains "vite": "workspace:*"
    // When: Building with yarn workspace
    // Expected: Should resolve workspace dependencies correctly
    // Note: This affects how we detect if vite is available locally
}
```

**4. Yarn Zero-Install Support (Missing)**
```csharp
[Fact]
public void Yarn_Should_Support_Zero_Install_Mode()
{
    // Given: .yarn/cache contains all dependencies + .yarnrc.yml has enableGlobalCache: false
    // When: Building without running yarn install first
    // Expected: Should work due to zero-install (all deps in .yarn/cache)
}
```

**5. Yarn Plugin System Environment Variables (Missing)**
```csharp
[Fact]
public void Yarn_Should_Set_Plugin_Environment_Variables()
{
    // From Yarn docs: Plugin system affects environment
    // Expected Environment: {
    //   "YARN_PLUGINS": "list,of,active,plugins",
    //   "YARN_RC_FILENAME": ".yarnrc.yml"
    // }
}
```

**6. Yarn Constraints Support (Edge Case We're Missing)**
```csharp
[Fact]
public void Yarn_Should_Respect_Constraints_File()
{
    // Given: constraints.pro file exists (Yarn constraints)
    // When: Building in workspace
    // Expected: Should validate constraints before running commands
    // Note: Could affect build success/failure
}
```

---

## 📦 PNPM Official Documentation Research

### **PNPM Unique Features We're Missing**

From [PNPM docs](https://pnpm.io/):

#### **Missing Test Cases We Should Add:**

**1. PNPM Workspace Filtering (Major Feature We're Missing)**
```csharp
[Theory]
[InlineData("--filter web", "pnpm --filter web run build")]
[InlineData("--filter \"./packages/*\"", "pnpm --filter \"./packages/*\" run build")]
[InlineData("--filter \"...^@scope/shared\"", "pnpm --filter \"...^@scope/shared\" run build")]
public void PNPM_Should_Support_Workspace_Filtering_In_Custom_Commands(string filter, string expected)
{
    // PNPM's most powerful feature - workspace filtering
    // Given: pnpm workspace with multiple packages
    // When: ViteBuildCommand includes --filter
    // Expected: Should support complex filtering patterns
}
```

**2. PNPM Store and Content Addressing (Missing)**
```csharp
[Fact]
public void PNPM_Should_Set_Store_Related_Environment_Variables()
{
    // From PNPM docs: Content-addressable store affects environment
    // Expected Environment: {
    //   "PNPM_HOME": "/Users/user/.local/share/pnpm",
    //   "PNPM_STORE_PATH": "/Users/user/.local/share/pnpm/store/v3",
    //   "PNPM_CACHE_PATH": "/Users/user/.cache/pnpm"
    // }
}
```

**3. PNPM Shamefully-Hoist and Public-Hoist Patterns (Missing)**
```csharp
[Theory]
[InlineData("shamefully-hoist=true", "NODE_PATH includes hoisted packages")]
[InlineData("public-hoist-pattern=*eslint*", "ESLint packages are hoisted")]
public void PNPM_Should_Handle_Hoisting_Configurations(string pnpmrcConfig, string expectedBehavior)
{
    // Given: .pnpmrc with hoisting configuration
    // When: Building
    // Expected: Environment should reflect hoisting decisions
}
```

**4. PNPM Peer Dependency Resolution (Edge Case)**
```csharp
[Fact]
public void PNPM_Should_Handle_Peer_Dependency_Resolution_Warnings()
{
    // Given: vite has peer dependencies that aren't met
    // When: Using pnpm dlx vite build
    // Expected: Should handle peer dependency warnings gracefully
}
```

**5. PNPM Node Linker Configuration (Missing)**
```csharp
[Theory]
[InlineData("node-linker=isolated", "isolated mode")]
[InlineData("node-linker=hoisted", "hoisted mode")]
[InlineData("node-linker=pnp", "pnp mode")]
public void PNPM_Should_Support_Different_Node_Linker_Modes(string linkerConfig, string expectedBehavior)
{
    // Given: .pnpmrc with specific node-linker
    // When: Building
    // Expected: Commands should work with different linking strategies
}
```

**6. PNPM Global vs Local Package Resolution (Missing)**
```csharp
[Fact]
public void PNPM_DLX_Should_Prefer_Local_Over_Global_When_Available()
{
    // Given: vite installed locally + global vite also exists
    // When: pnpm dlx vite build
    // Expected: Should prefer local version (like npx behavior)
    // Actual PNPM behavior: dlx always downloads latest unless --prefer-offline
}
```

---

## 🟠 Bun Official Documentation Research

### **Bun Unique Features We're Missing**

From [Bun docs](https://bun.sh/):

#### **Missing Test Cases We Should Add:**

**1. Bun Runtime vs Node Runtime (Major Missing Feature)**
```csharp
[Theory]
[InlineData("bun vite build", "Uses Bun runtime")]
[InlineData("bun --bun vite build", "Forces Bun runtime")]
[InlineData("bun --node vite build", "Uses Node.js runtime")]
public void Bun_Should_Support_Runtime_Selection(string command, string expectedRuntime)
{
    // Bun can run code with either Bun runtime or Node.js runtime
    // This is a MAJOR feature we're not testing
}
```

**2. Bun Install vs NPM Install Compatibility (Missing)**
```csharp
[Fact]
public void Bun_Should_Handle_NPM_Lock_File_Compatibility()
{
    // Given: package-lock.json exists (not bun.lockb)
    // When: Building with bun
    // Expected: Should handle npm lockfile gracefully or warn user
}
```

**3. Bun Workspaces with Different Package Managers (Edge Case)**
```csharp
[Fact]
public void Bun_Should_Handle_Mixed_Package_Manager_Workspaces()
{
    // Given: Root has bun.lockb but workspace package has package-lock.json
    // When: Building workspace package
    // Expected: Should handle mixed package manager scenario
}
```

**4. Bun Hot Reloading and Watch Mode (Missing)**
```csharp
[Fact]
public void Bun_Should_Set_Watch_Mode_Environment_Variables()
{
    // Given: bun run build --watch
    // Expected Environment: {
    //   "BUN_ENV": "development",
    //   "BUN_HOT": "true"
    // }
}
```

**5. Bun Native Binary Execution (Major Missing Feature)**
```csharp
[Fact]
public void Bun_Should_Support_Native_Binary_Execution()
{
    // Bun can compile to native binaries
    // Given: vite compiled as bun binary
    // When: bunx ./vite-binary build
    // Expected: Should execute native binary instead of Node.js script
}
```

**6. Bun Package Installation Speed Optimizations (Missing)**
```csharp
[Fact]
public void Bun_Should_Set_Performance_Environment_Variables()
{
    // From Bun docs: Performance-related settings
    // Expected Environment: {
    //   "BUN_INSTALL_CACHE_DIR": "/path/to/cache",
    //   "BUN_TMPDIR": "/tmp/bun-install",
    //   "BUN_INSTALL_PROGRESS": "true"
    // }
}
```

**7. Bun JSX and TypeScript Native Support (Missing)**
```csharp
[Fact]
public void Bun_Should_Handle_Native_TypeScript_And_JSX()
{
    // Given: vite.config.ts (TypeScript config)
    // When: bunx vite build --config vite.config.ts
    // Expected: Should handle .ts files natively without transpilation
    // Note: This affects config path handling
}
```

---

## 🔀 Cross-Package Manager Edge Cases from Documentation

### **Missing Universal Features**

**1. Lock File Conflict Resolution (Missing from ALL)**
```csharp
[Theory]
[InlineData("package-lock.json + yarn.lock", "Should warn about conflicts")]
[InlineData("package-lock.json + pnpm-lock.yaml", "Should warn about conflicts")]
[InlineData("yarn.lock + bun.lockb", "Should warn about conflicts")]
public void All_Package_Managers_Should_Handle_Lock_File_Conflicts(string lockFiles, string expectedBehavior)
{
    // Given: Multiple lock files exist
    // When: Building
    // Expected: Should warn user or pick primary package manager
}
```

**2. CI Environment Detection (Missing)**
```csharp
[Fact]
public void All_Package_Managers_Should_Detect_CI_Environments()
{
    // Given: CI environment variables set (CI=true, GITHUB_ACTIONS=true, etc.)
    // When: Building
    // Expected Environment: {
    //   "CI": "true",
    //   "NODE_ENV": "production",  // Default for CI
    //   "NO_COLOR": "1"            // Default for CI
    // }
}
```

**3. Offline Mode Support (Missing)**
```csharp
[Theory]
[InlineData(PackageManager.Npm, "npm run build --offline")]
[InlineData(PackageManager.Yarn, "yarn run build --offline")]
[InlineData(PackageManager.Pnpm, "pnpm run build --offline")]
public void Package_Managers_Should_Support_Offline_Mode(PackageManager pm, string expectedOfflineCommand)
{
    // When: Network unavailable or --offline flag used
    // Expected: Should work with cached packages only
}
```

**4. Package Manager Version Compatibility (Missing)**
```csharp
[Fact]
public void Should_Warn_About_Incompatible_Package_Manager_Versions()
{
    // Given: Old npm version (< 7) with workspaces
    // When: Building
    // Expected: Should warn that workspaces not supported in npm < 7
}
```

---

## 🔧 Configuration File Edge Cases We're Missing

### **Package Manager Configuration Files**

**1. Multiple Configuration File Priority (Missing)**
```csharp
[Theory]
[InlineData(".npmrc (user) + .npmrc (project)", "Project .npmrc wins")]
[InlineData(".yarnrc.yml + .yarnrc", "Modern .yarnrc.yml wins over classic .yarnrc")]
[InlineData(".pnpmrc + pnpm-workspace.yaml", "Both should be respected")]
public void Should_Handle_Multiple_Configuration_Files(string configFiles, string expectedBehavior)
```

**2. Configuration File Encoding (Missing)**
```csharp
[Fact]
public void Should_Handle_Non_UTF8_Configuration_Files()
{
    // Given: .npmrc with Windows-1252 encoding
    // When: Building
    // Expected: Should parse configuration correctly
}
```

---

## 🚨 Security Edge Cases We're Missing

**1. Package Manager Security Audit Integration (Missing)**
```csharp
[Fact]
public void Should_Respect_Package_Manager_Security_Policies()
{
    // Given: npm audit finds vulnerabilities in dependencies
    // When: Building
    // Expected: Should continue or fail based on security policy
}
```

**2. Registry Authentication (Missing)**
```csharp
[Fact]
public void Should_Handle_Private_Registry_Authentication()
{
    // Given: .npmrc contains private registry with auth token
    // When: Building with dependencies from private registry
    // Expected: Should authenticate correctly for package resolution
}
```

## 📋 Filtered Test Cases - Vite Execution Priority

### **🔴 Critical for Vite (Must Add)**
1. **Environment Variables for Vite Config** - `npm_package_version`, `VITE_PACKAGE_MANAGER`
2. **Module Resolution Environment** - NODE_OPTIONS for PnP, store paths for PNPM
3. **Vite Plugin Resolution** - Hoisting configs, workspace dependencies
4. **Node.js Version Compatibility** - Vite version requirements
5. **CI Environment for Vite** - Production builds, color handling
6. **Lock File Conflict Resolution** - **CRITICAL for incremental builds!**

### **Lock File Conflicts Impact on Vite Incremental Builds**

```csharp
[Theory]
[InlineData("package-lock.json + yarn.lock", "npm", "Should prefer npm and warn about yarn.lock")]
[InlineData("package-lock.json + pnpm-lock.yaml", "npm", "Should prefer npm and warn about pnpm-lock.yaml")]
[InlineData("yarn.lock + bun.lockb", "yarn", "Should prefer yarn and warn about bun.lockb")]
public void Lock_File_Conflicts_Should_Affect_Incremental_Build_Detection(string lockFiles, string expectedPM, string expectedBehavior)
{
    // CRITICAL: Lock files are inputs to our incremental build system!
    // Given: Multiple lock files exist (common in teams switching package managers)
    // When: MSBuild checks inputs for incremental build
    // Expected: Should pick primary package manager and use correct lock file for change detection
    // Impact: Wrong lock file = broken incremental builds!
}

[Fact]
public void Lock_File_Changes_Should_Trigger_Vite_Rebuild()
{
    // CRITICAL: Lock file changes mean dependency changes
    // Given: package-lock.json modified (new Vite plugin added)
    // When: Running incremental build
    // Expected: Should detect lock file change and trigger Vite rebuild
    // Impact: Using wrong lock file = missing dependency changes = stale builds!
}

[Fact]
public void Package_Manager_Detection_Should_Be_Deterministic_For_Incremental_Builds()
{
    // CRITICAL: Package manager detection must be consistent across builds
    // Given: Both npm and yarn lock files exist
    // When: Building multiple times
    // Expected: Should consistently pick same package manager
    // Impact: Different PM choice = different command = broken incremental build markers!
}
```

**Why This Matters for Vite:**

1. **MSBuild Incremental Build Inputs**: Our `ViteBuildAssets` target uses lock files as `@(ViteInputFiles)`
2. **Dependency Change Detection**: Lock file changes indicate new/updated Vite plugins or dependencies  
3. **Package Manager Consistency**: Different PM commands would invalidate build markers
4. **Node Modules State**: Wrong lock file = wrong dependency resolution = potential Vite failures

### **🟡 Important for Vite (Should Add)**
1. **TypeScript Config Execution** - Bun native TS, vite.config.ts handling
2. **Runtime Selection for Compatibility** - Bun --node for plugin compatibility  
3. **Development Environment Setup** - HMR, watch mode variables
4. **Asset Resolution Paths** - Different package manager structures

### **🟢 Nice to Have for Vite (Future)**
1. **Performance Optimizations** - Cache directories, fast resolution
2. **Workspace Filtering** - Monorepo build targeting
3. **Plugin Compatibility Checks** - Hoisting requirements

---

## 🎯 Refined Implementation Priority

### **Phase 1: Core Vite Execution Environment**
- Add package manager identification variables (`VITE_PACKAGE_MANAGER`)
- Add Node.js path variables for plugin resolution
- Add CI environment detection for production builds
- Fix color environment variables (already in progress)

### **Phase 2: Advanced Vite Features** 
- Add Yarn PnP support for module resolution
- Add PNPM hoisting configuration for plugin compatibility
- Add Bun TypeScript config execution
- Add workspace dependency resolution

### **Phase 3: Development Experience**
- Add HMR environment setup
- Add performance optimization variables
- Add asset resolution path handling

This filtered scope focuses on **what actually matters for running Vite successfully** across different package managers! 🎯