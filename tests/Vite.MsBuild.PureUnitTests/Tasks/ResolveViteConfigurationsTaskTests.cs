using FluentAssertions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.IO;
using Vite.MsBuild.Tasks;
using Xunit;

namespace Vite.MsBuild.PureUnitTests.Tasks
{
    public class ResolveViteConfigurationsTaskTests
    {
        private readonly string _tempDir;

        public ResolveViteConfigurationsTaskTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"ViteMsBuildTests_{System.Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
        }

        [Fact]
        public void Execute_WithNoConfigFile_CreatesDefaultConfiguration()
        {
            // Arrange
            var task = new ResolveViteConfigurationsTask
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ViteProjectRoot = _tempDir,
                ViteOutputDir = "wwwroot/dist",
                ViteMode = "development",
                PackageManager = "npm"
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.ResolvedConfigs.Should().HaveCount(1);
            task.IsMultiConfig.Should().BeFalse();
            task.ConfigCount.Should().Be(1);
            
            var config = task.ResolvedConfigs[0];
            config.GetMetadata("BuildId").Should().Be("default");
            config.GetMetadata("OutputDir").Should().Be("wwwroot/dist");
            config.GetMetadata("Mode").Should().Be("development");
            config.GetMetadata("PackageManager").Should().Be("npm");
            config.GetMetadata("Architecture").Should().Be("SPA");
        }

        [Fact]
        public void Execute_WithExistingConfigFile_DetectsAndUsesIt()
        {
            // Arrange
            var configPath = Path.Combine(_tempDir, "vite.config.ts");
            File.WriteAllText(configPath, "export default {}");
            
            var task = new ResolveViteConfigurationsTask
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ViteProjectRoot = _tempDir,
                ViteOutputDir = "wwwroot/dist"
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.ResolvedConfigs[0].GetMetadata("ConfigFile").Should().Be(configPath);
        }

