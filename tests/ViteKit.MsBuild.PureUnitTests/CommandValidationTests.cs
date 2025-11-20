using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;
using FluentAssertions;
using ViteKit.MsBuild.Tasks;

namespace Vite.MsBuild.PureUnitTests
{
    /// <summary>
    /// Pure unit tests for command validation - NO MSBuild execution required!
    /// These tests are fast, reliable, and cover all command construction scenarios.
    /// </summary>
    public class CommandValidationTests : IDisposable
    {
        private readonly List<string> _tempDirectories = new();

        [Fact]
        public void Should_Execute_Zero_Config_Experience()
        {
            // Arrange: Minimal project setup
            var projectDir = CreateTempProject();
            
            // Act: Build command with defaults
            var config = new ViteBuildConfiguration { ProjectRoot = projectDir, PackageManager = PackageManager.Npm };
            var command = new ViteCommandBuilder(config).Build();
            
            // Assert: Zero-config works
            command.ToString().Should().Be("npx vite build");
            command.WorkingDirectory.Should().Be(projectDir);
        }

        [Theory]
        [InlineData(PackageManager.Npm, "npm", "run build")]
        [InlineData(PackageManager.Yarn, "yarn", "build")]  // Yarn can run scripts directly
        [InlineData(PackageManager.Pnpm, "pnpm", "run build")]
        [InlineData(PackageManager.Bun, "bun", "run build")]
        public void Should_Support_All_Package_Managers_With_Scripts(
            PackageManager pm, string expectedExe, string expectedCmd)
        {
            // Arrange: Project with package.json build script
            var projectDir = CreateProjectWithPackageJson(new Dictionary<string, string>
            {
                ["build"] = "vite build"
            });

            // Act: Build command for package manager
            var config = new ViteBuildConfiguration { ProjectRoot = projectDir, PackageManager = pm };
            var command = new ViteCommandBuilder(config).Build();

            // Assert: Correct package manager used
            command.Executable.Should().Be(expectedExe);
            command.Command.Should().Be(expectedCmd);
        }

        [Theory]
        [InlineData(PackageManager.Npm, "npx vite build")]
        [InlineData(PackageManager.Yarn, "yarn dlx vite build")]
        [InlineData(PackageManager.Pnpm, "pnpm dlx vite build")]
        [InlineData(PackageManager.Bun, "bunx vite build")]
        public void Should_Fallback_To_Direct_Commands_Without_Scripts(
            PackageManager pm, string expectedCommand)
        {
            // Arrange: Project without build script
            var projectDir = CreateProjectWithPackageJson(new Dictionary<string, string>
            {
                ["dev"] = "vite"  // No build script
            });

            // Act: Build command
            var config = new ViteBuildConfiguration { ProjectRoot = projectDir, PackageManager = pm };
            var command = new ViteCommandBuilder(config).Build();

            // Assert: Direct command fallback
            command.ToString().Should().StartWith(expectedCommand);
        }

        [Fact]
        public void Should_Support_Custom_Command_Override()
        {
            // Arrange: Any project
            var projectDir = CreateTempProject();

            // Act: Override with custom command
            var config = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Npm,
                CustomBuildCommand = "yarn build:custom",
                Mode = "staging"
            };
            var command = new ViteCommandBuilder(config).Build();

            // Assert: Custom command takes precedence
            command.Executable.Should().Be("yarn");
            command.Command.Should().Be("build:custom");
            command.Mode.Should().Be("staging");
        }

        [Fact]
        public void Should_Build_Complete_Command_With_All_Options()
        {
            // Arrange: Project directory
            var projectDir = CreateTempProject();

            // Act: Build command with all options
            var config = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Pnpm,
                ConfigFile = "vite.admin.config.ts",
                Mode = "production",
                OutputDir = "dist/admin",
                LogLevel = "silent",
                EnableColors = false
            };
            config.Environment["NODE_ENV"] = "production";
            config.Environment["VITE_API_URL"] = "https://api.prod.com";
            var command = new ViteCommandBuilder(config).Build();

            // Assert: All options included
            var commandString = command.ToString();
            commandString.Should().Contain("--config \"vite.admin.config.ts\"");
            commandString.Should().Contain("--mode production");
            commandString.Should().Contain("--outDir \"dist/admin\"");
            commandString.Should().Contain("--logLevel silent");
            
            // Colors are handled via environment variables, not CLI flags
            command.Environment.Should().ContainKey("NO_COLOR").WhoseValue.Should().Be("1");

