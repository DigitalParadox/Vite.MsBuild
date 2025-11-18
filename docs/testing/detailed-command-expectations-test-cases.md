# Detailed Technical Command Expectations Test Case Document

## 📋 Purpose

Define **detailed test case expectations** for command construction across all package managers. Each package manager should have comprehensive test coverage for:

- **Direct calls** (npm run test, vite build)
- **Remote calls** (npx vite build, yarn dlx vite build, etc.)
- **Environment variables** specific to each package manager
- **Custom command definitions** and overrides

## 🔧 Test Categories per Package Manager

For each package manager, we need these test categories:
1. **Direct Script Execution** - Using package.json scripts
2. **Remote Tool Execution** - Using package manager's tool runner (npx equivalent)
3. **Environment Variables** - Package manager specific env vars and color handling
4. **Custom Command Override** - When user provides custom build command
5. **Error Handling** - Fallback behaviors and edge cases

---

## 📦 NPM Test Cases

### **Category 1: NPM Direct Script Execution**

#### **Test Case: NPM Script with No Additional Arguments**
```csharp
[Fact]
public void NPM_Should_Execute_Package_Script_Directly()
{
    // Given: package.json contains: { "scripts": { "build": "vite build --watch" } }
    // When: Building with npm package manager
    // Expected Command: "npm run build"
    // Expected Environment: { /* package manager defaults */ }
}
```

#### **Test Case: NPM Script with Additional Vite Arguments**
```csharp
[Fact] 
public void NPM_Should_Pass_Additional_Args_With_Double_Dash()
{
    // Given: package.json contains: { "scripts": { "build": "vite build" } }
    // When: Building with --config vite.prod.config.ts --mode production
    // Expected Command: "npm run build -- --config \"vite.prod.config.ts\" --mode production"
    // Expected Environment: { /* package manager defaults */ }
}
```

#### **Test Case: NPM Script with Complex Arguments**
```csharp
[Theory]
[InlineData("--config vite.config.ts", "npm run build -- --config \"vite.config.ts\"")]
[InlineData("--mode production --outDir dist", "npm run build -- --mode production --outDir \"dist\"")]
[InlineData("--config \"path with spaces.ts\"", "npm run build -- --config \"path with spaces.ts\"")]
public void NPM_Should_Handle_Complex_Script_Arguments(string viteArgs, string expectedCommand)
{
    // Test various argument combinations with npm's -- separator
}
```

### **Category 2: NPM Remote Tool Execution (NPX)**

#### **Test Case: NPX Direct Vite Execution**
```csharp
[Fact]
public void NPM_Should_Use_NPX_When_No_Build_Script_Exists()
{
    // Given: package.json has no "build" script (or no package.json)
    // When: Building with npm package manager
    // Expected Command: "npx vite build"
    // Expected Environment: { /* npm defaults */ }
}
```

#### **Test Case: NPX with Vite Arguments**
```csharp
[Fact]
public void NPM_Should_Pass_Args_Directly_To_NPX_Vite()
{
    // Given: No build script, using direct npx execution
    // When: Building with --config vite.config.ts --mode production
    // Expected Command: "npx vite build --config \"vite.config.ts\" --mode production"
    // Expected Environment: { /* npm defaults */ }
}
```

#### **Test Case: NPX with Package Installation**
```csharp
[Fact]
public void NPM_NPX_Should_Install_Vite_If_Not_Found_Locally()
{
    // Given: No local vite installation, no global vite
    // When: Using npx vite build
    // Expected Command: "npx vite build" (npx handles installation automatically)
    // Expected Behavior: Should not fail, npx downloads and executes
}
```

### **Category 3: NPM Environment Variables**

#### **Test Case: NPM Color Handling - Industry Standard**
```csharp
[Theory]
[InlineData(true, "FORCE_COLOR", "1")]
[InlineData(false, "NO_COLOR", "1")]
public void NPM_Should_Set_Standard_Color_Environment_Variables(bool enableColors, string expectedVar, string expectedValue)
{
    // Given: NPM package manager with color preference
    // When: enableColors is true/false
    // Expected Environment: { "FORCE_COLOR": "1" } or { "NO_COLOR": "1" }
    // Standard: https://no-color.org/ and https://force-color.org/
}
```

