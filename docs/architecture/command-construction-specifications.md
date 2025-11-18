# Vite.MsBuild Command Construction Technical Specifications

## 📋 Overview

This document defines the **authoritative specifications** for how Vite.MsBuild constructs commands across different scenarios. All test cases should align with these specifications.

## 🎯 Command Construction Priority System

Our system follows a **3-tier fallback strategy**:

1. **🔧 Custom Command Override** (Highest Priority)
2. **📦 Package Manager Scripts** (Medium Priority)  
3. **⚡ Direct Tool Execution** (Fallback)

---

## 🔧 **Tier 1: Custom Command Override**

When `ViteBuildCommand` property is set, it **overrides everything**.

### **Specification:**
```
Input:  ViteBuildCommand="npm run build:production --verbose"
Output: 
  - Executable: "npm"
  - Command:    "run build:production --verbose"
  - Full:       "npm run build:production --verbose"
```

### **Color Handling:**
- **Custom commands do NOT get automatic color flags**
- **Environment variables are still set** (`FORCE_COLOR=1` or `NO_COLOR=1`)
- **Rationale**: User has full control over custom commands

### **Test Expectations:**
```csharp
// ✅ CORRECT
command.ToString().Should().Be("npm run build:production --verbose");
command.Environment.Should().ContainKey("FORCE_COLOR").WhichValue.Should().Be("1");

// ❌ INCORRECT - Custom commands don't get automatic flags
command.ToString().Should().Contain("--color"); // WRONG!
```

---

## 📦 **Tier 2: Package Manager Scripts**

When `package.json` contains a `build` script, use the package manager to run it.

### **NPM Script Detection:**

**package.json:**
```json
{
  "scripts": {
    "build": "vite build --config vite.prod.config.ts"
  }
}
```

**Command Construction:**
```
Base Command: "npm run build"
With Config:  "npm run build -- --config \"vite.config.ts\""
With Mode:    "npm run build -- --mode production"
Full Example: "npm run build -- --config \"vite.config.ts\" --mode production --outDir \"dist\""
```

### **Package Manager Equivalents:**

| Package Manager | Script Execution | Config Passing | Example |
|----------------|------------------|----------------|---------|
| **npm** | `npm run build` | `npm run build -- --config "vite.config.ts"` | `npm run build -- --mode production` |
| **yarn** | `yarn run build` | `yarn run build --config "vite.config.ts"` | `yarn run build --mode production` |
| **pnpm** | `pnpm run build` | `pnpm run build --config "vite.config.ts"` | `pnpm run build --mode production` |
| **bun** | `bun run build` | `bun run build --config "vite.config.ts"` | `bun run build --mode production` |

### **Key Differences:**
- **npm**: Uses `--` separator before additional args
- **yarn/pnpm/bun**: Pass additional args directly

### **Test Expectations:**
```csharp
// NPM with script
var command = builder.WithPackageManager(PackageManager.Npm)
    .WithConfigPath("vite.config.ts")
    .WithMode("production")
    .Build();

command.ToString().Should().Be("npm run build -- --config \"vite.config.ts\" --mode production");

// Yarn with script  
var command = builder.WithPackageManager(PackageManager.Yarn)
    .WithConfigPath("vite.config.ts") 
    .WithMode("production")
    .Build();
    
command.ToString().Should().Be("yarn run build --config \"vite.config.ts\" --mode production");
```

---

## ⚡ **Tier 3: Direct Tool Execution (Fallback)**

When no custom command or package script exists, execute Vite directly via package manager's execution tool.

### **Package Manager Direct Execution Tools:**

| Package Manager | Direct Execution | Usage | Example |
|----------------|------------------|-------|---------|
| **npm** | `npx` | `npx vite build` | `npx vite build --config "vite.config.ts" --mode production` |
| **yarn** | `yarn dlx` (v3+) / `yarn` (v1) | `yarn dlx vite build` | `yarn dlx vite build --config "vite.config.ts" --mode production` |
| **pnpm** | `pnpm dlx` | `pnpm dlx vite build` | `pnpm dlx vite build --config "vite.config.ts" --mode production` |
| **bun** | `bunx` | `bunx vite build` | `bunx vite build --config "vite.config.ts" --mode production` |

### **Command Structure:**
```
Pattern: {PackageManagerTool} vite build {ViteArgs}

Where:
- PackageManagerTool: npx | yarn dlx | pnpm dlx | bunx
- ViteArgs: --config "path" --mode "mode" --outDir "dir" --logLevel "level"
```

