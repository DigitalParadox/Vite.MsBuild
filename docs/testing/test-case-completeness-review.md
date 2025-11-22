# ViteKit.Msbuild Test Case Completeness Review

## 📋 Purpose

Review our current test cases to identify gaps in coverage and ensure we're testing all critical scenarios for:
- **Package manager command construction** (npm, yarn, pnpm, bun)
- **Vite direct calls** (npx, yarn dlx, pnpm dlx, bunx) 
- **NPX and equivalent constructs** for other package managers
- **Custom command definition** scenarios

## 🔍 Current Test Cases Inventory

### **Package Manager Command Construction Tests**

#### **Existing Tests:**
```
✅ ViteCommandBuilderTests.Should_Build_Complete_Command_With_All_Options
✅ ViteCommandBuilderTests.Should_Use_Package_Manager_When_Available
✅ MultiPackageManagerTests.Should_Detect_Multiple_Package_Managers
✅ MultiPackageManagerTests.Should_Prefer_Lock_File_Over_Executable
```

#### **Coverage Analysis:**

| Package Manager | Script Detection | Argument Passing | Direct Execution | Status |
|----------------|------------------|------------------|------------------|--------|
| **npm** | ✅ Tested | ❓ Partial | ✅ Tested | **Needs arg passing** |
| **yarn** | ✅ Tested | ❓ Missing | ❓ Missing | **Needs dlx tests** |
| **pnpm** | ✅ Tested | ❓ Missing | ❓ Missing | **Needs dlx tests** |
| **bun** | ✅ Tested | ❓ Missing | ❓ Missing | **Needs bunx tests** |

#### **Missing Test Cases:**

**1. NPM Argument Passing with `--` Separator:**
```csharp
// MISSING TEST
[Fact]
public void Should_Use_NPM_Double_Dash_For_Script_Arguments()
{
    // When: package.json has "build" script + additional args
    // Then: "npm run build -- --config vite.config.ts --mode production"
}
```

**2. Yarn Direct Arguments (No `--` separator):**
```csharp
// MISSING TEST  
[Fact]
public void Should_Pass_Yarn_Script_Arguments_Directly()
{
    // When: yarn + build script + args
    // Then: "yarn run build --config vite.config.ts --mode production"
}
```

**3. Package Manager Script vs No Script:**
```csharp
// MISSING TESTS
[Theory]
[InlineData("npm", "package.json with build script", "npm run build")]
[InlineData("npm", "package.json without build script", "npx vite build")]
public void Should_Choose_Script_Or_Direct_Based_On_Package_Json(string pm, string scenario, string expected)
```

---

### **Direct Execution (NPX Equivalent) Tests**

#### **Existing Tests:**
```
✅ ViteCommandBuilderTests.Should_Fallback_To_Direct_Command_When_No_Script
✅ CommandValidationTests.Should_Execute_Zero_Config_Experience  
```

#### **Coverage Analysis:**

| Package Manager | Direct Tool | Current Test | Status |
|----------------|-------------|--------------|--------|
| **npm** | `npx vite` | ✅ Tested | **Complete** |
| **yarn** | `yarn dlx vite` | ❌ Missing | **Need test** |
| **pnpm** | `pnpm dlx vite` | ❌ Missing | **Need test** |
| **bun** | `bunx vite` | ❌ Missing | **Need test** |

#### **Missing Test Cases:**

**1. Yarn DLX Direct Execution:**
```csharp
// MISSING TEST
[Fact]
public void Should_Use_Yarn_DLX_For_Direct_Vite_Execution()
{
    // When: yarn detected + no build script
    // Then: "yarn dlx vite build --config ..."
}
```

**2. PNPM DLX Direct Execution:**
```csharp
// MISSING TEST
[Fact] 
public void Should_Use_PNPM_DLX_For_Direct_Vite_Execution()
{
    // When: pnpm detected + no build script  
    // Then: "pnpm dlx vite build --config ..."
}
```

