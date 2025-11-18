using Xunit;
using FluentAssertions;
using Vite.MsBuild.Tasks;
using System;
using System.Collections.Generic;

namespace Vite.MsBuild.PureUnitTests.Factories
{
    /// <summary>
    /// Comprehensive tests for ViteCommandFactory - the core factory implementation
    /// Tests all factory methods and builder creation scenarios
    /// </summary>
    public class ViteCommandFactoryComprehensiveTests
    {
        [Fact]
        public void CreateBuilder_WithScriptBasedType_ReturnsScriptBasedBuilder()
        {
            // Arrange
            var factory = new ViteCommandFactory();

            // Act
            var builder = factory.CreateBuilder(ViteCommandType.ScriptBased, PackageManager.Npm);

            // Assert
            builder.Should().NotBeNull();
            builder.Should().BeAssignableTo<IViteCommandBuilder>();
            
            // Verify it creates script-based commands
            var command = builder.Build();
            command.ToString().Should().Contain("npm run");
        }

        [Fact]
        public void CreateBuilder_WithDirectToolType_ReturnsDirectToolBuilder()
        {
            // Arrange
            var factory = new ViteCommandFactory();

            // Act
            var builder = factory.CreateBuilder(ViteCommandType.DirectTool, PackageManager.Npm);

            // Assert
            builder.Should().NotBeNull();
            builder.Should().BeAssignableTo<IViteCommandBuilder>();
            
            // Verify it creates direct tool commands
            var command = builder.Build();
            command.ToString().Should().Contain("npx vite");
        }

        [Fact]
        public void CreateBuilder_WithCustomCommandType_ReturnsCustomCommandBuilder()
        {
            // Arrange
            var factory = new ViteCommandFactory();

            // Act
            var builder = factory.CreateBuilder(ViteCommandType.CustomCommand, PackageManager.Npm);

            // Assert
            builder.Should().NotBeNull();
            builder.Should().BeAssignableTo<IViteCommandBuilder>();
        }

        [Theory]
        [InlineData(PackageManager.Npm)]
        [InlineData(PackageManager.Yarn)]
        [InlineData(PackageManager.Pnpm)]
        [InlineData(PackageManager.Bun)]
        public void CreateBuilder_WithAllPackageManagers_CreatesValidBuilders(PackageManager packageManager)
        {
            // Arrange
            var factory = new ViteCommandFactory();

            // Act
            var scriptBuilder = factory.CreateBuilder(ViteCommandType.ScriptBased, packageManager);
            var directBuilder = factory.CreateBuilder(ViteCommandType.DirectTool, packageManager);
            var customBuilder = factory.CreateBuilder(ViteCommandType.CustomCommand, packageManager)
                .WithCustomCommand("test-command");

            // Assert
            scriptBuilder.Should().NotBeNull();
            directBuilder.Should().NotBeNull();
            customBuilder.Should().NotBeNull();
            
            // All should build valid commands
            scriptBuilder.Build().Should().NotBeNull();
            directBuilder.Build().Should().NotBeNull();
            customBuilder.Build().Should().NotBeNull();
        }

        [Fact]
        public void CreateBuilder_WithInvalidCommandType_ThrowsArgumentException()
        {
            // Arrange
            var factory = new ViteCommandFactory();
            var invalidType = (ViteCommandType)999;

            // Act
            Action act = () => factory.CreateBuilder(invalidType, PackageManager.Npm);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Unsupported command type*");
        }

        [Fact]
        public void CreateScriptBuilder_WithDefaultScriptName_CreatesBuilderWithBuildScript()
        {
            // Arrange
            var factory = new ViteCommandFactory();

            // Act
            var builder = factory.CreateScriptBuilder(PackageManager.Npm, "build");
            var command = builder.Build();

            // Assert
            command.Should().NotBeNull();
            command.ToString().Should().Contain("npm run build");
        }