### **Test Expectations:**
```csharp
// NPM direct execution
var command = new ViteCommandBuilder(projectDir, PackageManager.Npm).Build();
command.ToString().Should().Be("npx vite build");

// Yarn direct execution (modern)
var command = new ViteCommandBuilder(projectDir, PackageManager.Yarn).Build();
command.ToString().Should().Be("yarn dlx vite build");

// PNPM direct execution  
var command = new ViteCommandBuilder(projectDir, PackageManager.Pnpm).Build();
command.ToString().Should().Be("pnpm dlx vite build");

// Bun direct execution
var command = new ViteCommandBuilder(projectDir, PackageManager.Bun).Build();
command.ToString().Should().Be("bunx vite build");
```

---

## 🎨 **Color Handling Specifications**

### **Environment Variable Approach** (Current Implementation)

Colors are controlled via **environment variables**, not command-line flags:

| Color Setting | Environment Variable | Vite Behavior |
|---------------|---------------------|---------------|
| **Colors Enabled** | `FORCE_COLOR=1` | Force colored output |
| **Colors Disabled** | `NO_COLOR=1` | Force no colored output |
| **Auto (Default)** | *(no env var)* | Auto-detect based on TTY |

### **Rationale:**
1. **Universal Standard**: `NO_COLOR` and `FORCE_COLOR` are widely supported
2. **Framework Agnostic**: Works with Vite, PostCSS, ESLint, Prettier, etc.
3. **CI/CD Friendly**: Better for automated environments
4. **Consistent**: Same mechanism across all execution paths

### **Test Expectations:**
```csharp
// ✅ CORRECT - Test environment variables
command.Environment.Should().ContainKey("FORCE_COLOR").WhichValue.Should().Be("1");
command.Environment.Should().ContainKey("NO_COLOR").WhichValue.Should().Be("1");

// ❌ INCORRECT - Don't test for command-line flags
command.ToString().Should().Contain("--color");     // WRONG!
command.ToString().Should().Contain("--no-color");  // WRONG!
```

---

## 🏗️ **Command Object Structure**

### **ViteCommand Properties:**

```csharp
public class ViteCommand
{
    public string Executable { get; }      // Package manager + tool
    public string Command { get; }         // Arguments after executable  
    public string WorkingDirectory { get; }
    public Dictionary<string, string> Environment { get; }
    
    // Consolidated command representation
    public override string ToString() => $"{Executable} {Command}".Trim();
}
```

### **Property Value Examples:**

| Scenario | Executable | Command | ToString() |
|----------|------------|---------|------------|
| **Custom Command** | `"npm"` | `"run build:production --verbose"` | `"npm run build:production --verbose"` |
| **NPM Script** | `"npm"` | `"run build -- --config \"vite.config.ts\""` | `"npm run build -- --config \"vite.config.ts\""` |
| **NPX Direct** | `"npx"` | `"vite build --config \"vite.config.ts\""` | `"npx vite build --config \"vite.config.ts\""` |
| **Yarn DLX** | `"yarn"` | `"dlx vite build --config \"vite.config.ts\""` | `"yarn dlx vite build --config \"vite.config.ts\""` |

### **Test Expectations:**
```csharp
// ✅ CORRECT - Test the complete command string
command.ToString().Should().Be("npx vite build --config \"vite.config.ts\"");

// ✅ ALSO CORRECT - Test individual properties if needed
command.Executable.Should().Be("npx");
command.Command.Should().Be("vite build --config \"vite.config.ts\"");

// ❌ INCORRECT - Don't expect wrong property values
command.Executable.Should().Be("npx vite"); // WRONG! "vite" is part of Command
```

---

## 📂 **Argument Construction Rules**

### **Argument Priority Order:**
1. `--config "path"` (if specified)
2. `--mode "mode"` (if specified)  
3. `--outDir "directory"` (if specified)
4. `--logLevel "level"` (if specified)

### **Path Handling:**
- **Always quote paths** containing spaces: `--config "My Folder/vite.config.ts"`
- **Use forward slashes** for cross-platform compatibility
- **Relative to WorkingDirectory**

### **Mode Mapping:**
```
MSBuild Configuration → Vite Mode
Debug                 → development
Release               → production
(Custom)              → (pass through)
```

### **Log Level Mapping:**
```
MSBuild Verbosity → Vite Log Level
quiet            → silent
minimal          → warn  
normal           → info
detailed         → info
diagnostic       → debug
```