#### **Test Case: NPM NODE_ENV Environment Variable**
```csharp
[Theory]
[InlineData("development", "development")]
[InlineData("production", "production")]
[InlineData("staging", "staging")]
public void NPM_Should_Set_NODE_ENV_Based_On_Build_Mode(string viteMode, string expectedNodeEnv)
{
    // Given: NPM with specific vite mode
    // When: Building with --mode {viteMode}
    // Expected Environment: { "NODE_ENV": "{expectedNodeEnv}" }
}
```

#### **Test Case: NPM Package Manager Identification**
```csharp
[Fact]
public void NPM_Should_Set_Package_Manager_Environment_Variables()
{
    // Given: NPM package manager detected
    // When: Building any command
    // Expected Environment: { 
    //   "npm_config_user_agent": "npm/8.x.x",
    //   "npm_execpath": "/path/to/npm",
    //   "VITE_PACKAGE_MANAGER": "npm"
    // }
}
```

### **Category 4: NPM Custom Command Override**

#### **Test Case: NPM Custom Command Priority**
```csharp
[Fact]
public void NPM_Custom_Command_Should_Override_Package_Script()
{
    // Given: package.json has "build" script AND custom command is set
    // When: ViteBuildCommand="npm run build:production --verbose"
    // Expected Command: "npm run build:production --verbose" (custom wins)
    // Expected Environment: { /* color vars still set */ }
}
```

#### **Test Case: NPM Custom Command with Environment Variables**
```csharp
[Fact]
public void NPM_Custom_Command_Should_Still_Set_Environment_Variables()
{
    // Given: Custom command "npm run custom-build"
    // When: Building with enableColors=false
    // Expected Command: "npm run custom-build"
    // Expected Environment: { "NO_COLOR": "1", /* other npm env vars */ }
}
```

### **Category 5: NPM Error Handling**

#### **Test Case: NPM Fallback When NPX Unavailable**
```csharp
[Fact]
public void NPM_Should_Fallback_When_NPX_Not_Found()
{
    // Given: npm is available but npx is not in PATH
    // When: Attempting direct vite execution
    // Expected Behavior: Fallback to "npm exec vite build" (npm 7+) or error with helpful message
}
```

---

## 🧶 Yarn Test Cases

### **Category 1: Yarn Direct Script Execution**

#### **Test Case: Yarn Script Execution (Modern Yarn 3+)**
```csharp
[Fact]
public void Yarn_Modern_Should_Execute_Scripts_Directly()
{
    // Given: .yarnrc.yml exists (Yarn Berry/Modern) + package.json has build script
    // When: Building with yarn package manager
    // Expected Command: "yarn build" (no "run" needed in modern Yarn)
    // Expected Environment: { /* yarn specific vars */ }
}
```

#### **Test Case: Yarn Script Execution (Classic Yarn 1.x)**
```csharp
[Fact]
public void Yarn_Classic_Should_Use_Run_Command_For_Scripts()
{
    // Given: No .yarnrc.yml (Yarn Classic) + package.json has build script
    // When: Building with yarn package manager  
    // Expected Command: "yarn run build"
    // Expected Environment: { /* yarn classic vars */ }
}
```

#### **Test Case: Yarn Script with Additional Arguments**
```csharp
[Theory]
[InlineData("yarn modern", ".yarnrc.yml exists", "yarn build --config \"vite.config.ts\"")]
[InlineData("yarn classic", "no .yarnrc.yml", "yarn run build --config \"vite.config.ts\"")]
public void Yarn_Should_Pass_Arguments_Directly_No_Double_Dash(string yarnType, string detection, string expected)
{
    // Given: Yarn (modern or classic) with additional vite arguments
    // When: Building with --config vite.config.ts
    // Expected: Arguments passed directly (NO -- separator like npm)
}
```

### **Category 2: Yarn Remote Tool Execution (DLX)**

#### **Test Case: Yarn DLX Direct Vite Execution (Modern)**
```csharp
[Fact]
public void Yarn_Modern_Should_Use_DLX_For_Direct_Execution()
{
    // Given: .yarnrc.yml exists + no build script in package.json
    // When: Building with yarn package manager
    // Expected Command: "yarn dlx vite build"
    // Expected Environment: { /* yarn modern vars */ }
}
```