**3. Bun Direct Execution:**
```csharp
// MISSING TEST
[Fact]
public void Should_Use_BUNX_For_Direct_Vite_Execution()
{
    // When: bun detected + no build script
    // Then: "bunx vite build --config ..."
}
```

**4. Yarn Version Detection for DLX:**
```csharp
// MISSING TEST
[Theory]
[InlineData(".yarnrc.yml exists", "yarn dlx vite build")]     // Yarn Berry
[InlineData("No .yarnrc.yml", "yarn vite build")]            // Yarn Classic
public void Should_Detect_Yarn_Version_For_Direct_Execution(string scenario, string expected)
```

---

### **Custom Command Definition Tests**

#### **Existing Tests:**
```
✅ ViteCommandBuilderTests.Should_Use_Custom_Command_When_Specified
✅ NamespacedSyntaxTests.Should_Support_Custom_Build_Commands
✅ MSBuildTaskIntegrationTests.Should_Support_Dynamic_MSBuild_Properties
```

#### **Coverage Analysis:**

| Custom Command Scenario | Test Exists | Status |
|-------------------------|-------------|--------|
| **Simple override** | ✅ Tested | **Complete** |
| **Complex with args** | ✅ Tested | **Complete** |
| **Environment vars with custom** | ❓ Partial | **Needs clarity** |
| **Custom + package manager interaction** | ❌ Missing | **Need test** |

#### **Missing Test Cases:**

**1. Custom Command Priority Over Package Scripts:**
```csharp
// MISSING TEST
[Fact]
public void Should_Use_Custom_Command_Even_When_Package_Script_Exists()
{
    // Given: package.json has build script + custom command set
    // When: ViteBuildCommand="npm run build:production --verbose" 
    // Then: Should use custom command, ignore package.json script
}
```

**2. Custom Command with Color Environment Variables:**
```csharp
// MISSING TEST  
[Fact]
public void Should_Set_Color_Environment_Variables_With_Custom_Commands()
{
    // Given: Custom command + color settings
    // When: ViteBuildCommand="npm run custom" + EnableColors=false
    // Then: Command = "npm run custom" AND Environment = { NO_COLOR: "1" }
}
```

---

### **Color and Environment Variable Tests**

#### **Existing Tests:**
```
✅ CommandValidationTests.Should_Control_Color_Output
✅ ErrorScenarioValidationTests.Should_Handle_Complex_Command_Construction  
```

#### **Coverage Analysis:**

| Color Scenario | Current Test | Expected Behavior | Status |
|----------------|--------------|------------------|--------|
| **Colors enabled** | ❓ Wrong expectation | `FORCE_COLOR=1` env var | **Fix needed** |
| **Colors disabled** | ❓ Wrong expectation | `NO_COLOR=1` env var | **Fix needed** |
| **Auto colors (default)** | ❌ Missing | No color env vars | **Need test** |
| **MSBuild color override** | ❌ Missing | MSBuild vars win | **Need test** |

#### **Missing Test Cases:**

**1. Default Color Behavior:**
```csharp
// MISSING TEST
[Fact]
public void Should_Not_Set_Color_Variables_By_Default()
{
    // When: No explicit color setting
    // Then: No FORCE_COLOR or NO_COLOR in environment
}
```

**2. MSBuild Environment Variable Precedence:**
```csharp
// MISSING TEST
[Fact]
public void Should_Respect_Existing_Color_Environment_Variables()
{
    // Given: MSBuild already set NO_COLOR=1
    // When: ViteCommandBuilder also tries to set colors
    // Then: Don't override existing environment variable
}
```

---

### **Path Handling and Argument Construction Tests**

#### **Existing Tests:**
```
✅ CommandValidationTests.Should_Build_Complete_Command_With_All_Options
✅ MultiConfigTests.Should_Handle_Complex_Path_Configurations
```

#### **Missing Test Cases:**

**1. Path Quoting:**
```csharp
// MISSING TESTS
[Theory]
[InlineData("vite.config.ts", "vite.config.ts")]                    // No quotes
[InlineData("config with spaces.ts", "\"config with spaces.ts\"")]  // Quotes needed
[InlineData("path/to/config.ts", "path/to/config.ts")]             // No quotes
public void Should_Quote_Paths_With_Spaces(string input, string expected)
```

