using Xunit;
using FluentAssertions;
using ViteKit.MsBuild.Tasks;
using System.Collections.Generic;

namespace ViteKit.MsBuild.PureUnitTests.Factories
{
    /// <summary>
    /// Comprehensive tests for DirectToolCommandBuilder
    /// Validates direct Vite execution for all package managers
    /// </summary>
    public class DirectToolCommandBuilderComprehensiveTests
    {
        [Theory]
        [InlineData(PackageManager.Npm, "npx", "vite build")]
        [InlineData(PackageManager.Yarn, "yarn dlx", "vite build")]
        [InlineData(PackageManager.Pnpm, "pnpm dlx", "vite build")]
        [InlineData(PackageManager.Bun, "bunx", "vite build")]
        public void Build_WithDifferentPackageManagers_UsesCorrectExecutable(
            PackageManager packageManager, string expectedExecutable, string expectedCommand)
        {
            // Arrange
            var builder = new DirectToolCommandBuilder(packageManager);

            // Act
            var command = builder.Build();

            // Assert
            command.Executable.Should().Contain(expectedExecutable);
            command.Command.Should().Contain(expectedCommand);
            command.ToString().Should().Contain($"{expectedExecutable} {expectedCommand}");
        }

        [Fact]
        public void Build_ForNpm_UsesNpx()
        {
            // Arrange
            var builder = new DirectToolCommandBuilder(PackageManager.Npm);

            // Act
            var command = builder.Build();

            // Assert
            command.Executable.Should().Be("npx");
            command.Command.Should().Be("vite build");
        }

        [Fact]
        public void Build_ForYarn_UsesYarnDlx()
        {
            // Arrange
            var builder = new DirectToolCommandBuilder(PackageManager.Yarn);

            // Act
            var command = builder.Build();

            // Assert
            command.Executable.Should().Be("yarn dlx");
            command.Command.Should().Be("vite build");
        }

        [Fact]
        public void Build_ForPnpm_UsesPnpmDlx()
        {
            // Arrange
            var builder = new DirectToolCommandBuilder(PackageManager.Pnpm);

            // Act
            var command = builder.Build();

            // Assert
            command.Executable.Should().Be("pnpm dlx");
            command.Command.Should().Be("vite build");
        }

        [Fact]
        public void Build_ForBun_UsesBunx()
        {
            // Arrange
            var builder = new DirectToolCommandBuilder(PackageManager.Bun);

            // Act
            var command = builder.Build();

            // Assert
            command.Executable.Should().Be("bunx");
            command.Command.Should().Be("vite build");
        }

        [Fact]
        public void Build_WithMode_IncludesModeFlag()
        {
            // Arrange
            var builder = new DirectToolCommandBuilder(PackageManager.Npm)
                .WithMode("production");

            // Act
            var command = builder.Build();

            // Assert
            command.Mode.Should().Be("production");
            command.ToString().Should().Contain("--mode production");
        }

        [Theory]
        [InlineData("development")]
        [InlineData("production")]
        [InlineData("staging")]
        [InlineData("test")]
        public void Build_WithDifferentModes_IncludesCorrectModeFlag(string mode)
        {
            // Arrange
            var builder = new DirectToolCommandBuilder(PackageManager.Npm)
                .WithMode(mode);

            // Act
            var command = builder.Build();

            // Assert
            command.Mode.Should().Be(mode);
            command.ToString().Should().Contain($"--mode {mode}");
        }

        [Fact]
        public void Build_WithConfigPath_IncludesConfigFlag()
        {
            // Arrange
            var builder = new DirectToolCommandBuilder(PackageManager.Npm)
                .WithConfigPath("vite.config.ts");

            // Act
            var command = builder.Build();

            // Assert
            command.ConfigPath.Should().Be("vite.config.ts");
            command.ToString().Should().Contain("--config \"vite.config.ts\"");
        }

        [Fact]
        public void Build_WithOutputDir_IncludesOutDirFlag()
        {
            // Arrange
            var builder = new DirectToolCommandBuilder(PackageManager.Npm)
                .WithOutputDir("wwwroot/dist");

            // Act
            var command = builder.Build();

            // Assert
            command.OutputDir.Should().Be("wwwroot/dist");
            command.ToString().Should().Contain("--outDir \"wwwroot/dist\"");
        }

        [Theory]
        [InlineData("silent")]
        [InlineData("error")]
        [InlineData("warn")]
        [InlineData("info")]
        public void Build_WithLogLevel_IncludesLogLevelFlag(string logLevel)
        {
            // Arrange
            var builder = new DirectToolCommandBuilder(PackageManager.Npm)
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
            var builder = new DirectToolCommandBuilder(PackageManager.Npm)
                .WithColors(true);

            // Act
            var command = builder.Build();

            // Assert
            command.Colors.Should().BeTrue();
            command.Environment.Should().ContainKey("FORCE_COLOR");
            command.Environment["FORCE_COLOR"].Should().Be("1");
            
            // Should NOT use --color flag
            command.ToString().Should().NotContain("--color");
        }

        [Fact]
        public void Build_WithColorsDisabled_SetsNoColorEnvironmentVariable()
        {
            // Arrange
            var builder = new DirectToolCommandBuilder(PackageManager.Npm)
                .WithColors(false);

            // Act
            var command = builder.Build();

            // Assert
            command.Colors.Should().BeFalse();
            command.Environment.Should().ContainKey("NO_COLOR");
            command.Environment["NO_COLOR"].Should().Be("1");
            
            // Should NOT use --no-color flag
            command.ToString().Should().NotContain("--no-color");
        }