#### **Test Case: Yarn Classic Direct Execution Fallback**
```csharp
[Fact]
public void Yarn_Classic_Should_Fallback_To_Yarn_Vite_For_Direct_Execution()
{
    // Given: No .yarnrc.yml (Yarn Classic) + no build script
    // When: Building with yarn package manager
    // Expected Command: "yarn vite build" (or fallback to npx if vite not in deps)
    // Expected Environment: { /* yarn classic vars */ }
}
```

#### **Test Case: Yarn DLX with Arguments**
```csharp
[Fact]
public void Yarn_DLX_Should_Pass_Arguments_To_Vite()
{
    // Given: Yarn modern with dlx + vite arguments
    // When: Building with --config vite.config.ts --mode production
    // Expected Command: "yarn dlx vite build --config \"vite.config.ts\" --mode production"
    // Expected Environment: { /* yarn vars */ }
}
```

### **Category 3: Yarn Environment Variables**

#### **Test Case: Yarn Color Handling**
```csharp
[Theory]
[InlineData(true, "FORCE_COLOR", "1")]
[InlineData(false, "NO_COLOR", "1")]
public void Yarn_Should_Use_Standard_Color_Environment_Variables(bool enableColors, string expectedVar, string expectedValue)
{
    // Given: Yarn with color preferences
    // When: enableColors setting applied
    // Expected Environment: Standard color vars (same as npm)
    // Note: Yarn respects NO_COLOR and FORCE_COLOR standards
}
```

#### **Test Case: Yarn Package Manager Identification**
```csharp
[Theory]
[InlineData("yarn modern", ".yarnrc.yml exists", "YARN_VERSION", "3.x.x")]
[InlineData("yarn classic", "no .yarnrc.yml", "YARN_VERSION", "1.x.x")]
public void Yarn_Should_Set_Package_Manager_Environment_Variables(string yarnType, string detection, string expectedVar, string expectedPattern)
{
    // Expected Environment: {
    //   "YARN_VERSION": "3.6.0" or "1.22.x",
    //   "VITE_PACKAGE_MANAGER": "yarn",
    //   "npm_config_user_agent": "yarn/3.x.x"  
    // }
}
```

#### **Test Case: Yarn Workspace Environment Variables**
```csharp
[Fact]
public void Yarn_Should_Set_Workspace_Variables_When_In_Workspace()
{
    // Given: yarn workspace (package.json has "workspaces" field)
    // When: Building from workspace package
    // Expected Environment: {
    //   "YARN_WORKSPACE_NAME": "web-app",
    //   "YARN_WORKSPACE_ROOT": "/path/to/workspace/root"
    // }
}
```

### **Category 4: Yarn Custom Command Override**
```csharp
[Fact]
public void Yarn_Custom_Command_Should_Override_Script_Detection()
{
    // Given: package.json has build script + .yarnrc.yml exists + custom command
    // When: ViteBuildCommand="yarn workspace @app/web build"
    // Expected Command: "yarn workspace @app/web build" (exact custom command)
    // Expected Environment: { /* color and yarn vars still set */ }
}
```

### **Category 5: Yarn Error Handling**

#### **Test Case: Yarn Version Detection Fallback**
```csharp
[Fact]
public void Yarn_Should_Fallback_When_Version_Detection_Fails()
{
    // Given: yarn command exists but version detection fails
    // When: Building with yarn
    // Expected Behavior: Assume yarn classic, use "yarn run build"
}
```

---

## 📦 PNPM Test Cases

### **Category 1: PNPM Direct Script Execution**

#### **Test Case: PNPM Script Execution**
```csharp
[Fact]
public void PNPM_Should_Execute_Package_Scripts_With_Run()
{
    // Given: pnpm-lock.yaml exists + package.json has build script
    // When: Building with pnpm package manager
    // Expected Command: "pnpm run build"
    // Expected Environment: { /* pnpm specific vars */ }
}
```

#### **Test Case: PNPM Script with Arguments**
```csharp
[Fact]
public void PNPM_Should_Pass_Arguments_Directly_To_Scripts()
{
    // Given: pnpm + build script + additional arguments
    // When: Building with --config vite.config.ts
    // Expected Command: "pnpm run build --config \"vite.config.ts\""
    // Note: pnpm passes args directly, no -- separator needed
}
```

