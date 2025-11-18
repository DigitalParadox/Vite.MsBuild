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
    /// Comprehensive tests for factory pattern edge cases and robustness
    /// These tests validate critical scenarios that could break in production
    /// </summary>
    public class FactoryPatternRobustnessTests : IDisposable
    {
        private readonly List<string> _tempDirectories = new();

        [Theory]
        [InlineData(PackageManager.Npm, "npm", "run", "build")]
        [InlineData(PackageManager.Yarn, "yarn", "", "build")]  // Yarn runs directly, no "run"
        [InlineData(PackageManager.Pnpm, "pnpm", "run", "build")]
        [InlineData(PackageManager.Bun, "bun", "run", "build")]
        public void Should_Handle_Script_Command_Arguments_Correctly(
            PackageManager packageManager, 
            string expectedExecutable, 
            string expectedMiddle, 
            string expectedScript)
        {
            // Arrange: Project with custom script
            var projectDir = CreateProjectWithScript("build", "vite build --base=/custom/");
            
            // Act: Build command
            var command = new ViteCommandBuilder(projectDir, packageManager).BuildCommand();
            
            // Assert: Correct script execution pattern
            command.Executable.Should().Be(expectedExecutable);
            
            if (string.IsNullOrEmpty(expectedMiddle))
            {
                // Yarn direct execution
                command.Command.Should().Be(expectedScript);
            }
            else
            {
                // npm/pnpm/bun with "run" keyword
                command.Command.Should().Be($"{expectedMiddle} {expectedScript}");
            }
        }

        [Theory]
        [InlineData(PackageManager.Npm, "npx", "vite")]
        [InlineData(PackageManager.Yarn, "yarn dlx", "vite")]
        [InlineData(PackageManager.Pnpm, "pnpm dlx", "vite")]
        [InlineData(PackageManager.Bun, "bunx", "vite")]
        public void Should_Map_Direct_Tool_Executables_Correctly(
            PackageManager packageManager,
            string expectedTool,
            string expectedCommand)
        {
            // Arrange: Project without package.json (forces direct tool usage)
            var projectDir = CreateTempDirectory();
            
            // Act: Build command (will fallback to direct tool)
            var command = new ViteCommandBuilder(projectDir, packageManager).BuildCommand();
            
            // Assert: Correct direct tool mapping
            command.Executable.Should().Be(expectedTool);
            command.Command.Should().Be($"{expectedCommand} build");
        }

        [Fact]
        public void Should_Merge_Environment_Variables_With_Precedence()
        {
            // Arrange: Project with environment settings
            var projectDir = CreateTempDirectory();
            
            // Act: Build command with multiple environment variables
            var command = new ViteCommandBuilder(projectDir, PackageManager.Npm)
                .WithEnvironmentVariable("NODE_ENV", "production")
                .WithEnvironmentVariable("VITE_API_URL", "https://api.prod.com")
                .WithEnvironmentVariable("CUSTOM_VAR", "value1")
                .WithEnvironmentVariable("CUSTOM_VAR", "value2")  // Override
                .WithColors(false)  // Should add NO_COLOR=1
                .BuildCommand();
            
            // Assert: Environment variables merged correctly with precedence
            command.Environment.Should().HaveCount(4);
            command.Environment["NODE_ENV"].Should().Be("production");
            command.Environment["VITE_API_URL"].Should().Be("https://api.prod.com");
            command.Environment["CUSTOM_VAR"].Should().Be("value2");  // Last one wins
            command.Environment["NO_COLOR"].Should().Be("1");  // Added by WithColors(false)
        }

        [Theory]
        [InlineData("C:\\Program Files\\MyApp\\vite.config.ts", "\"C:\\Program Files\\MyApp\\vite.config.ts\"")]
        [InlineData("C:\\App With Spaces\\config.js", "\"C:\\App With Spaces\\config.js\"")]
        [InlineData("/usr/local/app with spaces/vite.config.ts", "\"/usr/local/app with spaces/vite.config.ts\"")]
        [InlineData("./simple-path/config.ts", "\"./simple-path/config.ts\"")]
        [InlineData("config.ts", "\"config.ts\"")]  // Even simple paths should be quoted for consistency
        public void Should_Quote_Paths_With_Spaces_And_Special_Characters(string configPath, string expectedQuoted)
        {
            // Arrange: Project
            var projectDir = CreateTempDirectory();
            
            // Act: Build command with special characters in path
            var command = new ViteCommandBuilder(projectDir, PackageManager.Npm)
                .WithConfig(configPath)
                .BuildCommand();
            
            // Assert: Path is properly quoted in command string
            var commandString = command.ToString();
            commandString.Should().Contain($"--config {expectedQuoted}");
        }

        [Theory]
        [InlineData("npm run custom-build")]  // Custom npm script  
        [InlineData("yarn build:enterprise")] // Yarn script (direct)
        [InlineData("pnpm run build --watch")] // PNPM script with args
        public void Should_Support_Custom_Commands_With_Literal_Execution(string customCommand)
        {
            // Arrange: Project directory (scripts not relevant for custom commands)
            var projectDir = CreateTempDirectory();
            
            // Act: Build command with custom command using new factory method
            var command = new ViteCommandBuilder(projectDir, PackageManager.Npm)
                .WithCustomCommand(customCommand)
                .BuildCommand();  // Use new factory method instead of legacy Build()
            
            // Assert: Uses literal command execution as user specified
            var parts = customCommand.Split(' ', 2);
            command.Executable.Should().Be(parts[0]);
            command.Command.Should().Be(parts.Length > 1 ? parts[1] : "");
        }

        [Fact]
        public void Should_Fallback_To_Direct_Tool_When_No_Scripts_Available()
        {
            // Arrange: Project without any package.json or scripts
            var projectDir = CreateTempDirectory();
            
            // Act: Build command (no scripts available)
            var command = new ViteCommandBuilder(projectDir, PackageManager.Npm).BuildCommand();
            
            // Assert: Falls back to direct tool execution
            command.Executable.Should().Be("npx");
            command.Command.Should().Be("vite build");
        }

        [Fact]
        public void Should_Handle_Empty_Package_Json_Gracefully()
        {
            // Arrange: Project with empty package.json
            var projectDir = CreateTempDirectory();
            File.WriteAllText(Path.Combine(projectDir, "package.json"), "{}");
            
            // Act: Build command
            var command = new ViteCommandBuilder(projectDir, PackageManager.Pnpm).BuildCommand();
            
            // Assert: Falls back to direct tool (no scripts section)
            command.Executable.Should().Be("pnpm dlx");
            command.Command.Should().Be("vite build");
        }

        [Fact]
        public void Should_Handle_Invalid_Package_Json_Gracefully()
        {
            // Arrange: Project with malformed package.json
            var projectDir = CreateTempDirectory();
            File.WriteAllText(Path.Combine(projectDir, "package.json"), "{ invalid json");
            
            // Act: Build command (should not crash)
            var command = new ViteCommandBuilder(projectDir, PackageManager.Yarn).BuildCommand();
            
            // Assert: Falls back to direct tool when JSON is malformed
            command.Executable.Should().Be("yarn dlx");
            command.Command.Should().Be("vite build");
        }

        [Fact]
        public void Should_Preserve_Command_Builder_Fluent_Chain_State()
        {
            // Arrange: Project
            var projectDir = CreateTempDirectory();
            
            // Act: Build command using method chaining
            var builder = new ViteCommandBuilder(projectDir, PackageManager.Npm);
            var command1 = builder
                .WithConfig("config1.ts")
                .WithMode("development")
                .BuildCommand();
            
            var command2 = builder
                .WithConfig("config2.ts")  // Should override previous config
                .WithMode("production")    // Should override previous mode
                .BuildCommand();
            
            // Assert: Each build creates independent command with current state
            command1.ConfigPath.Should().Be("config1.ts");
            command1.Mode.Should().Be("development");
            
            command2.ConfigPath.Should().Be("config2.ts");
            command2.Mode.Should().Be("production");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]  // Whitespace only
        public void Should_Handle_Empty_Or_Null_Parameters_Gracefully(string emptyValue)
        {
            // Arrange: Project
            var projectDir = CreateTempDirectory();
            
            // Act: Build command with empty/null values (should not crash)
            var command = new ViteCommandBuilder(projectDir, PackageManager.Npm)
                .WithConfig(emptyValue)      // Empty config should be ignored
                .WithMode(emptyValue)        // Empty mode should be ignored  
                .WithOutputDir(emptyValue)   // Empty output should be ignored
                .BuildCommand();
            
            // Assert: Empty values are handled gracefully (not included in command)
            var commandString = command.ToString();
            commandString.Should().NotContain("--config \"\"");
            commandString.Should().NotContain("--mode \"\"");
            commandString.Should().NotContain("--outDir \"\"");
        }

        // Helper methods
        private string CreateTempDirectory()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            _tempDirectories.Add(tempDir);
            return tempDir;
        }

        private string CreateProjectWithScript(string scriptName, string scriptCommand)
        {
            var projectDir = CreateTempDirectory();
            var packageJson = new
            {
                scripts = new Dictionary<string, string>
                {
                    [scriptName] = scriptCommand
                }
            };
            
            File.WriteAllText(
                Path.Combine(projectDir, "package.json"), 
                JsonSerializer.Serialize(packageJson, new JsonSerializerOptions { WriteIndented = true })
            );
            
            return projectDir;
        }

        public void Dispose()
        {
            foreach (var dir in _tempDirectories)
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
            }
        }
    }
}