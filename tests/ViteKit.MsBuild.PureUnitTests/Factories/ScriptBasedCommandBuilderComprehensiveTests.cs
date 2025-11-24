using Xunit;
using FluentAssertions;
using ViteKit.MsBuild.Tasks;
using System.Collections.Generic;

namespace ViteKit.MsBuild.PureUnitTests.Factories
{
    /// <summary>
    /// Comprehensive tests for ScriptBasedCommandBuilder
    /// Validates script-based command construction for all package managers
    /// </summary>
    public class ScriptBasedCommandBuilderComprehensiveTests
    {
        [Theory]
        [InlineData(PackageManager.Npm, "npm", "run build")]
        [InlineData(PackageManager.Yarn, "yarn", "build")]  // Yarn executes scripts directly
        [InlineData(PackageManager.Pnpm, "pnpm", "run build")]
        [InlineData(PackageManager.Bun, "bun", "run build")]
        public void Build_WithDefaultBuildScript_GeneratesCorrectCommand(
            PackageManager packageManager, string expectedExecutable, string expectedCommand)
        {
            // Arrange
            var builder = new ScriptBasedCommandBuilder(packageManager);

            // Act
            var command = builder.Build();

            // Assert
            command.Executable.Should().Be(expectedExecutable);
            command.Command.Should().Be(expectedCommand);
        }

        [Theory]
        [InlineData("dev")]
        [InlineData("start")]
        [InlineData("build:production")]
        [InlineData("custom-script")]
        public void Build_WithCustomScriptName_UsesProvidedScriptName(string scriptName)
        {
            // Arrange
            var builder = new ScriptBasedCommandBuilder(PackageManager.Npm, scriptName);

            // Act
            var command = builder.Build();

            // Assert
            command.Command.Should().Contain(scriptName);
            command.ToString().Should().Contain($"npm run {scriptName}");
        }

        [Fact]
        public void Build_WithMode_IncludesModeInCommand()
        {
            // Arrange
            var builder = new ScriptBasedCommandBuilder(PackageManager.Npm)
                .WithMode("production");

            // Act
            var command = builder.Build();

            // Assert
            command.Mode.Should().Be("production");
            command.ToString().Should().Contain("--mode production");
        }

        [Fact]
        public void Build_WithConfigPath_IncludesConfigInCommand()
        {
            // Arrange
            var builder = new ScriptBasedCommandBuilder(PackageManager.Npm)
                .WithConfigPath("vite.config.ts");

            // Act
            var command = builder.Build();

            // Assert
            command.ConfigPath.Should().Be("vite.config.ts");
            command.ToString().Should().Contain("--config \"vite.config.ts\"");
        }

        [Fact]
        public void Build_WithOutputDir_IncludesOutDirInCommand()
        {
            // Arrange
            var builder = new ScriptBasedCommandBuilder(PackageManager.Npm)
                .WithOutputDir("dist/output");

            // Act
            var command = builder.Build();

            // Assert
            command.OutputDir.Should().Be("dist/output");
            command.ToString().Should().Contain("--outDir \"dist/output\"");
        }

        [Theory]
        [InlineData("silent")]
        [InlineData("error")]
        [InlineData("warn")]
        [InlineData("info")]
        public void Build_WithLogLevel_IncludesLogLevelInCommand(string logLevel)
        {
            // Arrange
            var builder = new ScriptBasedCommandBuilder(PackageManager.Npm)
                .WithLogLevel(logLevel);

            // Act
            var command = builder.Build();

            // Assert
            command.LogLevel.Should().Be(logLevel);
            command.ToString().Should().Contain($"--logLevel {logLevel}");
        }

        [Fact]
        public void Build_WithColorsEnabled_SetsForceColorEnvironmentVariable()
        {
            // Arrange
            var builder = new ScriptBasedCommandBuilder(PackageManager.Npm)
                .WithColors(true);

            // Act
            var command = builder.Build();

            // Assert
            command.Colors.Should().BeTrue();
            command.Environment.Should().ContainKey("FORCE_COLOR");
            command.Environment["FORCE_COLOR"].Should().Be("1");
        }

        [Fact]
        public void Build_WithColorsDisabled_SetsNoColorEnvironmentVariable()
        {
            // Arrange
            var builder = new ScriptBasedCommandBuilder(PackageManager.Npm)
                .WithColors(false);

            // Act
            var command = builder.Build();

            // Assert
            command.Colors.Should().BeFalse();
            command.Environment.Should().ContainKey("NO_COLOR");
            command.Environment["NO_COLOR"].Should().Be("1");
        }

        [Fact]
        public void Build_WithEnvironmentVariables_MergesWithExistingEnvironment()
        {
            // Arrange
            var customEnv = new Dictionary<string, string>
            {
                ["NODE_ENV"] = "production",
                ["DEBUG"] = "vite:*"
            };
            
            var builder = new ScriptBasedCommandBuilder(PackageManager.Npm)
                .WithEnvironment(customEnv)
                .WithColors(true);

            // Act
            var command = builder.Build();

            // Assert
            command.Environment.Should().ContainKey("NODE_ENV");
            command.Environment.Should().ContainKey("DEBUG");
            command.Environment.Should().ContainKey("FORCE_COLOR");
            command.Environment["NODE_ENV"].Should().Be("production");
            command.Environment["DEBUG"].Should().Be("vite:*");
        }

