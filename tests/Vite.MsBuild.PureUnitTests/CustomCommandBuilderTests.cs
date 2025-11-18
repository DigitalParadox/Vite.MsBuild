using System;
using System.IO;
using Xunit;
using FluentAssertions;
using Vite.MsBuild.Tasks;

namespace Vite.MsBuild.PureUnitTests
{
    /// <summary>
    /// Tests for CustomCommandBuilder to validate literal command execution
    /// </summary>
    public class CustomCommandBuilderTests : IDisposable
    {
        private string _tempDirectory;

        public CustomCommandBuilderTests()
        {
            _tempDirectory = CreateTempDirectory();
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        [Fact]
        public void CustomCommandBuilder_Should_Use_User_Command_As_Is()
        {
            // Arrange: User provides complete command
            var command = new ViteCommandBuilder(_tempDirectory, PackageManager.Npm)
                .WithCustomCommand("yarn build:enterprise --mode production")
                .BuildCommand();

            // Assert: Command should be executed exactly as user specified
            command.Executable.Should().Be("yarn");
            command.Command.Should().Be("build:enterprise --mode production");
        }

        [Fact]
        public void CustomCommandBuilder_Should_Handle_Simple_Executable()
        {
            // Arrange: User provides just executable name
            var command = new ViteCommandBuilder(_tempDirectory, PackageManager.Npm)
                .WithCustomCommand("vite")
                .BuildCommand();

            // Assert: Executable only, no arguments
            command.Executable.Should().Be("vite");
            command.Command.Should().Be("");
        }

        [Fact]
        public void CustomCommandBuilder_Should_Handle_Complex_Commands()
        {
            // Arrange: User provides complex command with multiple arguments
            var command = new ViteCommandBuilder(_tempDirectory, PackageManager.Npm)
                .WithCustomCommand("pnpm dlx vite build --config custom.config.ts --mode staging")
                .BuildCommand();

            // Assert: First part is executable, rest are arguments
            command.Executable.Should().Be("pnpm");
            command.Command.Should().Be("dlx vite build --config custom.config.ts --mode staging");
        }

        [Fact]
        public void CustomCommandBuilder_Should_Throw_For_Empty_Command()
        {
            // Arrange: Empty custom command
            var builder = new ViteCommandBuilder(_tempDirectory, PackageManager.Npm)
                .WithCustomCommand("   "); // Use whitespace instead of empty string

            // Assert: Should throw for empty/whitespace command
            var exception = Assert.Throws<ArgumentException>(() => builder.BuildCommand());
            exception.Message.Should().Contain("Custom command cannot be empty");
        }

        private string CreateTempDirectory()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "ViteMsBuildTest_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tempPath);
            return tempPath;
        }
    }
}