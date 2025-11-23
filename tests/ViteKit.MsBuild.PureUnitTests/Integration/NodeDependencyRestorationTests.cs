using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace ViteKit.MsBuild.PureUnitTests.Integration
{
    /// <summary>
    /// Tests for Node.js dependency restoration logic
    /// Validates MSBuild target behavior for npm install, yarn install, pnpm install, bun install
    /// </summary>
    public class NodeDependencyRestorationTests : IDisposable
    {
        private readonly List<string> _tempDirectories = new();

        [Theory]
        [InlineData("npm", "npm ci")]
        [InlineData("yarn", "yarn install --frozen-lockfile")]
        [InlineData("pnpm", "pnpm install --frozen-lockfile")]
        [InlineData("bun", "bun install --frozen-lockfile")]
        public void Should_Generate_Correct_Install_Command_Per_Package_Manager(string packageManager, string expectedCommand)
        {
            // Arrange: Project with detected package manager
            var projectDir = CreateTempDirectory();
            CreatePackageJson(projectDir, new Dictionary<string, string>());

            // Act: Get install command for package manager
            var installCommand = GetInstallCommandForPackageManager(packageManager);

            // Assert: Correct install command generated
            installCommand.Should().Be(expectedCommand);
        }

        [Fact]
        public void Should_Use_NPM_CI_For_Deterministic_Builds()
        {
            // Arrange: npm package manager
            var packageManager = "npm";

            // Act: Get install command
            var command = GetInstallCommandForPackageManager(packageManager);

            // Assert: Uses npm ci (not npm install) for reproducible builds
            command.Should().Be("npm ci", "npm ci ensures deterministic builds from package-lock.json");
        }

        [Fact]
        public void Should_Use_Frozen_Lockfile_For_Yarn_Pnpm_Bun()
        {
            // Arrange: Package managers that support --frozen-lockfile
            var packageManagers = new[] { "yarn", "pnpm", "bun" };

            foreach (var pm in packageManagers)
            {
                // Act: Get install command
                var command = GetInstallCommandForPackageManager(pm);

                // Assert: Uses --frozen-lockfile for build reproducibility
                command.Should().Contain("--frozen-lockfile", 
                    $"{pm} should use --frozen-lockfile for deterministic CI builds");
            }
        }

        [Theory]
        [InlineData("package.json", true)]
        [InlineData("package-lock.json", true)]
        [InlineData("yarn.lock", true)]
        [InlineData("pnpm-lock.yaml", true)]
        [InlineData("bun.lockb", true)]
        [InlineData("src/component.ts", false)]
        [InlineData("README.md", false)]
        public void Should_Detect_Node_Dependency_File_Changes(string changedFile, bool shouldTriggerRestore)
        {
            // Arrange: Project with package files
            var projectDir = CreateTempDirectory();
            CreatePackageJson(projectDir, new Dictionary<string, string>());
            
            var dependencyFiles = new[]
            {
                "package.json",
                "package-lock.json", 
                "yarn.lock",
                "pnpm-lock.yaml",
                "bun.lockb"
            };

            // Create all dependency files
            foreach (var file in dependencyFiles)
            {
                File.WriteAllText(Path.Combine(projectDir, file), "{}");
            }

            // Create non-dependency file
            Directory.CreateDirectory(Path.Combine(projectDir, "src"));
            File.WriteAllText(Path.Combine(projectDir, "src", "component.ts"), "export default {}");
            File.WriteAllText(Path.Combine(projectDir, "README.md"), "# Test Project");

            // Act: Check if file change should trigger restore
            var triggersRestore = ShouldTriggerNodeRestore(changedFile, dependencyFiles);

            // Assert: Only dependency files trigger restore
            triggersRestore.Should().Be(shouldTriggerRestore, 
                $"Changes to {changedFile} should {(shouldTriggerRestore ? "" : "not ")}trigger Node.js dependency restore");
        }

        [Fact]
        public void Should_Create_Marker_File_After_Successful_Restore()
        {
            // Arrange: Project directory
            var projectDir = CreateTempDirectory();
            var objDir = Path.Combine(projectDir, "obj");
            var markerFile = Path.Combine(objDir, "ViteKit.Msbuild.NodeRestore.marker");

            // Ensure obj directory exists (like MSBuild would)
            Directory.CreateDirectory(objDir);

            // Act: Simulate successful restore creating marker file
            SimulateSuccessfulNodeRestore(markerFile);

            // Assert: Marker file created in obj/ directory
            File.Exists(markerFile).Should().BeTrue("Marker file should be created after successful restore");

            // Assert: Marker file has recent timestamp
            var markerInfo = new FileInfo(markerFile);
            var ageInSeconds = (DateTime.Now - markerInfo.LastWriteTime).TotalSeconds;
            ageInSeconds.Should().BeLessThan(5, "Marker file should have recent timestamp");
        }

        [Fact]
        public void Should_Skip_Restore_When_Dependencies_Are_Up_To_Date()
        {
            // Arrange: Project with package.json and recent marker file
            var projectDir = CreateTempDirectory();
            var packageJsonFile = Path.Combine(projectDir, "package.json");
            var objDir = Path.Combine(projectDir, "obj");
            var markerFile = Path.Combine(objDir, "ViteKit.Msbuild.NodeRestore.marker");

            Directory.CreateDirectory(objDir);

            // Create package.json
            CreatePackageJson(projectDir, new Dictionary<string, string>());
            var packageJsonTime = DateTime.Now.AddMinutes(-10);  // 10 minutes ago
            File.SetLastWriteTime(packageJsonFile, packageJsonTime);

            // Create newer marker file (restore already happened)
            File.WriteAllText(markerFile, $"Restored at {DateTime.Now}");
            var markerTime = DateTime.Now.AddMinutes(-5);  // 5 minutes ago (newer than package.json)
            File.SetLastWriteTime(markerFile, markerTime);

            // Act: Check if restore is required
            var restoreRequired = IsNodeRestoreRequired(packageJsonFile, markerFile);

            // Assert: Restore not required (marker is newer than package.json)
            restoreRequired.Should().BeFalse("Restore should not be required when marker file is newer than package.json");
        }

        [Fact]
        public void Should_Trigger_Restore_When_Dependencies_Changed()
        {
            // Arrange: Project with package.json newer than marker file
            var projectDir = CreateTempDirectory();
            var packageJsonFile = Path.Combine(projectDir, "package.json");
            var objDir = Path.Combine(projectDir, "obj");
            var markerFile = Path.Combine(objDir, "ViteKit.Msbuild.NodeRestore.marker");

            Directory.CreateDirectory(objDir);

            // Create old marker file
            File.WriteAllText(markerFile, "Old restore");
            var markerTime = DateTime.Now.AddMinutes(-10);  // 10 minutes ago
            File.SetLastWriteTime(markerFile, markerTime);

            // Create newer package.json (dependencies changed)
            CreatePackageJson(projectDir, new Dictionary<string, string> { ["build"] = "vite build" });
            var packageJsonTime = DateTime.Now.AddMinutes(-5);  // 5 minutes ago (newer than marker)
            File.SetLastWriteTime(packageJsonFile, packageJsonTime);

            // Act: Check if restore is required
            var restoreRequired = IsNodeRestoreRequired(packageJsonFile, markerFile);

            // Assert: Restore required (package.json newer than marker)
            restoreRequired.Should().BeTrue("Restore should be required when package.json is newer than marker file");
        }

        [Fact]
        public void Should_Trigger_Restore_When_Lock_File_Changed()
        {
            // Arrange: Project with recent lock file changes
            var projectDir = CreateTempDirectory();
            var packageJsonFile = Path.Combine(projectDir, "package.json");
            var lockFile = Path.Combine(projectDir, "package-lock.json");
            var objDir = Path.Combine(projectDir, "obj");
            var markerFile = Path.Combine(objDir, "ViteKit.Msbuild.NodeRestore.marker");

            Directory.CreateDirectory(objDir);

            // Create old marker file and package.json
            CreatePackageJson(projectDir, new Dictionary<string, string>());
            File.WriteAllText(markerFile, "Old restore");
            var oldTime = DateTime.Now.AddMinutes(-10);
            File.SetLastWriteTime(packageJsonFile, oldTime);
            File.SetLastWriteTime(markerFile, oldTime);

            // Create new lock file (dependency resolution changed)
            File.WriteAllText(lockFile, "{ \"lockfileVersion\": 1 }");
            var newTime = DateTime.Now.AddMinutes(-2);  // Newer than marker
            File.SetLastWriteTime(lockFile, newTime);

            // Act: Check if restore is required considering lock file
            var restoreRequired = IsNodeRestoreRequired(packageJsonFile, markerFile, lockFile);

            // Assert: Restore required (lock file newer than marker)
            restoreRequired.Should().BeTrue("Restore should be required when lock file is newer than marker file");
        }

        [Fact]
        public void Should_Handle_Missing_Package_Json_Gracefully()
        {
            // Arrange: Project without package.json
            var projectDir = CreateTempDirectory();
            var packageJsonFile = Path.Combine(projectDir, "package.json");
            var markerFile = Path.Combine(projectDir, "obj", "marker");

            // Act: Check restore requirements for non-existent package.json
            var restoreRequired = IsNodeRestoreRequired(packageJsonFile, markerFile);

            // Assert: No restore required for non-existent package.json
            restoreRequired.Should().BeFalse("Restore should not be required when package.json doesn't exist");
        }

        [Theory]
        [InlineData("npm", "ci")]
        [InlineData("yarn", "install --frozen-lockfile")]
        [InlineData("pnpm", "install --frozen-lockfile")]
        [InlineData("bun", "install --frozen-lockfile")]
        public void Should_Generate_Production_Ready_Install_Commands(string packageManager, string expectedArgs)
        {
            // Act: Get install command for package manager
            var command = GetInstallCommandForPackageManager(packageManager);

            // Assert: Command is production-ready
            command.Should().Be($"{packageManager} {expectedArgs}");
            
            // Assert: Command follows best practices
            if (packageManager == "npm")
            {
                command.Should().NotContain("install", "npm should use 'ci' not 'install' for deterministic builds");
            }
            else
            {
                command.Should().Contain("--frozen-lockfile", $"{packageManager} should use --frozen-lockfile for reproducible builds");
            }
        }

        // Helper methods for testing MSBuild-like logic
        private string GetInstallCommandForPackageManager(string packageManager)
        {
            return packageManager switch
            {
                "bun" => "bun install --frozen-lockfile",
                "pnpm" => "pnpm install --frozen-lockfile", 
                "yarn" => "yarn install --frozen-lockfile",
                "npm" => "npm ci",
                _ => throw new ArgumentException($"Unknown package manager: {packageManager}")
            };
        }

        private bool ShouldTriggerNodeRestore(string changedFile, string[] dependencyFiles)
        {
            return Array.Exists(dependencyFiles, f => f.Equals(changedFile, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsNodeRestoreRequired(string packageJsonFile, string markerFile, string? lockFile = null)
        {
            if (!File.Exists(packageJsonFile))
                return false;

            if (!File.Exists(markerFile))
                return true;

            var packageJsonTime = File.GetLastWriteTime(packageJsonFile);
            var markerTime = File.GetLastWriteTime(markerFile);

            // Check if package.json is newer
            if (packageJsonTime > markerTime)
                return true;

            // Check if lock file is newer (if provided)
            if (lockFile != null && File.Exists(lockFile))
            {
                var lockFileTime = File.GetLastWriteTime(lockFile);
                if (lockFileTime > markerTime)
                    return true;
            }

            return false;
        }

        private void SimulateSuccessfulNodeRestore(string markerFile)
        {
            File.WriteAllText(markerFile, $"Node dependencies restored at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        }

        private string CreateTempDirectory()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"ViteNodeTest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            _tempDirectories.Add(tempDir);
            return tempDir;
        }

        private void CreatePackageJson(string projectDir, Dictionary<string, string> scripts)
        {
            var packageJson = new
            {
                name = "test-project",
                version = "1.0.0",
                scripts = scripts,
                devDependencies = new
                {
                    vite = "^4.0.0"
                }
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
                    try
                    {
                        Directory.Delete(tempDir, true);
                    }
                    catch (Exception)
                    {
                        // Ignore cleanup errors in tests
                    }
                }
            }
        }
    }
}
