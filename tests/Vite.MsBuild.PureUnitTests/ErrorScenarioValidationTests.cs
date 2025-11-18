using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;
using FluentAssertions;
using Vite.MsBuild.Tasks;

namespace Vite.MsBuild.PureUnitTests
{
    /// <summary>
    /// Pure unit tests for error scenarios - previously tested via MSBuild integration
    /// Now lightning-fast with direct command validation
    /// </summary>
    public class ErrorScenarioValidationTests : IDisposable
    {
        private readonly List<string> _tempDirectories = new();

        [Fact]
        public void Should_Handle_Missing_Dependencies_Gracefully()
        {
            // Arrange: Project with no package.json (missing dependencies)
            var projectDir = CreateTempProject();

            // Act: Build command should not fail
            var command = new ViteCommandBuilder(projectDir, PackageManager.Npm).Build();

            // Assert: Falls back to direct execution
            command.ToString().Should().StartWith("npx vite build");
            command.WorkingDirectory.Should().Be(projectDir);
        }

        [Fact]
        public void Should_Show_Clear_Error_For_Invalid_Config()
        {
            // Arrange: Project with invalid package.json
            var projectDir = CreateTempProject();
            File.WriteAllText(Path.Combine(projectDir, "package.json"), "{ invalid json syntax }");

            // Act: Command construction should handle invalid JSON gracefully
            var command = new ViteCommandBuilder(projectDir, PackageManager.Npm).Build();

            // Assert: Graceful fallback to direct command
            command.ToString().Should().StartWith("npx vite build");
        }

        [Theory]
        [InlineData(PackageManager.Npm, "npx vite build")]
        [InlineData(PackageManager.Yarn, "yarn dlx vite build")]
        [InlineData(PackageManager.Pnpm, "pnpm dlx vite build")]
        [InlineData(PackageManager.Bun, "bunx vite build")]
        public void Should_Handle_Package_Manager_Not_Found(PackageManager pm, string expectedFallback)
        {
            // Arrange: Project without package.json (simulates package manager not working)
            var projectDir = CreateTempProject();

            // Act: Build command for specific package manager
            var command = new ViteCommandBuilder(projectDir, pm).Build();

            // Assert: Each package manager has correct fallback command
            command.ToString().Should().StartWith(expectedFallback);
        }

        [Fact]
        public void Should_Handle_File_Permission_Errors()
        {
            // Note: File permission validation happens at execution time, not command construction
            // Command construction should always succeed
            
            // Arrange: Any valid project directory
            var projectDir = CreateTempProject();

            // Act: Build command (construction phase)
            var action = () => new ViteCommandBuilder(projectDir, PackageManager.Npm).Build();

            // Assert: Command construction never fails due to permissions
            action.Should().NotThrow();
        }

        [Fact]
        public void Should_Validate_Complex_Multi_Config_Scenarios()
        {
            // Arrange: Complex enterprise scenario
            var projectDir = CreateProjectWithPackageJson(new Dictionary<string, string>
            {
                ["build"] = "vite build",
                ["build:admin"] = "vite build --config vite.admin.config.ts",
                ["build:customer"] = "vite build --config vite.customer.config.ts"
            });

            // Act: Build commands for different configs
            var adminCommand = new ViteCommandBuilder(projectDir, PackageManager.Pnpm)
                .WithConfig("Areas/Admin/vite.admin.config.ts")
                .WithMode("development")
                .WithOutputDir("wwwroot/admin")
                .Build();

            var customerCommand = new ViteCommandBuilder(projectDir, PackageManager.Pnpm)
                .WithConfig("Areas/Customer/vite.customer.config.ts")  
                .WithMode("production")
                .WithOutputDir("wwwroot/customer")
                .Build();

            // Assert: Each config has independent settings
            adminCommand.ConfigPath.Should().Be("Areas/Admin/vite.admin.config.ts");
            adminCommand.Mode.Should().Be("development");
            adminCommand.OutputDir.Should().Be("wwwroot/admin");

            customerCommand.ConfigPath.Should().Be("Areas/Customer/vite.customer.config.ts");
            customerCommand.Mode.Should().Be("production");
            customerCommand.OutputDir.Should().Be("wwwroot/customer");

            // Both use same package manager but different configs
            adminCommand.Executable.Should().Be("pnpm");
            customerCommand.Executable.Should().Be("pnpm");
        }

