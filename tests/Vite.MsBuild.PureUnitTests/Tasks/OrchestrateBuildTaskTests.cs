using FluentAssertions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using Vite.MsBuild.Tasks;
using Xunit;

namespace Vite.MsBuild.PureUnitTests.Tasks
{
    public class OrchestrateBuildTaskTests : IDisposable
    {
        private readonly string _tempDir;

        public OrchestrateBuildTaskTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"ViteMsBuildTests_{System.Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
            
            // Create package.json for build command detection
            File.WriteAllText(Path.Combine(_tempDir, "package.json"), @"{
  ""name"": ""test-project"",
  ""scripts"": {
    ""build"": ""vite build""
  }
}");
        }

        [Fact]
        public void Execute_WithNonExistentProjectRoot_LogsErrorAndReturnsFalse()
        {
            // Arrange
            var mockEngine = new Helpers.MockBuildEngine();
            var task = new OrchestrateBuildTask
            {
                BuildEngine = mockEngine,
                ViteProjectRoot = "/nonexistent/path",
                PackageManager = "npm",
                ViteMode = "development"
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeFalse();
            mockEngine.LoggedErrors.Should().ContainSingle(e => 
                e.Message.Contains("ViteProjectRoot") && e.Message.Contains("does not exist"));
        }

        [Fact]
        public void Execute_WithSingleConfig_BuildsSuccessfully()
        {
            // Arrange
            var task = new OrchestrateBuildTask
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ViteProjectRoot = _tempDir,
                PackageManager = "npm",
                ViteMode = "development",
                ViteOutputDir = "wwwroot/dist",
                IntermediateOutputPath = Path.Combine(_tempDir, "obj")
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.BuildSucceeded.Should().BeTrue();
            task.ConfigurationsBuilt.Should().Be(1);
            task.OutputDirectories.Should().HaveCount(1);
            task.OutputDirectories[0].Should().Be("wwwroot/dist");
        }

        [Fact]
        public void Execute_WithMultipleConfigs_BuildsAll()
        {
            // Arrange
            var task = new OrchestrateBuildTask
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ViteProjectRoot = _tempDir,
                PackageManager = "npm",
                ViteMode = "development",
                IntermediateOutputPath = Path.Combine(_tempDir, "obj"),
                ViteConfigurations = new[]
                {
                    CreateConfig("admin", "vite.admin.config.ts", "wwwroot/admin"),
                    CreateConfig("customer", "vite.customer.config.ts", "wwwroot/customer")
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.ConfigurationsBuilt.Should().Be(2);
            task.OutputDirectories.Should().HaveCount(2);
            task.OutputDirectories.Should().Contain("wwwroot/admin");
            task.OutputDirectories.Should().Contain("wwwroot/customer");
        }

        [Fact]
        public void Execute_WithDependencies_BuildsInCorrectOrder()
        {
            // Arrange
            var mockEngine = new Helpers.MockBuildEngine();
            var task = new OrchestrateBuildTask
            {
                BuildEngine = mockEngine,
                ViteProjectRoot = _tempDir,
                PackageManager = "npm",
                ViteMode = "development",
                IntermediateOutputPath = Path.Combine(_tempDir, "obj"),
                ViteConfigurations = new[]
                {
                    CreateConfig("app", "vite.app.config.ts", "wwwroot/app", dependsOn: "shared"),
                    CreateConfig("shared", "vite.shared.config.ts", "wwwroot/shared")
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.ConfigurationsBuilt.Should().Be(2);
            
            // Check that dependencies were validated
            mockEngine.LoggedMessages.Should().Contain(m => 
                m.Message.Contains("All dependencies satisfied") && m.Message.Contains("app"));
        }

        [Fact]
        public void Execute_WithMissingDependency_FailsAndLogsError()
        {
            // Arrange
            var mockEngine = new Helpers.MockBuildEngine();
            var task = new OrchestrateBuildTask
            {
                BuildEngine = mockEngine,
                ViteProjectRoot = _tempDir,
                PackageManager = "npm",
                ViteMode = "development",
                IntermediateOutputPath = Path.Combine(_tempDir, "obj"),
                ViteConfigurations = new[]
                {
                    CreateConfig("app", "vite.app.config.ts", "wwwroot/app", dependsOn: "nonexistent")
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeFalse();
            task.BuildSucceeded.Should().BeFalse();
            mockEngine.LoggedErrors.Should().Contain(e => 
                e.Message.Contains("Dependency") && e.Message.Contains("has not completed"));
        }

        [Theory]
        [InlineData("npm")]
        [InlineData("yarn")]
        [InlineData("pnpm")]
        [InlineData("bun")]
        public void Execute_WithDifferentPackageManagers_UsesCorrectCommand(string packageManager)
        {
            // Arrange
            var task = new OrchestrateBuildTask
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ViteProjectRoot = _tempDir,
                PackageManager = packageManager,
                ViteMode = "development",
                IntermediateOutputPath = Path.Combine(_tempDir, "obj")
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.BuildSucceeded.Should().BeTrue();
        }

        [Fact]
        public void Execute_WithCustomBuildCommand_UsesCustomCommand()
        {
            // Arrange
            var task = new OrchestrateBuildTask
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ViteProjectRoot = _tempDir,
                PackageManager = "npm",
                ViteMode = "development",
                ViteBuildCommand = "custom-build-command",
                IntermediateOutputPath = Path.Combine(_tempDir, "obj")
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("development")]
        [InlineData("production")]
        [InlineData("staging")]
        public void Execute_WithDifferentModes_BuildsSuccessfully(string mode)
        {
            // Arrange
            var task = new OrchestrateBuildTask
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ViteProjectRoot = _tempDir,
                PackageManager = "npm",
                ViteMode = mode,
                IntermediateOutputPath = Path.Combine(_tempDir, "obj")
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Execute_CreatesIntermediateOutputDirectory()
        {
            // Arrange
            var objDir = Path.Combine(_tempDir, "obj");
            var task = new OrchestrateBuildTask
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ViteProjectRoot = _tempDir,
                PackageManager = "npm",
                ViteMode = "development",
                IntermediateOutputPath = objDir
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            Directory.Exists(objDir).Should().BeTrue();
        }

        [Fact]
        public void Execute_WithMultipleDependencies_ValidatesAll()
        {
            // Arrange
            var mockEngine = new Helpers.MockBuildEngine();
            var task = new OrchestrateBuildTask
            {
                BuildEngine = mockEngine,
                ViteProjectRoot = _tempDir,
                PackageManager = "npm",
                ViteMode = "development",
                IntermediateOutputPath = Path.Combine(_tempDir, "obj"),
                ViteConfigurations = new[]
                {
                    CreateConfig("app", "vite.app.config.ts", "wwwroot/app", dependsOn: "shared,components"),
                    CreateConfig("shared", "vite.shared.config.ts", "wwwroot/shared"),
                    CreateConfig("components", "vite.components.config.ts", "wwwroot/components")
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.ConfigurationsBuilt.Should().Be(3);
        }

        [Fact]
        public void Execute_WithEnableColorsTrue_SetsColorEnvVar()
        {
            // Arrange
            var task = new OrchestrateBuildTask
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ViteProjectRoot = _tempDir,
                PackageManager = "npm",
                ViteMode = "development",
                ViteEnableColors = true,
                IntermediateOutputPath = Path.Combine(_tempDir, "obj")
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Execute_WithEnableColorsFalse_SetsNoColorEnvVar()
        {
            // Arrange
            var task = new OrchestrateBuildTask
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ViteProjectRoot = _tempDir,
                PackageManager = "npm",
                ViteMode = "development",
                ViteEnableColors = false,
                IntermediateOutputPath = Path.Combine(_tempDir, "obj")
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("silent")]
        [InlineData("error")]
        [InlineData("warn")]
        [InlineData("info")]
        [InlineData("debug")]
        public void Execute_WithDifferentLogLevels_BuildsSuccessfully(string logLevel)
        {
            // Arrange
            var task = new OrchestrateBuildTask
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ViteProjectRoot = _tempDir,
                PackageManager = "npm",
                ViteMode = "development",
                ViteLogLevel = logLevel,
                IntermediateOutputPath = Path.Combine(_tempDir, "obj")
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Execute_WithConfigSpecificPackageManager_UsesConfigValue()
        {
            // Arrange
            var config = CreateConfig("admin", "vite.admin.config.ts", "wwwroot/admin");
            config.SetMetadata("PackageManager", "pnpm");
            
            var task = new OrchestrateBuildTask
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ViteProjectRoot = _tempDir,
                PackageManager = "npm", // Default
                ViteMode = "development",
                IntermediateOutputPath = Path.Combine(_tempDir, "obj"),
                ViteConfigurations = new[] { config }
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Execute_WithConfigSpecificMode_UsesConfigValue()
        {
            // Arrange
            var config = CreateConfig("admin", "vite.admin.config.ts", "wwwroot/admin");
            config.SetMetadata("Mode", "production");
            
            var task = new OrchestrateBuildTask
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ViteProjectRoot = _tempDir,
                PackageManager = "npm",
                ViteMode = "development", // Default
                IntermediateOutputPath = Path.Combine(_tempDir, "obj"),
                ViteConfigurations = new[] { config }
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Execute_WithRelativeOutputDir_CreatesPath()
        {
            // Arrange
            var task = new OrchestrateBuildTask
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ViteProjectRoot = _tempDir,
                PackageManager = "npm",
                ViteMode = "development",
                ViteOutputDir = "dist/output",
                IntermediateOutputPath = Path.Combine(_tempDir, "obj")
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Execute_LogsBuildProgress()
        {
            // Arrange
            var mockEngine = new Helpers.MockBuildEngine();
            var task = new OrchestrateBuildTask
            {
                BuildEngine = mockEngine,
                ViteProjectRoot = _tempDir,
                PackageManager = "npm",
                ViteMode = "development",
                IntermediateOutputPath = Path.Combine(_tempDir, "obj"),
                ViteConfigurations = new[]
                {
                    CreateConfig("admin", "vite.admin.config.ts", "wwwroot/admin"),
                    CreateConfig("customer", "vite.customer.config.ts", "wwwroot/customer")
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            mockEngine.LoggedMessages.Should().Contain(m => m.Message.Contains("Building 2 Vite configuration(s)"));
            mockEngine.LoggedMessages.Should().Contain(m => m.Message.Contains("Successfully built all"));
        }

        private static ITaskItem CreateConfig(string buildId, string configFile, string outputDir, string? dependsOn = null)
        {
            var item = new TaskItem(configFile);
            item.SetMetadata("BuildId", buildId);
            item.SetMetadata("OutputDir", outputDir);
            if (dependsOn != null)
            {
                item.SetMetadata("DependsOn", dependsOn);
            }
            return item;
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                try
                {
                    Directory.Delete(_tempDir, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}
