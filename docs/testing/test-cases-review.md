# ViteKit.Msbuild Test Cases Review Document

## 📋 Purpose

This document reviews our **current test cases** for completeness, correctness, and alignment with our [Command Construction Specifications](./command-construction-specifications.md). 

We need to identify:
- ✅ Tests that are **correct** and should pass
- ❌ Tests with **wrong expectations** that need updates  
- 🚫 **Missing test coverage** for important scenarios
- 🔄 Tests that conflict with our **3-tier fallback strategy**

---

## 📊 Current Test Suite Status

**Total Tests**: 72  
**Passing**: 62 ✅  
**Failing**: 10 ❌  

---

## 🔍 Failing Tests Analysis

### **Group 1: Color Handling Expectation Mismatches**

#### Test: `Should_Control_Color_Output`
**Location**: `CommandValidationTests.cs:211`  
**Current Expectation**: 
```csharp
command.ToString().Should().Contain("--no-color");  // WRONG!
command.ToString().Should().Contain("--color");     // WRONG!
```

**Correct Expectation** (per our spec):
```csharp
command.Environment.Should().ContainKey("NO_COLOR").WhichValue.Should().Be("1");
command.Environment.Should().ContainKey("FORCE_COLOR").WhichValue.Should().Be("1");
```

**Verdict**: ❌ **Test expectation is wrong** - Update needed

---

#### Test: `Should_Build_Complete_Command_With_All_Options` 
**Location**: `CommandValidationTests.cs:117`  
**Current Issue**: Expects `--no-color` flag in command string  
**Verdict**: ❌ **Test expectation is wrong** - Should test environment variables instead

---

#### Test: `Should_Demonstrate_Real_World_Namespaced_Usage`
**Location**: `NamespacedSyntaxTests.cs:279`  
**Current Issue**: Expects `--color` flag in command line  
**Verdict**: ❌ **Test expectation is wrong** - Should test environment variables

---

### **Group 2: Command Structure Expectation Mismatches**

#### Test: `Should_Handle_Invalid_Package_Json_Gracefully`
**Location**: `ViteCommandBuilderTests.cs:183`  
**Current Expectation**: 
```csharp
command.Executable.Should().Be("npx");  // Expects just "npx"
```
**Actual Implementation**: Returns `"npx vite"`

**Analysis**: This reveals an **implementation inconsistency**:
- For **scripts**: `Executable = "npm"`, `Command = "run build"`  
- For **direct**: `Executable = "npx vite"`, `Command = "build"`

**Verdict**: 🔄 **Architecture decision needed** - Should we fix implementation or update spec?

---

#### Test: `Should_Fallback_To_Direct_Command_When_No_Script`
**Location**: `ViteCommandBuilderTests.cs:71`  
**Same Issue**: Expects `Executable = "npx"` but gets `"npx vite"`

**Verdict**: 🔄 **Same architectural decision as above**

---

### **Group 3: Environment Variable Count Mismatches**

#### Test: `Should_Handle_Complex_Command_Construction`
**Location**: `ErrorScenarioValidationTests.cs:149`  
**Current Issue**: 
```csharp
// Expected: 3 environment variables  
// Actual: 4 environment variables (includes NO_COLOR=1)
command.Environment.Should().HaveCount(3);  // WRONG!
```

**Analysis**: Test didn't account for automatic color environment variable injection

**Verdict**: ❌ **Test expectation is wrong** - Should expect 4 variables or test specific keys

---

## 📝 Test Coverage Analysis

### **✅ Well-Covered Scenarios**

1. **Zero-config experience** - ✅ Good coverage
2. **Custom command overrides** - ✅ Good coverage  
3. **Package manager variety** - ✅ Good coverage
4. **Multi-config scenarios** - ✅ Good coverage
5. **Error handling** - ✅ Good coverage

### **🚫 Missing Test Coverage**

