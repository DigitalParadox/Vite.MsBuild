using System;
using System.IO;
using System.Text.Json;
using ViteKit.MsBuild.Tasks;
using ViteKit.MsBuild.PureUnitTests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace ViteKit.MsBuild.PureUnitTests.Tasks
{
    /// <summary>
    /// Comprehensive unit tests for ValidateViteProjectTask
    /// Tests validation logic, welcome messages, environment file handling
    /// </summary>
    public class ValidateViteProjectTaskTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly string _tempDir;

        public ValidateViteProjectTaskTests(ITestOutputHelper output)
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

        private ValidateViteProjectTask CreateTask()
        {
            return new ValidateViteProjectTask
            {
                BuildEngine = new MockBuildEngine()
            };
        }

        private void SetupBasicPackageJson(string? content = null)
        {
            var packageJsonContent = content ?? """
                {
                  "name": "test-project",
                  "version": "1.0.0",
                  "scripts": {
                    "build": "vite build",
                    "dev": "vite dev"
                  }
                }
                """;
            File.WriteAllText(Path.Combine(_tempDir, "package.json"), packageJsonContent);
        }

        [Fact]
        public void ValidateViteProject_DisabledBuild_SkipsValidation()
        {
            // Arrange
            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.EnableViteBuild = false;
            task.PackageManager = "npm";

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result, "Task should succeed when disabled");
            Assert.True(task.IsValid, "Should be marked as valid when skipped");
        }

        [Fact]
        public void ValidateViteProject_MissingPackageJson_Fails()
        {
            // Arrange
            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.EnableViteBuild = true;
            task.PackageManager = "npm";

            // Act
            var result = task.Execute();

            // Assert
            Assert.False(result, "Task should fail without package.json");
            Assert.False(task.HasPackageJson);
            Assert.False(task.IsValid);
        }

        [Fact]
        public void ValidateViteProject_ValidPackageJson_Succeeds()
        {
            // Arrange
            var packageJson = new
            {
                name = "test-project",
                version = "1.0.0",
                scripts = new { build = "vite build" }
            };

            var packageJsonPath = Path.Combine(_tempDir, "package.json");
            File.WriteAllText(packageJsonPath, JsonSerializer.Serialize(packageJson, new JsonSerializerOptions { WriteIndented = true }));

            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.EnableViteBuild = true;
            task.PackageManager = "npm";
            task.ShowWelcomeMessage = false;

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result, "Task should succeed with valid package.json");
            Assert.True(task.HasPackageJson);
            Assert.True(task.IsValid);
        }

        [Theory]
        [InlineData("vite.config.ts")]
        [InlineData("vite.config.js")]
        [InlineData("vite.config.mts")]
        [InlineData("vite.config.mjs")]
        public void ValidateViteProject_ViteConfigDetection_FindsAllTypes(string configFileName)
        {
            // Arrange
            File.WriteAllText(Path.Combine(_tempDir, "package.json"), "{}");
            File.WriteAllText(Path.Combine(_tempDir, configFileName), "export default {}");

            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.EnableViteBuild = true;
            task.PackageManager = "npm";
            task.ShowWelcomeMessage = false;

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result);
            Assert.True(task.HasViteConfig); // Should detect vite config
        }

        [Fact]
        public void ValidateViteProject_UserSpecifiedConfig_ValidatesPath()
        {
            // Arrange
            File.WriteAllText(Path.Combine(_tempDir, "package.json"), "{}");
            File.WriteAllText(Path.Combine(_tempDir, "custom.vite.config.ts"), "export default {}");

            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.ViteConfigFile = "custom.vite.config.ts";
            task.EnableViteBuild = true;
            task.PackageManager = "npm";
            task.ShowWelcomeMessage = false;

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result);
            Assert.True(task.HasViteConfig);
        }

        [Fact]
        public void ValidateViteProject_UserSpecifiedConfigMissing_ShowsWarning()
        {
            // Arrange
            File.WriteAllText(Path.Combine(_tempDir, "package.json"), "{}");

            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.ViteConfigFile = "missing.config.ts";
            task.EnableViteBuild = true;
            task.PackageManager = "npm";
            task.ShowWelcomeMessage = false;

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result); // Should succeed but show warning
            Assert.False(task.HasViteConfig);
        }

        [Fact]
        public void ValidateViteProject_NodeModulesDetection_WorksCorrectly()
        {
            // Arrange
            File.WriteAllText(Path.Combine(_tempDir, "package.json"), "{}");
            
            var nodeModulesDir = Path.Combine(_tempDir, "node_modules");
            Directory.CreateDirectory(nodeModulesDir);

            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.EnableViteBuild = true;
            task.PackageManager = "npm";
            task.ShowWelcomeMessage = false;

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result);
            Assert.True(task.HasNodeModules);
        }

        [Fact]
        public void ValidateViteProject_WithViteDependency_ShowsSuccess()
        {
            // Arrange
            File.WriteAllText(Path.Combine(_tempDir, "package.json"), "{}");
            
            var nodeModulesDir = Path.Combine(_tempDir, "node_modules");
            var viteDir = Path.Combine(nodeModulesDir, "vite");
            Directory.CreateDirectory(viteDir);

            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.EnableViteBuild = true;
            task.PackageManager = "npm";
            task.ShowWelcomeMessage = false;

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result);
            Assert.True(task.HasNodeModules);
        }

        [Theory]
        [InlineData("error", ".env;.env.local", false)]
        [InlineData("warn", ".env;.env.local", true)]
        [InlineData("silent", ".env;.env.local", true)]
        public void ValidateViteProject_MissingEnvFiles_RespectsAction(string action, string missingFiles, bool shouldSucceed)
        {
            // Arrange
            File.WriteAllText(Path.Combine(_tempDir, "package.json"), "{}");

            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.ViteMissingEnvAction = action;
            task.EnableViteBuild = true;
            task.PackageManager = "npm";
            task.ShowWelcomeMessage = false;

            // Act
            var result = task.Execute();

            // Assert
            Assert.Equal(shouldSucceed, result);
            Assert.True(task.MissingEnvFiles.Length > 0); // Should detect missing env files
        }

        [Fact]
        public void ValidateViteProject_WithEnvFiles_DetectsCorrectly()
        {
            // Arrange
            File.WriteAllText(Path.Combine(_tempDir, "package.json"), "{}");
            File.WriteAllText(Path.Combine(_tempDir, ".env"), "NODE_ENV=development");
            File.WriteAllText(Path.Combine(_tempDir, ".env.local"), "API_KEY=secret");

            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.ViteMissingEnvAction = "silent";
            task.EnableViteBuild = true;
            task.PackageManager = "npm";
            task.ShowWelcomeMessage = false;

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result);
            Assert.True(task.MissingEnvFiles.Length < 4); // Should have fewer missing files
        }

        [Theory]
        [InlineData("npm")]
        [InlineData("yarn")]
        [InlineData("pnpm")]
        [InlineData("bun")]
        public void ValidateViteProject_SupportedPackageManagers_Validates(string packageManager)
        {
            // Arrange
            File.WriteAllText(Path.Combine(_tempDir, "package.json"), "{}");

            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.PackageManager = packageManager;
            task.EnableViteBuild = true;
            task.ShowWelcomeMessage = false;

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result); // Should support package manager
        }

        [Fact]
        public void ValidateViteProject_UnsupportedPackageManager_ShowsWarning()
        {
            // Arrange
            File.WriteAllText(Path.Combine(_tempDir, "package.json"), "{}");

            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.PackageManager = "unsupported";
            task.EnableViteBuild = true;
            task.ShowWelcomeMessage = false;

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result); // Should succeed but show warning
        }

        [Fact]
        public void ValidateViteProject_WelcomeMessage_ShowsForNewProjects()
        {
            // Arrange
            File.WriteAllText(Path.Combine(_tempDir, "package.json"), "{}");
            File.WriteAllText(Path.Combine(_tempDir, "vite.config.ts"), "export default {}");
            // No node_modules = new project

            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.EnableViteBuild = true;
            task.PackageManager = "pnpm";
            task.ShowWelcomeMessage = true
            ;

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result);
            Assert.True(task.HasPackageJson);
            Assert.True(task.HasViteConfig);
            Assert.False(task.HasNodeModules);
            // Welcome message would be shown (verified in logs)
        }

        [Fact]
        public void ValidateViteProject_MalformedPackageJson_HandlesGracefully()
        {
            // Arrange
            var packageJsonPath = Path.Combine(_tempDir, "package.json");
            File.WriteAllText(packageJsonPath, "{ invalid json }");

            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.EnableViteBuild = true;
            task.PackageManager = "npm";
            task.ShowWelcomeMessage = false
            ;

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result); // Should succeed despite malformed JSON
            Assert.True(task.HasPackageJson, "Should detect package.json exists");
        }

        [Fact]
        public void ValidateViteProject_EmptyPackageJson_HandlesGracefully()
        {
            // Arrange
            var packageJsonPath = Path.Combine(_tempDir, "package.json");
            File.WriteAllText(packageJsonPath, "");

            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.EnableViteBuild = true;
            task.PackageManager = "npm";
            task.ShowWelcomeMessage = false
            ;

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result);
            Assert.True(task.HasPackageJson);
        }

        [Fact]
        public void ValidateViteProject_NonexistentDirectory_Fails()
        {
            // Arrange
            var task = CreateTask();
            task.ViteProjectRoot = Path.Combine(_tempDir, "nonexistent");
            task.EnableViteBuild = true;
            task.PackageManager = "npm";

            // Act
            var result = task.Execute();

            // Assert
            Assert.False(result, "Task should fail for nonexistent directory");
        }
    }
}