        [Fact]
        public void Build_WithEnvironmentVariables_IncludesAll()
        {
            // Arrange
            var env = new Dictionary<string, string>
            {
                ["NODE_ENV"] = "production",
                ["VITE_API_URL"] = "https://api.example.com"
            };
            
            var builder = new DirectToolCommandBuilder(PackageManager.Npm)
                .WithEnvironment(env);

            // Act
            var command = builder.Build();

            // Assert
            command.Environment.Should().ContainKey("NODE_ENV");
            command.Environment.Should().ContainKey("VITE_API_URL");
            command.Environment["NODE_ENV"].Should().Be("production");
            command.Environment["VITE_API_URL"].Should().Be("https://api.example.com");
        }

        [Fact]
        public void Build_WithWorkingDirectory_SetsWorkingDirectory()
        {
            // Arrange
            var builder = new DirectToolCommandBuilder(PackageManager.Npm)
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
            var env = new Dictionary<string, string> { ["NODE_ENV"] = "production" };
            var builder = new DirectToolCommandBuilder(PackageManager.Npm)
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
            command.Executable.Should().Be("npx");
            command.Command.Should().Contain("vite build");
            command.ConfigPath.Should().Be("vite.config.ts");
            command.Mode.Should().Be("production");
            command.OutputDir.Should().Be("dist");
            command.LogLevel.Should().Be("info");
            command.Colors.Should().BeTrue();
            command.WorkingDirectory.Should().Be("/project");
            command.Environment.Should().ContainKeys("NODE_ENV", "FORCE_COLOR");
            
            var commandString = command.ToString();
            commandString.Should().Contain("npx vite build");
            commandString.Should().Contain("--config \"vite.config.ts\"");
            commandString.Should().Contain("--mode production");
            commandString.Should().Contain("--outDir \"dist\"");
            commandString.Should().Contain("--logLevel info");
        }

        [Fact]
        public void Build_WithPathContainingSpaces_QuotesPaths()
        {
            // Arrange
            var builder = new DirectToolCommandBuilder(PackageManager.Npm)
                .WithConfigPath("path with spaces/vite.config.ts")
                .WithOutputDir("dist with spaces");

            // Act
            var command = builder.Build();

            // Assert
            command.ToString().Should().Contain("\"path with spaces/vite.config.ts\"");
            command.ToString().Should().Contain("\"dist with spaces\"");
        }

        [Fact]
        public void Build_CalledMultipleTimes_GeneratesConsistentCommands()
        {
            // Arrange
            var builder = new DirectToolCommandBuilder(PackageManager.Npm)
                .WithMode("production");

            // Act
            var command1 = builder.Build();
            var command2 = builder.Build();

            // Assert
            command1.ToString().Should().Be(command2.ToString());
        }

        [Fact]
        public void WithCustomCommand_IgnoredForDirectToolBuilder()
        {
            // Arrange
            var builder = new DirectToolCommandBuilder(PackageManager.Npm)
                .WithCustomCommand("ignored-command");

            // Act
            var command = builder.Build();

            // Assert
            // Custom command should be ignored for direct tool builders
            command.Executable.Should().Be("npx");
            command.Command.Should().Contain("vite build");
        }

        [Theory]
        [InlineData(PackageManager.Npm, "npx vite build --mode production")]
        [InlineData(PackageManager.Yarn, "yarn dlx vite build --mode production")]
        [InlineData(PackageManager.Pnpm, "pnpm dlx vite build --mode production")]
        [InlineData(PackageManager.Bun, "bunx vite build --mode production")]
        public void Build_AcrossPackageManagers_WithMode_GeneratesCorrectSyntax(
            PackageManager packageManager, string expectedSubstring)
        {
            // Arrange
            var builder = new DirectToolCommandBuilder(packageManager)
                .WithMode("production");

            // Act
            var command = builder.Build();

            // Assert
            command.ToString().Should().Contain(expectedSubstring);
        }

        [Fact]
        public void Build_WithNullMode_OmitsModeFlag()
        {
            // Arrange
            var builder = new DirectToolCommandBuilder(PackageManager.Npm)
                .WithMode(null);

            // Act
            var command = builder.Build();

            // Assert
            command.Mode.Should().BeNull();
            command.ToString().Should().NotContain("--mode");
        }

        [Fact]
        public void Build_WithEmptyMode_OmitsModeFlag()
        {
            // Arrange
            var builder = new DirectToolCommandBuilder(PackageManager.Npm)
                .WithMode("");

            // Act
            var command = builder.Build();

            // Assert
            command.ToString().Should().NotContain("--mode");
        }

        [Fact]
        public void FluentInterface_AllowsMethodChaining()
        {
            // Arrange & Act
            var builder = new DirectToolCommandBuilder(PackageManager.Npm);
            
            var result = builder
                .WithMode("production")
                .WithConfigPath("vite.config.ts")
                .WithOutputDir("dist")
                .WithLogLevel("warn")
                .WithColors(false);

            // Assert
            result.Should().BeSameAs(builder);
            
            var command = result.Build();
            command.Should().NotBeNull();
        }

        [Fact]
        public void Build_WithSpecialCharactersInConfig_HandlesCorrectly()
        {
            // Arrange
            var builder = new DirectToolCommandBuilder(PackageManager.Npm)
                .WithConfigPath("configs/@company/vite.config.ts");

            // Act
            var command = builder.Build();

            // Assert
            command.ConfigPath.Should().Be("configs/@company/vite.config.ts");
        }
    }
}