            command.Environment.Should().ContainKey("NODE_ENV").WhoseValue.Should().Be("production");
            command.Environment.Should().ContainKey("VITE_API_URL").WhoseValue.Should().Be("https://api.prod.com");
        }

        [Fact]
        public void Should_Support_Multi_SPA_Configuration()
        {
            // Arrange: Multi-SPA project
            var projectDir = CreateTempProject();

            var adminConfig = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Npm,
                ConfigFile = "Areas/Admin/vite.admin.config.ts",
                Mode = "development",
                OutputDir = "wwwroot/admin"
            };
            var adminCommand = new ViteCommandBuilder(adminConfig).Build();

            var customerConfig = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Npm,
                ConfigFile = "Areas/Customer/vite.customer.config.ts",
                Mode = "production",
                OutputDir = "wwwroot/customer"
            };
            var customerCommand = new ViteCommandBuilder(customerConfig).Build();

            // Assert: Independent configurations
            adminCommand.ConfigPath.Should().Be("Areas/Admin/vite.admin.config.ts");
            adminCommand.Mode.Should().Be("development");
            adminCommand.OutputDir.Should().Be("wwwroot/admin");

            customerCommand.ConfigPath.Should().Be("Areas/Customer/vite.customer.config.ts");
            customerCommand.Mode.Should().Be("production");
            customerCommand.OutputDir.Should().Be("wwwroot/customer");
        }

        [Fact]
        public void Should_Handle_Missing_Dependencies_Scenario()
        {
            // Arrange: Empty project (no package.json)
            var projectDir = CreateTempProject();

            // Act: Build command without dependencies
            var config = new ViteBuildConfiguration { ProjectRoot = projectDir, PackageManager = PackageManager.Npm };
            var command = new ViteCommandBuilder(config).Build();

            // Assert: Fallback to direct execution
            command.ToString().Should().StartWith("npx vite build");
        }

        [Fact]
        public void Should_Handle_Invalid_Package_Json_Gracefully()
        {
            // Arrange: Project with corrupted package.json
            var projectDir = CreateTempProject();
            File.WriteAllText(Path.Combine(projectDir, "package.json"), "{ invalid json }");

            // Act: Build command despite invalid JSON
            var config = new ViteBuildConfiguration { ProjectRoot = projectDir, PackageManager = PackageManager.Npm };
            var command = new ViteCommandBuilder(config).Build();

            // Assert: Graceful fallback
            command.ToString().Should().StartWith("npx vite build");
        }

        [Theory]
        [InlineData("silent")]
        [InlineData("warn")]
        [InlineData("info")]
        [InlineData("debug")]
        public void Should_Support_All_Log_Levels(string logLevel)
        {
            // Arrange: Project
            var projectDir = CreateTempProject();

            // Act: Build command with log level
            var config = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Npm,
                LogLevel = logLevel
            };
            var command = new ViteCommandBuilder(config).Build();

            // Assert: Log level included
            command.ToString().Should().Contain($"--logLevel {logLevel}");
        }

        [Theory]
        [InlineData(true, "FORCE_COLOR", "1")]
        [InlineData(false, "NO_COLOR", "1")]
        public void Should_Control_Color_Output(bool enableColors, string expectedEnvKey, string expectedEnvValue)
        {
            // Arrange: Project
            var projectDir = CreateTempProject();

            // Act: Build command with color setting
            var config = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Npm,
                EnableColors = enableColors
            };
            var command = new ViteCommandBuilder(config).Build();

            // Assert: Environment variable set correctly (more reliable than CLI flags)
            command.Environment.Should().ContainKey(expectedEnvKey);
            command.Environment[expectedEnvKey].Should().Be(expectedEnvValue);
        }

        [Fact]
        public void Should_Support_Yarn_Berry_Configuration()
        {
            // Arrange: Yarn Berry project
            var projectDir = CreateProjectWithYarnBerry();

            // Act: Build command
            var config = new ViteBuildConfiguration { ProjectRoot = projectDir, PackageManager = PackageManager.Yarn };
            var command = new ViteCommandBuilder(config).Build();

            // Assert: Yarn Berry syntax (no "run" needed)
            command.Command.Should().Be("build");  // Not "run build"
        }

        [Fact]
        public void Should_Support_Command_Priority_Hierarchy()
        {
            // Arrange: Project with package.json
            var projectDir = CreateProjectWithPackageJson(new Dictionary<string, string>
            {
                ["build"] = "vite build"
            });

            // Test 1: Custom command (highest priority)
            var customConfig = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Npm,
                CustomBuildCommand = "yarn build:special"
            };
            var customCommand = new ViteCommandBuilder(customConfig).Build();
            customCommand.ToString().Should().StartWith("yarn build:special");

            // Test 2: Package script (middle priority)
            var scriptConfig = new ViteBuildConfiguration { ProjectRoot = projectDir, PackageManager = PackageManager.Npm };
            var scriptCommand = new ViteCommandBuilder(scriptConfig)
                .Build();
            scriptCommand.ToString().Should().StartWith("npm run build");

            // Test 3: Direct fallback (when no script available)
            File.Delete(Path.Combine(projectDir, "package.json"));
            var fallbackConfig = new ViteBuildConfiguration { ProjectRoot = projectDir, PackageManager = PackageManager.Npm };
            var fallbackCommand = new ViteCommandBuilder(fallbackConfig).Build();
            fallbackCommand.ToString().Should().StartWith("npx vite build");
        }

        // Helper methods
        private string CreateTempProject()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"ViteTest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            _tempDirectories.Add(tempDir);
            return tempDir;
        }

        private string CreateProjectWithPackageJson(Dictionary<string, string> scripts)
        {
            var projectDir = CreateTempProject();
            var packageJson = new
            {
                name = "test-project",
                scripts = scripts
            };

            var json = JsonSerializer.Serialize(packageJson, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(projectDir, "package.json"), json);
            return projectDir;
        }

        private string CreateProjectWithYarnBerry()
        {
            var projectDir = CreateProjectWithPackageJson(new Dictionary<string, string>
            {
                ["build"] = "vite build"
            });

            // Create .yarnrc.yml to indicate Yarn Berry
            File.WriteAllText(Path.Combine(projectDir, ".yarnrc.yml"), "yarnPath: .yarn/releases/yarn-3.0.0.cjs");
            return projectDir;
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
                    catch
                    {
                        // Ignore cleanup failures in tests
                    }
                }
            }
        }
    }
}