### **Category 2: PNPM Remote Tool Execution (DLX)**

#### **Test Case: PNPM DLX Direct Execution**
```csharp
[Fact]
public void PNPM_Should_Use_DLX_For_Direct_Vite_Execution()
{
    // Given: pnpm detected + no build script in package.json
    // When: Building with pnpm package manager
    // Expected Command: "pnpm dlx vite build"
    // Expected Environment: { /* pnpm vars */ }
}
```

#### **Test Case: PNPM DLX with Arguments**
```csharp
[Fact]
public void PNPM_DLX_Should_Handle_Vite_Arguments()
{
    // Given: pnpm dlx + vite arguments
    // When: Building with --config vite.config.ts --mode production
    // Expected Command: "pnpm dlx vite build --config \"vite.config.ts\" --mode production"
}
```

### **Category 3: PNPM Environment Variables**

#### **Test Case: PNPM Package Manager Variables**
```csharp
[Fact]
public void PNPM_Should_Set_Package_Manager_Environment_Variables()
{
    // Given: pnpm package manager detected
    // When: Building any command
    // Expected Environment: {
    //   "PNPM_VERSION": "8.x.x",
    //   "VITE_PACKAGE_MANAGER": "pnpm",
    //   "npm_config_user_agent": "pnpm/8.x.x"
    // }
}
```

#### **Test Case: PNPM Store and Cache Variables**
```csharp
[Fact] 
public void PNPM_Should_Set_Store_Location_Variables()
{
    // Given: pnpm with store configuration
    // When: Building
    // Expected Environment: {
    //   "PNPM_HOME": "/path/to/pnpm",
    //   "PNPM_STORE_PATH": "/path/to/store"
    // }
}
```

### **Category 4: PNPM Custom Command Override**
```csharp
[Fact]
public void PNPM_Custom_Command_Should_Support_Workspace_Commands()
{
    // Given: pnpm workspace + custom command
    // When: ViteBuildCommand="pnpm --filter web build"
    // Expected Command: "pnpm --filter web build"
    // Expected Environment: { /* pnpm + color vars */ }
}
```

---

## 🟠 Bun Test Cases

### **Category 1: Bun Direct Script Execution**

#### **Test Case: Bun Script Execution**
```csharp
[Fact]
public void Bun_Should_Execute_Package_Scripts_With_Run()
{
    // Given: bun.lockb exists + package.json has build script
    // When: Building with bun package manager
    // Expected Command: "bun run build"
    // Expected Environment: { /* bun specific vars */ }
}
```

#### **Test Case: Bun Fast Script Execution**
```csharp
[Fact]
public void Bun_Should_Support_Direct_Script_Names()
{
    // Given: bun + package.json script
    // When: Building (bun allows "bun build" if script exists)
    // Expected Command: "bun build" (bun can omit "run" for script names)
    // Alternative: "bun run build" (also valid)
}
```

### **Category 2: Bun Remote Tool Execution (BUNX)**

#### **Test Case: Bun BUNX Direct Execution**
```csharp
[Fact]
public void Bun_Should_Use_BUNX_For_Direct_Vite_Execution()
{
    // Given: bun detected + no build script
    // When: Building with bun package manager
    // Expected Command: "bunx vite build"
    // Expected Environment: { /* bun vars */ }
}
```

#### **Test Case: Bun BUNX with Arguments**
```csharp
[Fact]
public void Bun_BUNX_Should_Handle_Vite_Arguments()
{
    // Given: bunx + vite arguments  
    // When: Building with --config vite.config.ts
    // Expected Command: "bunx vite build --config \"vite.config.ts\""
}
```

### **Category 3: Bun Environment Variables**

#### **Test Case: Bun Runtime Environment**
```csharp
[Fact]
public void Bun_Should_Set_Bun_Specific_Environment_Variables()
{
    // Given: bun package manager
    // When: Building
    // Expected Environment: {
    //   "BUN_VERSION": "1.x.x",
    //   "VITE_PACKAGE_MANAGER": "bun",
    //   "BUN_RUNTIME": "bun"
    // }
}
```

#### **Test Case: Bun Performance Variables**
```csharp
[Fact]
public void Bun_Should_Set_Performance_Related_Variables()
{
    // Given: bun with performance optimizations
    // When: Building
    // Expected Environment: {
    //   "BUN_JSC_forceRAMSize": "auto",
    //   "NODE_ENV": "production" // when building for production
    // }
}
```

