using System;
using System.IO;
using System.Text.Json;
using ViteKit.MsBuild.Tasks;
using ViteKit.MsBuild.PureUnitTests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace ViteKit.MsBuild.PureUnitTests.Tasks
{
    /// <summary>
    /// Comprehensive unit tests for DetectPackageManagerTask
    /// Tests lock file detection, conflict resolution, package.json parsing
    /// </summary>
    public class DetectPackageManagerTaskTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly string _tempDir;

        public DetectPackageManagerTaskTests(ITestOutputHelper output)
        {
            _output = output;
            _tempDir = Path.Combine(Path.GetTempPath(), "ViteTest_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }

        private DetectPackageManagerTask CreateTask()
        {
            return new DetectPackageManagerTask
            {
                BuildEngine = new MockBuildEngine()
            };
        }

        [Fact]
        public void DetectPackageManager_NoLockFiles_DefaultsToNpm()
        {
            // Arrange
            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.ConflictAction = "warn";

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result, "Task should succeed");
            Assert.Equal("npm", task.PackageManager);
            Assert.False(task.HasConflicts);
            Assert.Empty(task.ConflictingFiles);
            Assert.Equal("npm ci", task.InstallCommand);
        }

        [Theory]
        [InlineData("bun.lockb", "bun", "bun install --frozen-lockfile")]
        [InlineData("pnpm-lock.yaml", "pnpm", "pnpm install --frozen-lockfile")]
        [InlineData("yarn.lock", "yarn", "yarn install --frozen-lockfile")]
        [InlineData("package-lock.json", "npm", "npm ci")]
        public void DetectPackageManager_SingleLockFile_DetectsCorrectly(string lockFile, string expectedManager, string expectedInstall)
        {
            // Arrange
            var lockPath = Path.Combine(_tempDir, lockFile);
            File.WriteAllText(lockPath, "# lock file content");

            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.ConflictAction = "warn";

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result, "Task should succeed");
            Assert.Equal(expectedManager, task.PackageManager);
            Assert.False(task.HasConflicts);
            Assert.Equal(expectedInstall, task.InstallCommand);
        }

        [Fact]
        public void DetectPackageManager_MultipleLockFiles_DetectsConflict()
        {
            // Arrange
            File.WriteAllText(Path.Combine(_tempDir, "package-lock.json"), "{}");
            File.WriteAllText(Path.Combine(_tempDir, "yarn.lock"), "# yarn");

            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.ConflictAction = "warn"
            ;

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result, "Task should succeed with warning");
            Assert.True(task.HasConflicts);
            Assert.Equal(2, task.ConflictingFiles.Length);
            Assert.Contains("package-lock.json", task.ConflictingFiles[0].ItemSpec);
            Assert.Contains("yarn.lock", task.ConflictingFiles[1].ItemSpec);
            Assert.NotEmpty(task.CleanupCommand);
        }

        [Fact]
        public void DetectPackageManager_ConflictActionError_FailsBuild()
        {
            // Arrange
            File.WriteAllText(Path.Combine(_tempDir, "package-lock.json"), "{}");
            File.WriteAllText(Path.Combine(_tempDir, "pnpm-lock.yaml"), "lockfileVersion: 5.4");

            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.ConflictAction = "error"
            ;

            // Act
            var result = task.Execute();

            // Assert
            Assert.False(result, "Task should fail with error action");
            Assert.True(task.HasConflicts);
        }

        [Fact]
        public void DetectPackageManager_PriorityOrder_ChoosesBunOverOthers()
        {
            // Arrange - Create all lock files
            File.WriteAllText(Path.Combine(_tempDir, "package-lock.json"), "{}");
            File.WriteAllText(Path.Combine(_tempDir, "yarn.lock"), "# yarn");
            File.WriteAllText(Path.Combine(_tempDir, "pnpm-lock.yaml"), "lockfileVersion: 5.4");
            File.WriteAllText(Path.Combine(_tempDir, "bun.lockb"), "binary content");

            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.ConflictAction = "warn"
            ;

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result, "Task should succeed");
            Assert.Equal("bun", task.PackageManager); // Should choose bun due to priority order
            Assert.True(task.HasConflicts);
            Assert.Contains("rm package-lock.json", task.CleanupCommand);
        }

        [Theory]
        [InlineData("npm@8.19.2", "npm")]
        [InlineData("yarn@3.2.0", "yarn")]
        [InlineData("pnpm@8.10.0", "pnpm")]
        [InlineData("bun@1.0.0", "bun")]
        [InlineData("invalid@1.0.0", null)]
        public void DetectPackageManager_PackageJsonField_RespectsSpecification(string packageManagerValue, string? expectedManager)
        {
            // Arrange
            var packageJson = new
            {
                name = "test-project",
                packageManager = packageManagerValue
            };

            var packageJsonPath = Path.Combine(_tempDir, "package.json");
            File.WriteAllText(packageJsonPath, JsonSerializer.Serialize(packageJson, new JsonSerializerOptions { WriteIndented = true }));

            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.ConflictAction = "warn"
            ;

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result, "Task should succeed");
            Assert.Equal(expectedManager ?? "npm", task.PackageManager);
        }

        [Fact]
        public void DetectPackageManager_MalformedPackageJson_HandlesGracefully()
        {
            // Arrange
            var packageJsonPath = Path.Combine(_tempDir, "package.json");
            File.WriteAllText(packageJsonPath, "{ invalid json }");

            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.ConflictAction = "warn"
            ;

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result, "Task should succeed despite malformed JSON");
            Assert.Equal("npm", task.PackageManager); // Should fall back to npm default
        }

        [Fact]
        public void DetectPackageManager_NonexistentDirectory_Fails()
        {
            // Arrange
            var nonexistentDir = Path.Combine(_tempDir, "nonexistent");
            var task = CreateTask();
            task.ViteProjectRoot = nonexistentDir;
            task.ConflictAction = "warn"
            ;

            // Act
            var result = task.Execute();

            // Assert
            Assert.False(result, "Task should fail for nonexistent directory");
        }

        [Fact]
        public void DetectPackageManager_CleanupCommand_GeneratesCorrectSyntax()
        {
            // Arrange
            File.WriteAllText(Path.Combine(_tempDir, "package-lock.json"), "{}");
            File.WriteAllText(Path.Combine(_tempDir, "yarn.lock"), "# yarn");
            File.WriteAllText(Path.Combine(_tempDir, "pnpm-lock.yaml"), "lockfileVersion: 5.4");

            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.ConflictAction = "warn"
            ;

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result);
            Assert.Equal("pnpm", task.PackageManager); // pnpm wins priority
            Assert.Contains("rm package-lock.json yarn.lock", task.CleanupCommand);
            Assert.Contains("pnpm install --frozen-lockfile", task.CleanupCommand);
        }

        [Fact]
        public void DetectPackageManager_EmptyDirectory_ReturnsNpmDefault()
        {
            // Arrange
            var emptyDir = Path.Combine(_tempDir, "empty");
            Directory.CreateDirectory(emptyDir);

            var task = CreateTask();
            task.ViteProjectRoot = emptyDir;
            task.ConflictAction = "warn"
            ;

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result);
            Assert.Equal("npm", task.PackageManager);
            Assert.False(task.HasConflicts);
            Assert.Equal("npm ci", task.InstallCommand);
        }
    }
}