#### **1. Package Manager Script Argument Passing**
**Missing Tests**:
```csharp
// NPM: Should use -- separator
"npm run build -- --config vite.config.ts --mode production"

// Yarn/PNPM/Bun: Should pass args directly  
"yarn run build --config vite.config.ts --mode production"
"pnpm run build --config vite.config.ts --mode production"
"bun run build --config vite.config.ts --mode production"
```

#### **2. Direct Execution Tool Mapping**
**Missing Tests**:
```csharp
[Theory]
[InlineData(PackageManager.Npm, "npx")]
[InlineData(PackageManager.Yarn, "yarn")]  // should be yarn dlx  
[InlineData(PackageManager.Pnpm, "pnpm")]  // should be pnpm dlx
[InlineData(PackageManager.Bun, "bun")]    // should be bunx
public void Should_Use_Correct_Direct_Execution_Tools(PackageManager pm, string expectedTool)
```

#### **3. Yarn Version Detection**
**Missing Tests**:
```csharp
[Fact]
public void Should_Detect_Yarn_Berry_From_YarnRc()
{
    // Test .yarnrc.yml detection logic
}

[Fact]
public void Should_Use_Yarn_Classic_Commands_When_No_YarnRc()
{
    // Test fallback to yarn v1 behavior
}
```

#### **4. Environment Variable Precedence**
**Missing Tests**:
```csharp
[Fact] 
public void Should_Not_Override_Existing_Color_Environment_Variables()
{
    // If NO_COLOR already set in _environment, don't override it
    // If FORCE_COLOR already set, don't override it
}
```

#### **5. Path Quoting and Cross-Platform**
**Missing Tests**:
```csharp
[Theory]
[InlineData("simple.config.ts", "simple.config.ts")]          // No quotes needed
[InlineData("path with spaces.ts", "\"path with spaces.ts\"")]  // Quotes needed
[InlineData(@"windows\path.ts", "windows/path.ts")]            // Path normalization
public void Should_Handle_Path_Quoting_Correctly(string input, string expected)
```

#### **6. Custom Command with Environment Variables**
**Missing Tests**:
```csharp
[Fact]
public void Should_Set_Environment_Variables_For_Custom_Commands()
{
    // Even with custom commands, environment variables should be set
    var command = builder
        .WithCustomCommand("npm run build:production") 
        .WithColors(false)
        .Build();
        
    command.Environment.Should().ContainKey("NO_COLOR");
    // But command string should NOT contain --no-color flags
}
```

---

## 🎯 Test Architecture Issues

### **Issue 1: Inconsistent Property Testing**

Some tests check `command.ToString()`, others check `command.Executable` and `command.Command` separately. We need consistency.

**Recommendation**: 
- **Primary**: Test `command.ToString()` for complete command validation
- **Secondary**: Test individual properties only when testing specific property logic

### **Issue 2: Missing Package.json Setup**

Many tests don't properly set up `package.json` files, making it unclear whether they're testing script detection or direct execution.

**Recommendation**: Explicit test setup:
```csharp
// For script tests
CreatePackageJsonWithBuildScript("vite build --watch");

// For direct execution tests  
CreateEmptyProjectDirectory(); // No package.json
```

### **Issue 3: Environment Variable Testing Strategy**

Current tests inconsistently check environment variables. Need standardized approach.

**Recommendation**:
```csharp
// Standard environment variable validation
private void AssertColorEnvironment(ViteCommand command, bool colorsEnabled)
{
    if (colorsEnabled)
    {
        command.Environment.Should().ContainKey("FORCE_COLOR").WhichValue.Should().Be("1");
        command.Environment.Should().NotContainKey("NO_COLOR");
    }
    else
    {
        command.Environment.Should().ContainKey("NO_COLOR").WhichValue.Should().Be("1"); 
        command.Environment.Should().NotContainKey("FORCE_COLOR");
    }
}
```

---

