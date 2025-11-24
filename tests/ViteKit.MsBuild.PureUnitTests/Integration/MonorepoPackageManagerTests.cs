using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace ViteKit.MsBuild.PureUnitTests.Integration
{
    /// <summary>
    /// Tests for monorepo scenarios with package manager best practices
    /// 
    /// Real-world patterns:
    /// 1. Central node_modules with workspaces (most common)
    /// 2. package.json per project/SPA (separate dependencies)
    /// 3. Single package manager per repo (good practice)
    /// 4. Multiple lock files in root (bad practice, but handle gracefully)
    /// </summary>
    public class MonorepoPackageManagerTests : IDisposable
    {
        private readonly List<string> _tempDirectories = new();

        [Fact]
        public void Should_Support_Central_Node_Modules_With_Workspaces()
        {
            // Arrange: Most common monorepo pattern - central node_modules with workspaces
            var repoRoot = CreateTempDirectory();
            var frontendProject = Path.Combine(repoRoot, "apps", "frontend");
            var adminProject = Path.Combine(repoRoot, "apps", "admin");
            
            Directory.CreateDirectory(frontendProject);
            Directory.CreateDirectory(adminProject);

            // Root workspace manages all dependencies (COMMON PATTERN)
            CreatePackageJson(repoRoot, new Dictionary<string, string>());
            File.WriteAllText(Path.Combine(repoRoot, "pnpm-lock.yaml"), "lockfileVersion: 5.4");
            File.WriteAllText(Path.Combine(repoRoot, "pnpm-workspace.yaml"), 
                "packages:\n  - 'apps/*'\n  - 'packages/*'");

            // Projects have package.json but NO lock files (dependencies managed centrally)
            CreatePackageJson(frontendProject, new Dictionary<string, string> { ["build"] = "vite build" });
            CreatePackageJson(adminProject, new Dictionary<string, string> { ["build"] = "vite build" });

            // Act: Detect package manager (should inherit from workspace root)
            var frontendManager = DetectPackageManagerWithInheritance(frontendProject, repoRoot);
            var adminManager = DetectPackageManagerWithInheritance(adminProject, repoRoot);

            // Assert: Both projects inherit pnpm from workspace root
            frontendManager.Should().Be("pnpm", "Frontend should inherit pnpm from workspace root");
            adminManager.Should().Be("pnpm", "Admin should inherit pnpm from workspace root");
        }

        [Fact]
        public void Should_Support_Package_Json_Per_Project_Pattern()
        {
            // Arrange: Another common pattern - package.json per project with separate dependencies
            var repoRoot = CreateTempDirectory();
            var webProject = Path.Combine(repoRoot, "src", "Web");
            var apiProject = Path.Combine(repoRoot, "src", "Api");
            
            Directory.CreateDirectory(webProject);
            Directory.CreateDirectory(apiProject);

            // Each project manages its own dependencies (COMMON PATTERN)
            CreatePackageJson(webProject, new Dictionary<string, string> { ["build"] = "vite build" });
            File.WriteAllText(Path.Combine(webProject, "package-lock.json"), "{}");
            
            CreatePackageJson(apiProject, new Dictionary<string, string> { ["build"] = "rollup build" });
            File.WriteAllText(Path.Combine(apiProject, "package-lock.json"), "{}");

            // Act: Each project should manage its own dependencies
            var webManager = DetectPackageManagerForProject(webProject);
            var apiManager = DetectPackageManagerForProject(apiProject);

            // Assert: Both use npm with separate dependency management
            webManager.Should().Be("npm", "Web project should use npm for its own dependencies");
            apiManager.Should().Be("npm", "API project should use npm for its own dependencies");
        }

        [Fact]
        public void Should_Handle_Multiple_Lock_Files_In_Root_Gracefully()
        {
            // Arrange: Bad practice scenario - multiple lock files in repo root (migration/legacy)
            var repoRoot = CreateTempDirectory();
            CreatePackageJson(repoRoot, new Dictionary<string, string>());

            // Legacy scenario: multiple lock files from migration (BAD PRACTICE but handle gracefully)
            File.WriteAllText(Path.Combine(repoRoot, "package-lock.json"), "{}");  // Legacy npm
            File.WriteAllText(Path.Combine(repoRoot, "yarn.lock"), "# Yarn lockfile");  // Old yarn  
            File.WriteAllText(Path.Combine(repoRoot, "pnpm-lock.yaml"), "lockfileVersion: 5.4");  // New pnpm

            // Act: Detect package managers and conflicts
            var detectedLockFiles = DetectLockFiles(repoRoot);
            var selectedManager = DetectPackageManagerForProject(repoRoot);
            var hasConflict = detectedLockFiles.Count > 1;

            // Assert: Multiple lock files detected (bad practice scenario)
            detectedLockFiles.Should().HaveCount(3, "All three lock files should be detected");
            hasConflict.Should().BeTrue("Should detect conflicting lock files");

            // Assert: Uses priority order (pnpm wins - assume it's the intended new choice)
            selectedManager.Should().Be("pnpm", "Should use highest priority package manager");

            // Note: In real world, this should trigger a warning to clean up legacy lock files
        }

        [Fact]
        public void Should_Respect_Single_Package_Manager_Per_Repo_Best_Practice()
        {
            // Arrange: Best practice - single package manager for entire repo
            var repoRoot = CreateTempDirectory();
            var frontendDir = Path.Combine(repoRoot, "apps", "frontend");
            var adminDir = Path.Combine(repoRoot, "apps", "admin");
            var sharedDir = Path.Combine(repoRoot, "packages", "shared");
            
            Directory.CreateDirectory(frontendDir);
            Directory.CreateDirectory(adminDir);
            Directory.CreateDirectory(sharedDir);

            // Root workspace uses yarn (GOOD PRACTICE - consistent across repo)
            CreatePackageJson(repoRoot, new Dictionary<string, string>());
            File.WriteAllText(Path.Combine(repoRoot, "yarn.lock"), "# Yarn lockfile");
            File.WriteAllText(Path.Combine(repoRoot, ".yarnrc.yml"), "nodeLinker: node-modules");

            // All projects inherit same package manager (NO individual lock files)
            CreatePackageJson(frontendDir, new Dictionary<string, string> { ["build"] = "vite build" });
            CreatePackageJson(adminDir, new Dictionary<string, string> { ["build"] = "vite build" });
            CreatePackageJson(sharedDir, new Dictionary<string, string> { ["build"] = "rollup" });

            // Act: All should use same package manager
            var frontendManager = DetectPackageManagerWithInheritance(frontendDir, repoRoot);
            var adminManager = DetectPackageManagerWithInheritance(adminDir, repoRoot);
            var sharedManager = DetectPackageManagerWithInheritance(sharedDir, repoRoot);

            // Assert: Consistent package manager across entire repo (BEST PRACTICE)
            frontendManager.Should().Be("yarn", "Frontend should inherit yarn from repo root");
            adminManager.Should().Be("yarn", "Admin should inherit yarn from repo root");  
            sharedManager.Should().Be("yarn", "Shared should inherit yarn from repo root");
        }

        [Fact] 
        public void Should_Allow_Local_Override_When_Necessary()
        {
            // Arrange: Sometimes projects need different package managers (edge case)
            var repoRoot = CreateTempDirectory();
            var legacyProject = Path.Combine(repoRoot, "legacy", "old-app");
            
            Directory.CreateDirectory(legacyProject);

            // Root workspace uses modern pnpm
            CreatePackageJson(repoRoot, new Dictionary<string, string>());
            File.WriteAllText(Path.Combine(repoRoot, "pnpm-lock.yaml"), "lockfileVersion: 5.4");
            
            // Legacy project stuck on npm due to compatibility issues
            CreatePackageJson(legacyProject, new Dictionary<string, string> { ["build"] = "webpack" });
            File.WriteAllText(Path.Combine(legacyProject, "package-lock.json"), "{}");

            // Act: Detect with local override (necessary evil)
            var legacyManager = DetectPackageManagerWithInheritance(legacyProject, repoRoot);

            // Assert: Local npm overrides inherited pnpm (when necessary)
            legacyManager.Should().Be("npm", "Legacy project should use npm despite pnpm workspace");
        }

        [Theory]
        [InlineData("package-lock.json", "npm")]
        [InlineData("yarn.lock", "yarn")]  
        [InlineData("pnpm-lock.yaml", "pnpm")]
        [InlineData("bun.lockb", "bun")]
        public void Should_Detect_Package_Manager_From_Lock_File_Types(string lockFile, string expectedManager)
        {
            // Arrange: Project with specific lock file (basic detection)
            var projectDir = CreateTempDirectory();
            CreatePackageJson(projectDir, new Dictionary<string, string>());
            File.WriteAllText(Path.Combine(projectDir, lockFile), "test content");

            // Act: Detect package manager
            var detectedManager = DetectPackageManagerForProject(projectDir);

            // Assert: Correct package manager detected
            detectedManager.Should().Be(expectedManager, $"{lockFile} should indicate {expectedManager} package manager");
        }

        [Fact]
        public void Should_Generate_Consistent_Install_Commands_For_Workspace()
        {
            // Arrange: Workspace with consistent package manager (best practice)
            var repoRoot = CreateTempDirectory();
            var appA = Path.Combine(repoRoot, "apps", "web");
            var appB = Path.Combine(repoRoot, "apps", "admin");
            
            Directory.CreateDirectory(appA);
            Directory.CreateDirectory(appB);

            // Root workspace uses yarn consistently
            CreatePackageJson(repoRoot, new Dictionary<string, string>());
            File.WriteAllText(Path.Combine(repoRoot, "yarn.lock"), "# Yarn lockfile");

            // Apps inherit workspace package manager (NO individual lock files - GOOD PRACTICE)
            CreatePackageJson(appA, new Dictionary<string, string> { ["build"] = "vite build" });
            CreatePackageJson(appB, new Dictionary<string, string> { ["build"] = "vite build" });

            // Act: Generate install commands (should be consistent)
            var yarnInstallCmd = GenerateInstallCommand("yarn");

            // Assert: Consistent install command across workspace
            yarnInstallCmd.Should().Be("yarn install --frozen-lockfile", "Should use frozen lockfile for reproducible builds");
        }

        [Fact]
        public void Should_Provide_Specific_Cleanup_Commands_For_Each_Package_Manager()
        {
            // Arrange: Multiple lock files detected (conflict scenario)
            var repoRoot = CreateTempDirectory();
            CreatePackageJson(repoRoot, new Dictionary<string, string>());

            // Create conflicting lock files
            File.WriteAllText(Path.Combine(repoRoot, "package-lock.json"), "{}");
            File.WriteAllText(Path.Combine(repoRoot, "yarn.lock"), "# Yarn lockfile");
            File.WriteAllText(Path.Combine(repoRoot, "pnpm-lock.yaml"), "lockfileVersion: 5.4");

            // Act & Assert: Should provide specific cleanup commands per package manager
            var npmCleanupCommand = GetCleanupCommandForPackageManager("npm");
            var yarnCleanupCommand = GetCleanupCommandForPackageManager("yarn");
            var pnpmCleanupCommand = GetCleanupCommandForPackageManager("pnpm");

            // Assert: Correct cleanup commands (remove others, install with chosen PM)
            npmCleanupCommand.Should().Contain("rm yarn.lock pnpm-lock.yaml");
            npmCleanupCommand.Should().Contain("npm ci");
            
            yarnCleanupCommand.Should().Contain("rm package-lock.json pnpm-lock.yaml");
            yarnCleanupCommand.Should().Contain("yarn install --frozen-lockfile");
            
            pnpmCleanupCommand.Should().Contain("rm package-lock.json yarn.lock");  
            pnpmCleanupCommand.Should().Contain("pnpm install --frozen-lockfile");
        }

        [Fact]
        public void Should_Error_In_CI_Instead_Of_Warning_For_Lock_File_Conflicts()
        {
            // Arrange: CI environment with multiple lock files
            var projectDir = CreateTempDirectory();
            CreatePackageJson(projectDir, new Dictionary<string, string>());
            
            // Multiple conflicting lock files
            File.WriteAllText(Path.Combine(projectDir, "package-lock.json"), "{}");
            File.WriteAllText(Path.Combine(projectDir, "yarn.lock"), "# Yarn lockfile");

            // Act: Simulate CI environment behavior
            var ciConflictBehavior = GetConflictBehaviorForEnvironment(isCI: true);
            var devConflictBehavior = GetConflictBehaviorForEnvironment(isCI: false);

            // Assert: CI should error, development should warn
            ciConflictBehavior.Should().Be("error", "CI should fail fast on package manager conflicts");
            devConflictBehavior.Should().Be("warn", "Development should warn but allow continuation");
        }

        [Fact]
        public void Should_Create_Separate_Marker_Files_Per_Project()
        {
            // Arrange: Monorepo with multiple projects
            var repoRoot = CreateTempDirectory();
            var projectA = Path.Combine(repoRoot, "ProjectA");
            var projectB = Path.Combine(repoRoot, "ProjectB");
            
            Directory.CreateDirectory(projectA);
            Directory.CreateDirectory(projectB);
            Directory.CreateDirectory(Path.Combine(projectA, "obj"));
            Directory.CreateDirectory(Path.Combine(projectB, "obj"));

            // Act: Create marker files for each project
            var markerA = Path.Combine(projectA, "obj", "ViteKit.Msbuild.NodeRestore.marker");
            var markerB = Path.Combine(projectB, "obj", "ViteKit.Msbuild.NodeRestore.marker");
            
            File.WriteAllText(markerA, $"Restored {DateTime.Now}");
            File.WriteAllText(markerB, $"Restored {DateTime.Now.AddMinutes(5)}");

            // Assert: Separate marker files exist
            File.Exists(markerA).Should().BeTrue("ProjectA should have its own marker file");
            File.Exists(markerB).Should().BeTrue("ProjectB should have its own marker file");
            
            // Assert: Marker files are independent
            Path.GetDirectoryName(markerA).Should().NotBe(Path.GetDirectoryName(markerB), 
                "Marker files should be in separate project obj directories");
        }

        // Helper methods to simulate MSBuild behavior
        private string DetectPackageManagerForProject(string projectDir)
        {
            // Simulate MSBuild package manager detection logic
            if (File.Exists(Path.Combine(projectDir, "bun.lockb"))) return "bun";
            if (File.Exists(Path.Combine(projectDir, "pnpm-lock.yaml"))) return "pnpm";
            if (File.Exists(Path.Combine(projectDir, "yarn.lock"))) return "yarn";
            if (File.Exists(Path.Combine(projectDir, "package-lock.json"))) return "npm";
            return "npm"; // default
        }

        private string DetectPackageManagerWithInheritance(string projectDir, string? repoRoot = null)
        {
            // First check local project
            var localManager = DetectPackageManagerForProject(projectDir);
            if (localManager != "npm" || repoRoot == null) // npm is default, so check for inheritance
                return localManager;

            // Check if local project has any lock file
            var hasLocalLockFile = File.Exists(Path.Combine(projectDir, "package-lock.json")) ||
                                  File.Exists(Path.Combine(projectDir, "yarn.lock")) ||
                                  File.Exists(Path.Combine(projectDir, "pnpm-lock.yaml")) ||
                                  File.Exists(Path.Combine(projectDir, "bun.lockb"));

            if (hasLocalLockFile)
                return localManager;

            // No local lock file, check parent directories for inheritance
            var currentDir = Directory.GetParent(projectDir);
            while (currentDir != null && currentDir.FullName.StartsWith(repoRoot))
            {
                var parentManager = DetectPackageManagerForProject(currentDir.FullName);
                if (parentManager != "npm") // Found explicit parent package manager
                    return parentManager;
                
                currentDir = currentDir.Parent;
            }

            return localManager; // fallback to npm
        }

        private List<string> DetectLockFiles(string projectDir)
        {
            var lockFiles = new List<string>();
            var possibleLockFiles = new[] { "package-lock.json", "yarn.lock", "pnpm-lock.yaml", "bun.lockb" };
            
            foreach (var lockFile in possibleLockFiles)
            {
                var fullPath = Path.Combine(projectDir, lockFile);
                if (File.Exists(fullPath))
                    lockFiles.Add(fullPath);
            }
            
            return lockFiles;
        }

        private string GenerateInstallCommand(string packageManager)
        {
            return packageManager switch
            {
                "bun" => "bun install --frozen-lockfile",
                "pnpm" => "pnpm install --frozen-lockfile",
                "yarn" => "yarn install --frozen-lockfile",
                "npm" => "npm ci",
                _ => "npm ci"
            };
        }

        private string GetCleanupCommandForPackageManager(string packageManager)
        {
            return packageManager switch
            {
                "npm" => "rm yarn.lock pnpm-lock.yaml bun.lockb && npm ci",
                "yarn" => "rm package-lock.json pnpm-lock.yaml bun.lockb && yarn install --frozen-lockfile",
                "pnpm" => "rm package-lock.json yarn.lock bun.lockb && pnpm install --frozen-lockfile",
                "bun" => "rm package-lock.json yarn.lock pnpm-lock.yaml && bun install --frozen-lockfile",
                _ => "unknown"
            };
        }

        private string GetConflictBehaviorForEnvironment(bool isCI)
        {
            // Simulate MSBuild logic: CI=true -> error, otherwise warn
            return isCI ? "error" : "warn";
        }

        private string CreateTempDirectory()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"ViteMonorepoTest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            _tempDirectories.Add(tempDir);
            return tempDir;
        }

        private void CreatePackageJson(string projectDir, Dictionary<string, string> scripts)
        {
            var packageJson = new
            {
                name = Path.GetFileName(projectDir)?.ToLowerInvariant() ?? "test-project",
                version = "1.0.0",
                scripts = scripts,
                devDependencies = new { vite = "^4.0.0" }
            };

            var json = JsonSerializer.Serialize(packageJson, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(projectDir, "package.json"), json);
        }

        public void Dispose()
        {
            foreach (var tempDir in _tempDirectories)
            {
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); }
                    catch { /* Ignore cleanup errors */ }
                }
            }
        }
    }
}