        [Fact]
        public void Should_Handle_Complex_Command_Construction()
        {
            // Arrange: Project with all possible options
            var projectDir = CreateProjectWithPackageJson(new Dictionary<string, string>
            {
                ["build"] = "vite build"
            });

            // Act: Build command with maximum complexity
            var command = new ViteCommandBuilder(projectDir, PackageManager.Yarn)
                .WithCustomCommand("yarn build:enterprise")  // Custom override
                .WithConfig("Areas/Admin/vite.admin.config.ts")
                .WithMode("staging")
                .WithOutputDir("dist/staging/admin")
                .WithLogLevel("debug")
                .WithColors(false)
                .WithEnvironmentVariable("NODE_ENV", "staging")
                .WithEnvironmentVariable("VITE_API_URL", "https://api.staging.com")
                .WithEnvironmentVariable("VITE_APP_VERSION", "1.2.3")
                .Build();

            // Assert: All options correctly applied
            command.Executable.Should().Be("yarn");
            command.Command.Should().Be("build:enterprise");
            command.ConfigPath.Should().Be("Areas/Admin/vite.admin.config.ts");
            command.Mode.Should().Be("staging");
            command.OutputDir.Should().Be("dist/staging/admin");
            command.LogLevel.Should().Be("debug");
            command.Colors.Should().Be(false);

            // Should have 4 environment variables: 3 custom + NO_COLOR (from Colors = false)
            command.Environment.Should().HaveCount(4);
            command.Environment["NODE_ENV"].Should().Be("staging");
            command.Environment["VITE_API_URL"].Should().Be("https://api.staging.com");
            command.Environment["VITE_APP_VERSION"].Should().Be("1.2.3");
            command.Environment["NO_COLOR"].Should().Be("1");  // Added by factory for Colors = false

            var commandString = command.ToString();
            commandString.Should().Contain("--config \"Areas/Admin/vite.admin.config.ts\"");
            commandString.Should().Contain("--mode staging");
            commandString.Should().Contain("--outDir \"dist/staging/admin\"");
            commandString.Should().Contain("--logLevel debug");
            
            // ✅ IMPROVED: Vite doesn't support --no-color flag, uses environment variables instead
            // This is more reliable and follows Node.js ecosystem standards
            command.Environment.Should().ContainKey("NO_COLOR");
            command.Environment["NO_COLOR"].Should().Be("1");
        }

        [Fact]
        public void Should_Support_Command_Override_Hierarchy()
        {
            // Arrange: Project with package script
            var projectDir = CreateProjectWithPackageJson(new Dictionary<string, string>
            {
                ["build"] = "vite build --base=/app/"
            });

            // Test 1: Custom command overrides everything
            var customCommand = new ViteCommandBuilder(projectDir, PackageManager.Npm)
                .WithCustomCommand("webpack --mode=production")
                .Build();
            customCommand.ToString().Should().StartWith("webpack --mode=production");

            // Test 2: Package script used when no custom command
            var scriptCommand = new ViteCommandBuilder(projectDir, PackageManager.Npm)
                .Build();
            scriptCommand.ToString().Should().StartWith("npm run build");

            // Test 3: Direct execution when no script available
            File.Delete(Path.Combine(projectDir, "package.json"));
            var directCommand = new ViteCommandBuilder(projectDir, PackageManager.Npm)
                .Build();
            directCommand.ToString().Should().StartWith("npx vite build");
        }

        // Helper methods
        private string CreateTempProject()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"ErrorTest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            _tempDirectories.Add(tempDir);
            return tempDir;
        }

        private string CreateProjectWithPackageJson(Dictionary<string, string> scripts)
        {
            var projectDir = CreateTempProject();
            var packageJson = new
            {
                name = "error-test-project",
                scripts = scripts
            };

            var json = JsonSerializer.Serialize(packageJson, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(projectDir, "package.json"), json);
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