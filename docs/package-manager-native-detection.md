# Package Manager Native Dependency Detection

## Philosophy: Use Package Manager Intelligence

Instead of reimplementing dependency change detection, leverage the package managers' built-in capabilities to determine when installs are needed.

## Package Manager Commands for Dependency Checking

### NPM
```bash
# Check if install is needed (exits 0 if up-to-date, non-zero if install needed)
npm ls --depth=0 --json --silent
# or
npm ci --dry-run
```

### PNPM  
```bash
# Check if install is needed
pnpm install --frozen-lockfile --dry-run
# or check if lockfile and node_modules are in sync
pnpm install --frozen-lockfile --reporter=silent
```

### Yarn (Classic & Berry)
```bash
# Check if install is needed
yarn install --frozen-lockfile --silent
# or check lockfile sync
yarn check --integrity
```

### Bun
```bash
# Check if install is needed  
bun install --dry-run
# or check lockfile sync
bun install --frozen-lockfile
```

## Enhanced MSBuild Implementation

### Strategy: Query Package Managers Directly
Instead of timestamp/hash comparisons, ask the package manager if dependencies are out of sync:

```xml
<!-- Check if package manager thinks install is needed -->
<PropertyGroup>
  <!-- NPM: Use 'npm ls' to check if dependencies are satisfied -->
  <_CheckDepsCommand Condition="'$(PackageManager)' == 'npm'">npm ls --depth=0 --json --silent</_CheckDepsCommand>
  
  <!-- PNPM: Use dry-run to check if install would do anything -->
  <_CheckDepsCommand Condition="'$(PackageManager)' == 'pnpm'">pnpm install --frozen-lockfile --dry-run --reporter=silent</_CheckDepsCommand>
  
  <!-- Yarn: Use frozen-lockfile check -->
  <_CheckDepsCommand Condition="'$(PackageManager)' == 'yarn'">yarn install --frozen-lockfile --silent</_CheckDepsCommand>
  
  <!-- Bun: Use dry-run check -->
  <_CheckDepsCommand Condition="'$(PackageManager)' == 'bun'">bun install --frozen-lockfile --dry-run</_CheckDepsCommand>
</PropertyGroup>

<Target Name="CheckDependencySync" BeforeTargets="ViteBuildAssets">
  <!-- Ask package manager if dependencies are in sync -->
  <Exec Command="$(_CheckDepsCommand)"
        WorkingDirectory="$(ViteProjectRoot)"
        ContinueOnError="true"
        ConsoleToMSBuild="true">
    <Output PropertyName="DepsCheckExitCode" TaskParameter="ExitCode" />
  </Exec>
  
  <!-- Set restore required based on package manager's assessment -->
  <PropertyGroup>
    <NodeRestoreRequired Condition="'$(DepsCheckExitCode)' != '0'">true</NodeRestoreRequired>
    <NodeRestoreReason Condition="'$(DepsCheckExitCode)' != '0'">Package manager detected dependency changes</NodeRestoreReason>
  </PropertyGroup>
</Target>
```

## Benefits of Native Package Manager Approach

### ✅ **Accuracy**
- Package managers know their own lockfile formats best
- Handles edge cases we might miss (integrity checks, platform-specific deps)
- Works correctly with workspaces/monorepos
- Respects package manager specific features (Yarn PnP, PNPM symlinks, etc.)

### ✅ **Reliability** 
- Uses battle-tested logic from package managers themselves
- Automatically supports new package manager features
- Handles version resolution correctly
- Respects .npmrc, .yarnrc.yml configurations

### ✅ **Simplicity**
- No custom hash implementations
- No need to understand lockfile formats
- No need to track package manager version differences
- Less code to maintain and test

### ✅ **Future-Proof**
- Automatically works with new package manager versions
- Supports new lockfile formats as they emerge
- Package manager optimizations benefit us automatically

## Implementation Plan

### Phase 1: Add Native Dependency Checking
- Replace timestamp-based logic with package manager queries
- Keep existing logic as fallback for offline scenarios
- Add comprehensive tests for each package manager

### Phase 2: Optimize Performance  
- Cache results for short periods to avoid repeated checks
- Use package manager flags for fastest possible checks
- Consider parallel execution for monorepos

### Phase 3: Enhanced Integration
- Use package manager warnings/errors for better user feedback
- Support package manager-specific optimizations
- Add support for custom package manager configurations

## Example Test Cases

```csharp
[Theory]
[InlineData("npm")]
[InlineData("pnpm")] 
[InlineData("yarn")]
[InlineData("bun")]
public void Should_Use_Package_Manager_To_Detect_Dependency_Changes(string packageManager)
{
    // Test that each package manager correctly identifies when install is needed
}

[Fact]
public void Should_Respect_Package_Manager_Specific_Config()
{
    // Test that .npmrc, .yarnrc.yml, etc. are respected
}

[Fact]
public void Should_Handle_Monorepo_Workspaces()
{
    // Test workspace dependency detection
}
```

## Fallback Strategy

If package manager check fails (offline, corrupted install, etc.):
1. Fall back to timestamp-based detection
2. Log warning about fallback usage  
3. Suggest running package manager manually

This gives us the best of both worlds: intelligent detection when possible, reliable fallback when needed.