## 📋 Action Items by Priority

### **Priority 1: Fix Failing Tests (Quick Wins)**

1. **Update color tests** - Change from flag expectations to environment variable expectations
2. **Update environment count tests** - Account for automatic color environment variables
3. **Decide on command structure** - Executable vs Command property split

### **Priority 2: Add Missing Critical Coverage**

1. **Package manager script argument passing** - Test `--` separator logic
2. **Direct execution tool mapping** - Verify npx/dlx/bunx usage
3. **Environment variable precedence** - Test MSBuild vs builder precedence

### **Priority 3: Architectural Improvements**

1. **Standardize test helpers** - Consistent package.json setup
2. **Unified assertion patterns** - Standard environment variable validation
3. **Test organization** - Group by feature rather than by test type

---

## 🔧 Specific Test Updates Needed

### **File: CommandValidationTests.cs**

```diff
[Theory]
[InlineData(false, "NO_COLOR", "1")]
[InlineData(true, "FORCE_COLOR", "1")]
- public void Should_Control_Color_Output(bool enableColors, string expectedFlag)
+ public void Should_Control_Color_Output(bool enableColors, string expectedEnvVar, string expectedValue)
{
    var command = new ViteCommandBuilder(projectDir, PackageManager.Npm)
        .WithColors(enableColors)
        .Build();

-   command.ToString().Should().Contain(expectedFlag);
+   command.Environment.Should().ContainKey(expectedEnvVar).WhichValue.Should().Be(expectedValue);
+   
+   // Should NOT contain command-line flags
+   command.ToString().Should().NotContain("--color");
+   command.ToString().Should().NotContain("--no-color");
}
```

### **File: ViteCommandBuilderTests.cs**

```diff
[Fact]
public void Should_Fallback_To_Direct_Command_When_No_Script()
{
    // Arrange: No package.json (direct execution)
    var projectDir = CreateEmptyProjectDirectory();
    
    var command = new ViteCommandBuilder(projectDir, PackageManager.Npm).Build();
    
-   command.Executable.Should().Be("npx");
+   command.ToString().Should().Be("npx vite build");
+   
+   // OR if testing properties individually:
+   command.Executable.Should().Be("npx");
+   command.Command.Should().Be("vite build");
}
```

### **File: ErrorScenarioValidationTests.cs**

```diff
[Fact]
public void Should_Handle_Complex_Command_Construction()
{
    var command = builder
        .WithEnvironment("NODE_ENV", "staging")
        .WithEnvironment("VITE_API_URL", "https://api.staging.com") 
        .WithEnvironment("VITE_APP_VERSION", "1.2.3")
        .WithColors(false)  // This adds NO_COLOR=1
        .Build();

-   command.Environment.Should().HaveCount(3);
+   command.Environment.Should().HaveCount(4); // Includes NO_COLOR
+   
+   // Or test specific keys instead of count
+   command.Environment.Should().ContainKey("NODE_ENV");
+   command.Environment.Should().ContainKey("VITE_API_URL");  
+   command.Environment.Should().ContainKey("VITE_APP_VERSION");
+   command.Environment.Should().ContainKey("NO_COLOR").WhichValue.Should().Be("1");
}
```

---

## 🎯 Summary

Our test suite is **86% passing** and covers most core functionality well. The failing tests are primarily **expectation mismatches** rather than functional issues, which is a good sign!

**Key Issues**:
1. **Color handling** - Tests expect flags, implementation uses environment variables  
2. **Command structure** - Inconsistent Executable vs Command property split
3. **Missing edge cases** - Package manager specific behaviors need more coverage

**Next Steps**:
1. **Review** this document with the team
2. **Decide** on the Executable/Command property split architecture
3. **Update** failing tests with correct expectations  
4. **Add** missing test coverage for critical scenarios

This analysis shows we have a **solid foundation** and just need **alignment** between specifications, implementation, and test expectations! 🎯