### **Test Expectations:**
```csharp
var command = builder
    .WithConfigPath("config/vite.config.ts")
    .WithMode("staging")
    .WithOutputDir("dist/staging")
    .WithLogLevel("debug")
    .Build();

command.ToString().Should().Be(
    "npx vite build --config \"config/vite.config.ts\" --mode staging --outDir \"dist/staging\" --logLevel debug"
);
```

---

## 🧪 **Test Case Categories**

### **1. Zero-Config Tests**
```csharp
[Fact]
public void Should_Work_With_Zero_Configuration()
{
    var command = new ViteCommandBuilder(projectDir, PackageManager.Npm).Build();
    command.ToString().Should().Be("npx vite build");
}
```

### **2. Custom Command Tests**
```csharp
[Fact] 
public void Should_Use_Custom_Command_Override()
{
    var command = builder.WithCustomCommand("npm run build:production").Build();
    command.ToString().Should().Be("npm run build:production");
    // Environment variables are still set for color control
    command.Environment.Should().ContainKey("FORCE_COLOR");
}
```

### **3. Package Script Tests**
```csharp
[Fact]
public void Should_Use_Package_Scripts_When_Available()
{
    // Arrange: Create package.json with build script
    var packageJson = new { scripts = new { build = "vite build --watch" } };
    File.WriteAllText(Path.Combine(projectDir, "package.json"), JsonSerializer.Serialize(packageJson));
    
    var command = builder.Build();
    command.ToString().Should().Be("npm run build");
}
```

### **4. Direct Execution Tests**
```csharp
[Theory]
[InlineData(PackageManager.Npm, "npx vite build")]
[InlineData(PackageManager.Yarn, "yarn dlx vite build")]  
[InlineData(PackageManager.Pnpm, "pnpm dlx vite build")]
[InlineData(PackageManager.Bun, "bunx vite build")]
public void Should_Use_Direct_Execution_Tools(PackageManager pm, string expected)
{
    var command = new ViteCommandBuilder(projectDir, pm).Build();
    command.ToString().Should().Be(expected);
}
```

### **5. Color Control Tests**
```csharp
[Theory]
[InlineData(true, "FORCE_COLOR", "1")]
[InlineData(false, "NO_COLOR", "1")]
public void Should_Set_Color_Environment_Variables(bool enableColors, string expectedKey, string expectedValue)
{
    var command = builder.WithColors(enableColors).Build();
    command.Environment.Should().ContainKey(expectedKey).WhichValue.Should().Be(expectedValue);
    
    // Should NOT contain command-line color flags
    command.ToString().Should().NotContain("--color");
    command.ToString().Should().NotContain("--no-color");
}
```

### **6. Complex Configuration Tests**
```csharp
[Fact]
public void Should_Handle_Complex_Multi_Option_Commands()
{
    var command = builder
        .WithPackageManager(PackageManager.Pnpm)
        .WithConfigPath("apps/web/vite.config.ts") 
        .WithMode("production")
        .WithOutputDir("dist/web")
        .WithLogLevel("info")
        .WithColors(false)
        .Build();
        
    command.ToString().Should().Be(
        "pnpm dlx vite build --config \"apps/web/vite.config.ts\" --mode production --outDir \"dist/web\" --logLevel info"
    );
    command.Environment.Should().ContainKey("NO_COLOR").WhichValue.Should().Be("1");
}
```

---

## ✅ **Test Update Action Items**

Based on this specification, here are the **specific test changes needed**:

### **1. Color Tests - Update Expectations**
```diff
// OLD (incorrect)
- command.ToString().Should().Contain("--no-color");
- command.ToString().Should().Contain("--color");

// NEW (correct)
+ command.Environment.Should().ContainKey("NO_COLOR").WhichValue.Should().Be("1");
+ command.Environment.Should().ContainKey("FORCE_COLOR").WhichValue.Should().Be("1");
```

### **2. Executable Tests - Update Expectations**
```diff
// OLD (incorrect)
- command.Executable.Should().Be("npx");

// NEW (correct - depends on what we're testing)
+ command.ToString().Should().Be("npx vite build");
// OR if testing properties individually:
+ command.Executable.Should().Be("npx");
+ command.Command.Should().Be("vite build");
```

### **3. Package Manager Tests - Verify Correctness**
```diff
// Ensure we're testing the right direct execution tools
+ [InlineData(PackageManager.Npm, "npx vite build")]
+ [InlineData(PackageManager.Yarn, "yarn dlx vite build")]  
+ [InlineData(PackageManager.Pnpm, "pnpm dlx vite build")]
+ [InlineData(PackageManager.Bun, "bunx vite build")]
```

This specification provides the **authoritative reference** for what our command construction should produce and what our tests should expect! 🎯