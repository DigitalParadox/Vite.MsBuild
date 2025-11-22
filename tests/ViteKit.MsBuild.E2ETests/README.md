# E2E Tests - Test Explorer Setup

## Issue: Tests Not Appearing in Test Explorer

If E2E tests are not appearing in Visual Studio Test Explorer or Rider's test runner, try these steps:

### **For Visual Studio:**

1. **Rebuild Solution**
   ```
   Right-click solution → Clean Solution
   Right-click solution → Rebuild Solution
   ```

2. **Clear Test Explorer Cache**
   ```
   Test → Configure Run Settings → Select Solution Wide runsettings File → tests/ViteKit.MsBuild.E2ETests/xunit.runsettings
   ```
   
   Then:
   ```
   Test → Test Explorer → Clear All Results
   ```

3. **Reset VS Test Platform**
   - Close Visual Studio
   - Delete `%TEMP%\.vs\*` folders
   - Delete `.vs` folder in solution directory
   - Reopen Visual Studio

4. **Update Test Adapter**
   - Tools → Extensions and Updates
   - Update "Test Adapter for xUnit"

### **For JetBrains Rider:**

1. **Invalidate Caches**
   ```
   File → Invalidate Caches → Invalidate and Restart
   ```

2. **Rebuild Solution**
   ```
   Build → Rebuild Solution
   ```

3. **Check Unit Testing Settings**
   ```
   File → Settings → Build, Execution, Deployment → Unit Testing → xUnit.net
   Ensure "Enable xUnit.net support" is checked
   ```

4. **Re-run Discovery**
   - Right-click on E2E test project
   - Select "Run Unit Tests" or "Debug Unit Tests"
   - Tests should appear after first run

### **For VS Code:**

1. **Install Extensions**
   - .NET Core Test Explorer
   - C# Dev Kit

2. **Reload Window**
   ```
   Ctrl+Shift+P → "Developer: Reload Window"
   ```

3. **Check Test Log**
   ```
   View → Output → Select ".NET Core Test Explorer" from dropdown
   ```

### **Command Line (Always Works):**

```powershell
# List all tests
dotnet test tests/ViteKit.MsBuild.E2ETests --list-tests

# Run specific test
dotnet test tests/ViteKit.MsBuild.E2ETests --filter "FullyQualifiedName~EdgeCaseTests"

# Run all E2E tests
dotnet test tests/ViteKit.MsBuild.E2ETests
```

## Verification

Run this to verify tests are discoverable:

```powershell
dotnet test tests/ViteKit.MsBuild.E2ETests --list-tests
```

You should see **36 tests** listed.

## Common Issues

### Issue: "No tests discovered"
**Solution:** Ensure project has `<PackageReference Include="xunit.runner.visualstudio" />` and rebuild.

### Issue: Tests show but don't run
**Solution:** The test projects require npm/node to be installed since they run real MSBuild commands that invoke Vite.

### Issue: Tests are slow to appear
**Solution:** First test run triggers discovery. Subsequent runs are faster.

## Test Structure

E2E tests execute **real `dotnet build` commands** against actual ASP.NET Core projects in `TestProjects/` directory. They verify:

- Incremental builds
- Multi-SPA configurations  
- Custom build scripts
- Output directory overrides
- MSBuild integration
- Dependency ordering
- And more...

See individual test classes for details.