        [Fact]
        public void Build_WithWorkingDirectory_SetsWorkingDirectory()
        {
            // Arrange
            var builder = new ScriptBasedCommandBuilder(PackageManager.Npm)
                .WithWorkingDirectory("/path/to/project");

            // Act
            var command = builder.Build();

            // Assert
            command.WorkingDirectory.Should().Be("/path/to/project");
        }

        [Fact]
        public void Build_WithAllOptions_GeneratesCompleteCommand()
        {
            // Arrange
            var env = new Dictionary<string, string> { ["NODE_ENV"] = "test" };
            var builder = new ScriptBasedCommandBuilder(PackageManager.Npm)
                .WithConfigPath("vite.config.ts")
                .WithMode("production")
                .WithOutputDir("dist")
                .WithLogLevel("info")
                .WithColors(true)
                .WithEnvironment(env)
                .WithWorkingDirectory("/project");

            // Act
            var command = builder.Build();

            // Assert
            command.ConfigPath.Should().Be("vite.config.ts");
            command.Mode.Should().Be("production");
            command.OutputDir.Should().Be("dist");
            command.LogLevel.Should().Be("info");
            command.Colors.Should().BeTrue();
            command.WorkingDirectory.Should().Be("/project");
            command.Environment.Should().ContainKey("NODE_ENV");
            command.Environment.Should().ContainKey("FORCE_COLOR");
        }

        [Fact]
        public void Build_CalledMultipleTimes_GeneratesIdenticalCommands()
        {
            // Arrange
            var builder = new ScriptBasedCommandBuilder(PackageManager.Npm)
                .WithMode("production");

            // Act
            var command1 = builder.Build();
            var command2 = builder.Build();

            // Assert
            command1.ToString().Should().Be(command2.ToString());
            command1.Mode.Should().Be(command2.Mode);
        }

        [Theory]
        [InlineData(PackageManager.Npm, "npm", "run my-script")]
        [InlineData(PackageManager.Yarn, "yarn", "my-script")]
        [InlineData(PackageManager.Pnpm, "pnpm", "run my-script")]
        [InlineData(PackageManager.Bun, "bun", "run my-script")]
        public void Build_WithCustomScriptAcrossPackageManagers_UsesCorrectSyntax(
            PackageManager packageManager, string expectedExecutable, string expectedCommand)
        {
            // Arrange
            var builder = new ScriptBasedCommandBuilder(packageManager, "my-script");

            // Act
            var command = builder.Build();

            // Assert
            command.Executable.Should().Be(expectedExecutable);
            command.Command.Should().Be(expectedCommand);
        }

        [Fact]
        public void WithCustomCommand_IgnoredForScriptBasedBuilder()
        {
            // Arrange
            var builder = new ScriptBasedCommandBuilder(PackageManager.Npm)
                .WithCustomCommand("ignored-command");

            // Act
            var command = builder.Build();

            // Assert
            // Custom command should be ignored for script-based builders
            command.Executable.Should().Be("npm");
            command.Command.Should().Be("run build");
        }

        [Fact]
        public void Build_WithPathContainingSpaces_QuotesProperly()
        {
            // Arrange
            var builder = new ScriptBasedCommandBuilder(PackageManager.Npm)
                .WithConfigPath("path with spaces/vite.config.ts")
                .WithOutputDir("dist with spaces");

            // Act
            var command = builder.Build();

            // Assert
            var commandString = command.ToString();
            commandString.Should().Contain("\"path with spaces/vite.config.ts\"");
            commandString.Should().Contain("\"dist with spaces\"");
        }

        [Fact]
        public void Build_WithSpecialCharactersInPaths_EscapesCorrectly()
        {
            // Arrange
            var builder = new ScriptBasedCommandBuilder(PackageManager.Npm)
                .WithConfigPath("path/@scope/vite.config.ts")
                .WithOutputDir("dist#output");

            // Act
            var command = builder.Build();

            // Assert
            command.ConfigPath.Should().Be("path/@scope/vite.config.ts");
            command.OutputDir.Should().Be("dist#output");
        }

        [Fact]
        public void FluentInterface_AllowsMethodChaining()
        {
            // Arrange & Act
            var builder = new ScriptBasedCommandBuilder(PackageManager.Npm);
            
            var result = builder
                .WithMode("production")
                .WithConfigPath("vite.config.ts")
                .WithOutputDir("dist")
                .WithLogLevel("warn")
                .WithColors(true);

            // Assert
            result.Should().BeSameAs(builder);
            
            var command = result.Build();
            command.Should().NotBeNull();
        }
    }
}