**2. Cross-Platform Path Normalization:**
```csharp
// MISSING TEST
[Theory]
[InlineData(@"windows\style\path.ts", "windows/style/path.ts")]
[InlineData("unix/style/path.ts", "unix/style/path.ts")]
public void Should_Normalize_Paths_For_Cross_Platform(string input, string expected)
```

---

### **Error Handling and Edge Cases**

#### **Existing Tests:**
```
✅ ErrorScenarioValidationTests.Should_Handle_Missing_Package_Json
✅ ErrorScenarioValidationTests.Should_Handle_Invalid_Package_Json  
✅ ViteCommandBuilderTests.Should_Handle_Invalid_Package_Json_Gracefully
```

#### **Missing Test Cases:**

**1. Package Manager Not Found:**
```csharp
// MISSING TEST
[Fact]
public void Should_Fallback_To_NPM_When_Package_Manager_Not_Found()
{
    // Given: lock file says "pnpm" but pnpm not installed
    // When: Building command
    // Then: Should fallback to npm/npx
}
```

**2. Empty or Malformed Scripts:**
```csharp
// MISSING TEST
[Theory]
[InlineData("{ \"scripts\": {} }")]                    // Empty scripts
[InlineData("{ \"scripts\": { \"build\": \"\" } }")]  // Empty build script  
[InlineData("{ \"scripts\": { \"test\": \"jest\" } }")] // No build script
public void Should_Handle_Malformed_Package_Scripts(string packageJsonContent)
```

---

## 📊 Test Coverage Summary

### **Current Coverage Assessment:**

| Category | Tests Exist | Coverage % | Critical Gaps |
|----------|-------------|------------|---------------|
| **NPM** | ✅ Good | ~80% | Argument passing with `--` |
| **Yarn** | ⚠️ Partial | ~40% | DLX usage, version detection |
| **PNPM** | ⚠️ Partial | ~30% | DLX direct execution |
| **Bun** | ⚠️ Partial | ~30% | BUNX direct execution |
| **Custom Commands** | ✅ Good | ~70% | Priority testing, env vars |
| **Color Handling** | ❌ Wrong | ~20% | Environment variable approach |
| **Path Handling** | ⚠️ Basic | ~50% | Quoting, cross-platform |
| **Error Cases** | ✅ Good | ~75% | Package manager fallbacks |

### **Priority Gaps to Fill:**

#### **🔴 Critical (Must Fix):**
1. **Color environment variables** - All color tests have wrong expectations
2. **Yarn/PNPM/Bun direct execution** - Missing `dlx`/`bunx` test coverage
3. **NPM script argument passing** - Missing `--` separator tests

#### **🟡 Important (Should Add):**
1. **Custom command priority** - Test override behavior
2. **MSBuild environment precedence** - Test variable conflicts
3. **Path quoting and normalization** - Cross-platform compatibility

#### **🟢 Nice to Have (Future):**
1. **Package manager fallback** - When tools not found
2. **Performance testing** - Command construction speed
3. **Integration edge cases** - Complex monorepo scenarios

---

## 🎯 Recommended Test Implementation Priority

### **Phase 1: Fix Existing Test Expectations**
- Update 7 color tests to expect environment variables
- Fix command structure expectations (Executable vs Command)

### **Phase 2: Add Critical Missing Coverage**
- Add yarn dlx, pnpm dlx, bunx direct execution tests
- Add NPM `--` separator argument passing tests
- Add custom command priority tests

### **Phase 3: Add Path and Environment Handling**
- Add path quoting and cross-platform normalization tests
- Add MSBuild environment variable precedence tests

### **Phase 4: Error Handling Edge Cases**
- Add package manager fallback tests
- Add malformed package.json handling tests

This analysis shows we have **good foundational coverage** but are missing **package manager specific behaviors** and have **incorrect color handling expectations**. The gaps are focused and addressable! 🎯