### **Category 4: Bun Custom Command Override**
```csharp
[Fact]
public void Bun_Custom_Command_Should_Support_Bun_Specific_Features()
{
    // Given: bun + custom command with bun features
    // When: ViteBuildCommand="bun --bun vite build"
    // Expected Command: "bun --bun vite build"
    // Expected Environment: { /* bun + color vars */ }
}
```

---

## 🎯 Cross-Package Manager Test Cases

### **Priority Override Tests**

#### **Test Case: Custom Command Priority Across All Package Managers**
```csharp
[Theory]
[InlineData(PackageManager.Npm, "npm run custom", "npm run custom")]
[InlineData(PackageManager.Yarn, "yarn workspace web build", "yarn workspace web build")]
[InlineData(PackageManager.Pnpm, "pnpm --filter api build", "pnpm --filter api build")]
[InlineData(PackageManager.Bun, "bun --bun build", "bun --bun build")]
public void All_Package_Managers_Should_Respect_Custom_Command_Priority(PackageManager pm, string customCommand, string expected)
{
    // Given: Any package manager + package.json script exists + custom command set
    // When: ViteBuildCommand is specified
    // Expected: Custom command takes priority over script detection
}
```

### **Environment Variable Consistency Tests**

#### **Test Case: Standard Color Variables Across All Package Managers**
```csharp
[Theory]
[InlineData(PackageManager.Npm, true, "FORCE_COLOR", "1")]
[InlineData(PackageManager.Yarn, true, "FORCE_COLOR", "1")]
[InlineData(PackageManager.Pnpm, true, "FORCE_COLOR", "1")]
[InlineData(PackageManager.Bun, true, "FORCE_COLOR", "1")]
[InlineData(PackageManager.Npm, false, "NO_COLOR", "1")]
[InlineData(PackageManager.Yarn, false, "NO_COLOR", "1")]
[InlineData(PackageManager.Pnpm, false, "NO_COLOR", "1")]
[InlineData(PackageManager.Bun, false, "NO_COLOR", "1")]
public void All_Package_Managers_Should_Use_Standard_Color_Variables(PackageManager pm, bool enableColors, string expectedVar, string expectedValue)
{
    // Ensures consistency across all package managers for color handling
}
```

### **Argument Passing Consistency Tests**

#### **Test Case: Argument Passing Patterns**
```csharp
[Theory]
[InlineData(PackageManager.Npm, "npm run build -- --config \"test.ts\"")]      // npm uses --
[InlineData(PackageManager.Yarn, "yarn run build --config \"test.ts\"")]      // yarn direct
[InlineData(PackageManager.Pnpm, "pnpm run build --config \"test.ts\"")]      // pnpm direct
[InlineData(PackageManager.Bun, "bun run build --config \"test.ts\"")]        // bun direct
public void Package_Managers_Should_Use_Correct_Argument_Passing_Pattern(PackageManager pm, string expected)
{
    // Given: Each package manager + script + additional arguments
    // When: Building with --config test.ts
    // Expected: Correct argument passing pattern for each PM
}
```

---

## 📋 Implementation Checklist

### **Current Status Assessment**

- ✅ **NPM Tests**: ~80% coverage, missing argument passing tests
- ❌ **Yarn Tests**: ~40% coverage, missing dlx and version detection
- ❌ **PNPM Tests**: ~30% coverage, missing dlx execution tests  
- ❌ **Bun Tests**: ~30% coverage, missing bunx tests
- ❌ **Environment Variables**: Wrong expectations in all color tests
- ✅ **Custom Commands**: Good coverage, missing priority tests

### **Priority Implementation Order**

1. **Fix Color Environment Variable Tests** - All package managers
2. **Add Missing Direct Execution Tests** - yarn dlx, pnpm dlx, bunx
3. **Add Argument Passing Tests** - npm --, yarn/pnpm/bun direct
4. **Add Package Manager Environment Variable Tests**
5. **Add Custom Command Priority Tests**
6. **Add Error Handling and Fallback Tests**

This document provides the **complete test case specification** you requested, organized by package manager with all the categories you specified! 🎯