        [Theory]
        [InlineData("vite.config.ts")]
        [InlineData("vite.config.js")]
        [InlineData("vite.config.mts")]
        [InlineData("vite.config.mjs")]
        public void Execute_DetectsAllConfigFileVariants(string configFileName)
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), $"ViteMsBuildTests_{System.Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            
            var configPath = Path.Combine(tempDir, configFileName);
            File.WriteAllText(configPath, "export default {}");
            
            var task = new ResolveViteConfigurationsTask
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ViteProjectRoot = tempDir
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.ResolvedConfigs[0].GetMetadata("ConfigFile").Should().Be(configPath);
            
            // Cleanup
            Directory.Delete(tempDir, true);
        }

        [Fact]
        public void Execute_WithUserDefinedConfigs_ProcessesAll()
        {
            // Arrange
            var config1 = Path.Combine(_tempDir, "vite.admin.config.ts");
            var config2 = Path.Combine(_tempDir, "vite.customer.config.ts");
            File.WriteAllText(config1, "export default {}");
            File.WriteAllText(config2, "export default {}");
            
            var task = new ResolveViteConfigurationsTask
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ViteProjectRoot = _tempDir,
                UserDefinedConfigs = new[]
                {
                    CreateConfigItem("vite.admin.config.ts", "admin", "wwwroot/admin"),
                    CreateConfigItem("vite.customer.config.ts", "customer", "wwwroot/customer")
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.ResolvedConfigs.Should().HaveCount(2);
            task.IsMultiConfig.Should().BeTrue();
            task.ConfigCount.Should().Be(2);
            
            task.ResolvedConfigs[0].GetMetadata("BuildId").Should().Be("admin");
            task.ResolvedConfigs[0].GetMetadata("OutputDir").Should().Be("wwwroot/admin");
            
            task.ResolvedConfigs[1].GetMetadata("BuildId").Should().Be("customer");
            task.ResolvedConfigs[1].GetMetadata("OutputDir").Should().Be("wwwroot/customer");
        }

        [Fact]
        public void Execute_GeneratesBuildIdFromConfigFileName()
        {
            // Arrange
            var configPath = Path.Combine(_tempDir, "vite.admin.config.ts");
            File.WriteAllText(configPath, "export default {}");
            
            var task = new ResolveViteConfigurationsTask
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ViteProjectRoot = _tempDir,
                UserDefinedConfigs = new[]
                {
                    CreateConfigItem("vite.admin.config.ts", null, null) // No BuildId provided
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.ResolvedConfigs[0].GetMetadata("BuildId").Should().Be("admin");
        }

        [Fact]
        public void Execute_GeneratesOutputDirFromBuildId()
        {
            // Arrange
            var configPath = Path.Combine(_tempDir, "vite.admin.config.ts");
            File.WriteAllText(configPath, "export default {}");
            
            var task = new ResolveViteConfigurationsTask
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ViteProjectRoot = _tempDir,
                UserDefinedConfigs = new[]
                {
                    CreateConfigItem("vite.admin.config.ts", "admin", null) // No OutputDir provided
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.ResolvedConfigs[0].GetMetadata("OutputDir").Should().Be("wwwroot/admin");
        }

        [Fact]
        public void Execute_DetectsAreasArchitecture()
        {
            // Arrange
            var areasDir = Path.Combine(_tempDir, "Areas", "Admin");
            Directory.CreateDirectory(areasDir);
            var configPath = Path.Combine(areasDir, "vite.config.ts");
            File.WriteAllText(configPath, "export default {}");
            
            var task = new ResolveViteConfigurationsTask
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ViteProjectRoot = _tempDir,
                UserDefinedConfigs = new[]
                {
                    CreateConfigItem("Areas/Admin/vite.config.ts", "admin", null)
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.ResolvedConfigs[0].GetMetadata("Architecture").Should().Be("Areas");
        }

        [Fact]
        public void Execute_DetectsMultiSpaArchitecture()
        {
            // Arrange
            var spaDir = Path.Combine(_tempDir, "spa", "admin");
            Directory.CreateDirectory(spaDir);
            var configPath = Path.Combine(spaDir, "vite.config.ts");
            File.WriteAllText(configPath, "export default {}");
            
            var task = new ResolveViteConfigurationsTask
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ViteProjectRoot = _tempDir,
                UserDefinedConfigs = new[]
                {
                    CreateConfigItem("spa/admin/vite.config.ts", "admin", null)
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.ResolvedConfigs[0].GetMetadata("Architecture").Should().Be("MultiSPA");
        }

        [Fact]
        public void Execute_WithDuplicateBuildIds_LogsErrorAndReturnsFalse()
        {
            // Arrange
            var mockEngine = new Helpers.MockBuildEngine();
            var config1 = Path.Combine(_tempDir, "vite.admin.config.ts");
            var config2 = Path.Combine(_tempDir, "vite.admin2.config.ts");
            File.WriteAllText(config1, "export default {}");
            File.WriteAllText(config2, "export default {}");
            
            var task = new ResolveViteConfigurationsTask
            {
                BuildEngine = mockEngine,
                ViteProjectRoot = _tempDir,
                UserDefinedConfigs = new[]
                {
                    CreateConfigItem("vite.admin.config.ts", "admin", "wwwroot/admin"),
                    CreateConfigItem("vite.admin2.config.ts", "admin", "wwwroot/admin2") // Duplicate BuildId
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeFalse();
            mockEngine.LoggedErrors.Should().ContainSingle(e => 
                e.Message.Contains("Duplicate BuildId") && e.Message.Contains("admin"));
        }

        [Fact]
        public void Execute_WithConflictingOutputDirs_LogsWarning()
        {
            // Arrange
            var mockEngine = new Helpers.MockBuildEngine();
            var config1 = Path.Combine(_tempDir, "vite.admin.config.ts");
            var config2 = Path.Combine(_tempDir, "vite.customer.config.ts");
            File.WriteAllText(config1, "export default {}");
            File.WriteAllText(config2, "export default {}");
            
            var task = new ResolveViteConfigurationsTask
            {
                BuildEngine = mockEngine,
                ViteProjectRoot = _tempDir,
                UserDefinedConfigs = new[]
                {
                    CreateConfigItem("vite.admin.config.ts", "admin", "wwwroot/shared"),
                    CreateConfigItem("vite.customer.config.ts", "customer", "wwwroot/shared") // Same output
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            mockEngine.LoggedWarnings.Should().ContainSingle(w => 
                w.Message.Contains("Multiple configurations") && w.Message.Contains("same directory"));
        }

        [Fact]
        public void Execute_WithNonExistentProjectRoot_LogsErrorAndReturnsFalse()
        {
            // Arrange
            var mockEngine = new Helpers.MockBuildEngine();
            var task = new ResolveViteConfigurationsTask
            {
                BuildEngine = mockEngine,
                ViteProjectRoot = "/nonexistent/path"
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeFalse();
            mockEngine.LoggedErrors.Should().ContainSingle(e => 
                e.Message.Contains("ViteProjectRoot") && e.Message.Contains("does not exist"));
        }

        [Fact]
        public void Execute_WithRelativePaths_ConvertsToAbsolute()
        {
            // Arrange
            var configPath = Path.Combine(_tempDir, "Areas", "Admin", "vite.config.ts");
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            File.WriteAllText(configPath, "export default {}");
            
            var task = new ResolveViteConfigurationsTask
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ViteProjectRoot = _tempDir,
                UserDefinedConfigs = new[]
                {
                    CreateConfigItem("Areas/Admin/vite.config.ts", "admin", null) // Relative path
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.ResolvedConfigs[0].GetMetadata("ConfigFile").Should().Be(configPath); // Absolute
        }

        [Fact]
        public void Execute_AppliesDefaultsToUserConfigs()
        {
            // Arrange
            var configPath = Path.Combine(_tempDir, "vite.admin.config.ts");
            File.WriteAllText(configPath, "export default {}");
            
            var task = new ResolveViteConfigurationsTask
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ViteProjectRoot = _tempDir,
                ViteMode = "production",
                PackageManager = "pnpm",
                UserDefinedConfigs = new[]
                {
                    CreateConfigItem("vite.admin.config.ts", "admin", null) // No mode/package manager
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.ResolvedConfigs[0].GetMetadata("Mode").Should().Be("production");
            task.ResolvedConfigs[0].GetMetadata("PackageManager").Should().Be("pnpm");
            task.ResolvedConfigs[0].GetMetadata("ProjectRoot").Should().Be(_tempDir);
        }

        private static ITaskItem CreateConfigItem(string itemSpec, string? buildId, string? outputDir)
        {
            var item = new TaskItem(itemSpec);
            if (buildId != null)
                item.SetMetadata("BuildId", buildId);
            if (outputDir != null)
                item.SetMetadata("OutputDir", outputDir);
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