        [Theory]
        [InlineData("dev", "npm run dev")]
        [InlineData("start", "npm run start")]
        [InlineData("custom-script", "npm run custom-script")]
        [InlineData("build:production", "npm run build:production")]
        public void CreateScriptBuilder_WithCustomScriptName_CreatesCorrectCommand(string scriptName, string expectedCommand)
        {
            // Arrange
            var factory = new ViteCommandFactory();

            // Act
            var builder = factory.CreateScriptBuilder(PackageManager.Npm, scriptName);
            var command = builder.Build();

            // Assert
            command.ToString().Should().Contain(expectedCommand);
        }

        [Theory]
        [InlineData(PackageManager.Npm, "npm run custom")]
        [InlineData(PackageManager.Yarn, "yarn custom")]  // Yarn runs scripts directly
        [InlineData(PackageManager.Pnpm, "pnpm run custom")]
        [InlineData(PackageManager.Bun, "bun run custom")]
        public void CreateScriptBuilder_WithDifferentPackageManagers_UsesCorrectSyntax(
            PackageManager packageManager, string expectedCommand)
        {
            // Arrange
            var factory = new ViteCommandFactory();

            // Act
            var builder = factory.CreateScriptBuilder(packageManager, "custom");
            var command = builder.Build();

            // Assert
            command.ToString().Should().Contain(expectedCommand);
        }

        [Fact]
        public void CreateDirectToolBuilder_CreatesBuilderWithDirectViteExecution()
        {
            // Arrange
            var factory = new ViteCommandFactory();

            // Act
            var builder = factory.CreateDirectToolBuilder(PackageManager.Npm);
            var command = builder.Build();

            // Assert
            command.ToString().Should().Contain("npx vite build");
        }

        [Theory]
        [InlineData(PackageManager.Npm, "npx", "vite build")]
        [InlineData(PackageManager.Yarn, "yarn dlx", "vite build")]
        [InlineData(PackageManager.Pnpm, "pnpm dlx", "vite build")]
        [InlineData(PackageManager.Bun, "bunx", "vite build")]
        public void CreateDirectToolBuilder_WithDifferentPackageManagers_UsesCorrectExecutable(
            PackageManager packageManager, string expectedExecutable, string expectedCommand)
        {
            // Arrange
            var factory = new ViteCommandFactory();

            // Act
            var builder = factory.CreateDirectToolBuilder(packageManager);
            var command = builder.Build();

            // Assert
            command.Executable.Should().Contain(expectedExecutable);
            command.Command.Should().Contain(expectedCommand);
        }

        [Fact]
        public void CreateCustomBuilder_WithSimpleCommand_ParsesCorrectly()
        {
            // Arrange
            var factory = new ViteCommandFactory();

            // Act
            var builder = factory.CreateCustomBuilder(PackageManager.Npm, "my-tool");
            var command = builder.Build();

            // Assert
            command.Executable.Should().Be("my-tool");
            command.Command.Should().Be("");
        }

        [Theory]
        [InlineData("yarn build:production", "yarn", "build:production")]
        [InlineData("npm run build -- --watch", "npm", "run build -- --watch")]
        [InlineData("pnpm exec turbo build", "pnpm", "exec turbo build")]
        [InlineData("bunx --bun vite build", "bunx", "--bun vite build")]
        public void CreateCustomBuilder_WithComplexCommand_ParsesExecutableAndArguments(
            string customCommand, string expectedExecutable, string expectedCommand)
        {
            // Arrange
            var factory = new ViteCommandFactory();

            // Act
            var builder = factory.CreateCustomBuilder(PackageManager.Npm, customCommand);
            var command = builder.Build();

            // Assert
            command.Executable.Should().Be(expectedExecutable);
            command.Command.Should().Be(expectedCommand);
        }

        [Fact]
        public void CreateCustomBuilder_WithNullCommand_CreatesBuilderButThrowsOnBuild()
        {
            // Arrange
            var factory = new ViteCommandFactory();

            // Act - Factory accepts null but Build() should throw
            var builder = factory.CreateCustomBuilder(PackageManager.Npm, null!);
            Action act = () => builder.Build();

            // Assert
            builder.Should().NotBeNull();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*CustomCommand must be specified*");
        }

        [Fact]
        public void CreateCustomBuilder_WithEmptyCommand_CreatesBuilderButThrowsOnBuild()
        {
            // Arrange
            var factory = new ViteCommandFactory();

            // Act - Factory accepts empty string but Build() should throw
            var builder = factory.CreateCustomBuilder(PackageManager.Npm, "");
            Action act = () => builder.Build();

            // Assert
            builder.Should().NotBeNull();
            act.Should().Throw<InvalidOperationException>
                ().WithMessage("*CustomCommand must be specified*");
        }

        [Fact]
        public void CreateCustomBuilder_WithWhitespaceCommand_CreatesBuilderButThrowsOnBuild()
        {
            // Arrange
            var factory = new ViteCommandFactory();

            // Act - Factory accepts whitespace but Build() should throw
            var builder = factory.CreateCustomBuilder(PackageManager.Npm, "   ");
            Action act = () => builder.Build();

            // Assert
            builder.Should().NotBeNull();
            act.Should().Throw<ArgumentException>
                ().WithMessage("*Custom command cannot be empty*");
        }

        [Fact]
        public void Factory_CreatesIndependentBuilders_ThatDontShareState()
        {
            // Arrange
            var factory = new ViteCommandFactory();

            // Act
            var builder1 = factory.CreateBuilder(ViteCommandType.ScriptBased, PackageManager.Npm)
                .WithMode("development");
            
            var builder2 = factory.CreateBuilder(ViteCommandType.ScriptBased, PackageManager.Npm)
                .WithMode("production");

            var command1 = builder1.Build();
            var command2 = builder2.Build();

            // Assert
            command1.Mode.Should().Be("development");
            command2.Mode.Should().Be("production");
            command1.ToString().Should().NotBe(command2.ToString());
        }

        [Fact]
        public void Factory_CanCreateMultipleBuildersOfSameType_WithDifferentPackageManagers()
        {
            // Arrange
            var factory = new ViteCommandFactory();

            // Act
            var npmBuilder = factory.CreateDirectToolBuilder(PackageManager.Npm);
            var yarnBuilder = factory.CreateDirectToolBuilder(PackageManager.Yarn);
            var pnpmBuilder = factory.CreateDirectToolBuilder(PackageManager.Pnpm);
            var bunBuilder = factory.CreateDirectToolBuilder(PackageManager.Bun);

            // Assert
            npmBuilder.Build().ToString().Should().Contain("npx");
            yarnBuilder.Build().ToString().Should().Contain("yarn dlx");
            pnpmBuilder.Build().ToString().Should().Contain("pnpm dlx");
            bunBuilder.Build().ToString().Should().Contain("bunx");
        }

        [Fact]
        public async System.Threading.Tasks.Task Factory_Methods_AreThreadSafe()
        {
            // Arrange
            var factory = new ViteCommandFactory();
            var results = new List<IViteCommand>();
            var tasks = new List<System.Threading.Tasks.Task>();

            // Act - Create builders from multiple threads
            for (int i = 0; i < 10; i++)
            {
                var task = System.Threading.Tasks.Task.Run(() =>
                {
                    var builder = factory.CreateBuilder(ViteCommandType.DirectTool, PackageManager.Npm);
                    var command = builder.Build();
                    lock (results)
                    {
                        results.Add(command);
                    }
                });
                tasks.Add(task);
            }

            await System.Threading.Tasks.Task.WhenAll(tasks);

            // Assert
            results.Should().HaveCount(10);
            results.Should().OnlyContain(cmd => cmd != null);
        }
    }
}
