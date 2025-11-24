using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Build.Framework;
using ViteKit.MsBuild.Tasks;
using Xunit;

namespace ViteKit.Msbuild.Tasks.Tests
{
    public class ViteCommandBuilderTests : IDisposable
    {
        private readonly string _tempDir;

        public ViteCommandBuilderTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"ViteCommandBuilderTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
        }

        [Fact]
        public void Should_Build_Custom_Command_When_Provided()
        {
            // Arrange
            var config = new ViteBuildConfiguration
            {
                ProjectRoot = _tempDir,
                PackageManager = PackageManager.Npm,
                CustomBuildCommand = "yarn build:custom",
                Mode = "production"
            };
            var builder = new ViteCommandBuilder(config);

            // Act
            var command = builder.BuildCommand();

            // Assert
            command.Executable.Should().Be("yarn");
            command.Command.Should().Be("build:custom");
            command.Mode.Should().Be("production");
            command.ToString().Should().StartWith("yarn build:custom");
        }

        [Fact]
        public void Should_Use_Package_Script_When_Available()
        {
            // Arrange
            CreatePackageJson(new Dictionary<string, string> { ["build"] = "vite build" });
            var config = new ViteBuildConfiguration
            {
                ProjectRoot = _tempDir,
                PackageManager = PackageManager.Npm,
                Mode = "development"
            };
            var builder = new ViteCommandBuilder(config);

            // Act
            var command = builder.BuildCommand();

            // Assert
            command.Executable.Should().Be("npm");
            command.Command.Should().Be("run build");
            command.Mode.Should().Be("development");
            command.ToString().Should().Contain("npm run build");
            command.ToString().Should().Contain("--mode development");
        }

        [Fact]
        public void Should_Fallback_To_Direct_Command_When_No_Script()
        {
            // Arrange
            CreatePackageJson(new Dictionary<string, string> { ["dev"] = "vite" }); // No build script
            var config = new ViteBuildConfiguration
            {
                ProjectRoot = _tempDir,
                PackageManager = PackageManager.Npm,
                Mode = "production"
            };
            var builder = new ViteCommandBuilder(config);

            // Act
            var command = builder.BuildCommand();

            // Assert
            command.Executable.Should().Be("npx");
            command.Command.Should().Be("vite build");
            command.ToString().Should().StartWith("npx vite build");
        }

        [Theory]
        [InlineData(PackageManager.Npm, "npm", "run build")]
        [InlineData(PackageManager.Yarn, "yarn", "build")]  // Yarn can run scripts directly
        [InlineData(PackageManager.Pnpm, "pnpm", "run build")]
        [InlineData(PackageManager.Bun, "bun", "run build")]
        public void Should_Use_Correct_Package_Manager_For_Scripts(
            PackageManager packageManager, string expectedExecutable, string expectedCommand)
        {
            // Arrange
            CreatePackageJson(new Dictionary<string, string> { ["build"] = "vite build" });
            var config = new ViteBuildConfiguration
            {
                ProjectRoot = _tempDir,
                PackageManager = packageManager
            };
            var builder = new ViteCommandBuilder(config);

            // Act
            var command = builder.BuildCommand();

            // Assert
            command.Executable.Should().Be(expectedExecutable);
            command.Command.Should().Be(expectedCommand);
        }

        [Theory]
        [InlineData(PackageManager.Npm, "npx", "vite build")]
        [InlineData(PackageManager.Yarn, "yarn dlx", "vite build")]
        [InlineData(PackageManager.Pnpm, "pnpm dlx", "vite build")]
        [InlineData(PackageManager.Bun, "bunx", "vite build")]
        public void Should_Use_Correct_Direct_Executable_For_Fallback(
            PackageManager packageManager, string expectedExecutable, string expectedCommand)
        {
            // Arrange (no package.json)
            var config = new ViteBuildConfiguration
            {
                ProjectRoot = _tempDir,
                PackageManager = packageManager
            };
            var builder = new ViteCommandBuilder(config);

            // Act
            var command = builder.BuildCommand();

            // Assert
            command.Executable.Should().Be(expectedExecutable);
            command.Command.Should().Be(expectedCommand);
        }

        [Fact]
        public void Should_Build_Complete_Command_With_All_Options()
        {
            // Arrange
            var config = new ViteBuildConfiguration
            {
                ProjectRoot = _tempDir,
                PackageManager = PackageManager.Npm,
                ConfigFile = "vite.config.ts",
                Mode = "staging",
                OutputDir = "dist/staging",
                LogLevel = "info",
                EnableColors = false
            };
            var builder = new ViteCommandBuilder(config);

            // Act
            var command = builder.BuildCommand();

            // Assert
            var commandString = command.ToString();
            commandString.Should().Contain("--config \"vite.config.ts\"");
            commandString.Should().Contain("--mode staging");
            commandString.Should().Contain("--outDir \"dist/staging\"");
            commandString.Should().Contain("--logLevel info");
            
            // Color is controlled via environment variables, not CLI flags
            command.Environment.Should().ContainKey("NO_COLOR");
            command.Environment["NO_COLOR"].Should().Be("1");
        }

        [Fact]
        public void Should_Handle_Yarn_Berry_Correctly()
        {
            // Arrange
            CreatePackageJson(new Dictionary<string, string> { ["build"] = "vite build" });
            File.WriteAllText(Path.Combine(_tempDir, ".yarnrc.yml"), "yarnPath: .yarn/releases/yarn-3.0.0.cjs");
            
            var config = new ViteBuildConfiguration
            {
                ProjectRoot = _tempDir,
                PackageManager = PackageManager.Yarn
            };
            var builder = new ViteCommandBuilder(config);

            // Act
            var command = builder.BuildCommand();

            // Assert
            command.Executable.Should().Be("yarn");
            command.Command.Should().Be("build"); // Yarn Berry doesn't need "run"
        }

        [Fact]
        public void Should_Set_Environment_Variables()
        {
            // Arrange
            var config = new ViteBuildConfiguration
            {
                ProjectRoot = _tempDir,
                PackageManager = PackageManager.Npm
            };
            config.Environment["NODE_ENV"] = "production";
            config.Environment["VITE_API_URL"] = "https://api.example.com";
            var builder = new ViteCommandBuilder(config);

            // Act
            var command = builder.BuildCommand();

            // Assert
            command.Environment.Should().ContainKey("NODE_ENV").WhoseValue.Should().Be("production");
            command.Environment.Should().ContainKey("VITE_API_URL").WhoseValue.Should().Be("https://api.example.com");
            command.WorkingDirectory.Should().Be(_tempDir);
        }

        [Fact]
        public void Should_Handle_Invalid_Package_Json_Gracefully()
        {
            // Arrange
            File.WriteAllText(Path.Combine(_tempDir, "package.json"), "{ invalid json }");
            var config = new ViteBuildConfiguration
            {
                ProjectRoot = _tempDir,
                PackageManager = PackageManager.Npm
            };
            var builder = new ViteCommandBuilder(config);

            // Act
            var command = builder.BuildCommand();

            // Assert - Should fall back to direct command
            command.Executable.Should().Be("npx");
            command.Command.Should().Be("vite build");
        }

        private void CreatePackageJson(Dictionary<string, string>? scripts = null)
        {
            var packageJson = new
            {
                name = "test-project",
                scripts = scripts ?? new Dictionary<string, string>()
            };

            var json = JsonSerializer.Serialize(packageJson, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(_tempDir, "package.json"), json);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
    }

    public class BuildViteCommandTaskTests
    {
        [Fact]
        public void Should_Build_Command_Without_Execution_In_Validate_Mode()
        {
            // Arrange
            var task = new BuildViteCommand
            {
                BuildEngine = new MockBuildEngine(),
                ProjectRoot = Directory.GetCurrentDirectory(),
                PackageManagerName = "npm",
                Mode = "production",
                ValidateOnly = true
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.CommandLine.Should().NotBeNullOrEmpty();
            task.CommandLine.Should().Contain("--mode production");
            task.ExecutedSuccessfully.Should().BeTrue();
            task.ExitCode.Should().Be(0);
        }

        [Fact]
        public void Should_Handle_Custom_Command_Override()
        {
            // Arrange
            var task = new BuildViteCommand
            {
                BuildEngine = new MockBuildEngine(),
                ProjectRoot = Directory.GetCurrentDirectory(),
                PackageManagerName = "npm",
                CustomCommand = "yarn build:custom",
                ValidateOnly = true
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.CommandLine.Should().StartWith("yarn build:custom");
        }

        [Fact]
        public void Should_Handle_Environment_Variables()
        {
            // Arrange - Environment variables as semicolon-separated string (MSBuild format)
            var environmentString = "NODE_ENV=production;VITE_API_URL=https://api.example.com";

            var task = new BuildViteCommand
            {
                BuildEngine = new MockBuildEngine(),
                ProjectRoot = Directory.GetCurrentDirectory(),
                PackageManagerName = "npm",
                EnvironmentVariables = environmentString,
                ValidateOnly = true
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            // Note: Environment variables are set during execution, not visible in command line
        }

        [Fact]
        public void Should_Fail_With_Invalid_Package_Manager()
        {
            // Arrange
            var task = new BuildViteCommand
            {
                BuildEngine = new MockBuildEngine(),
                ProjectRoot = Directory.GetCurrentDirectory(),
                PackageManagerName = "invalid",
                ValidateOnly = true
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeFalse();
        }
    }

    // Mock implementation for testing
    internal class MockBuildEngine : IBuildEngine
    {
        public bool ContinueOnError { get; set; }
        public int LineNumberOfTaskNode { get; set; }
        public int ColumnNumberOfTaskNode { get; set; }
        public string ProjectFileOfTaskNode { get; set; } = string.Empty;

        public bool BuildProjectFile(string projectFileName, string[] targetNames, 
            System.Collections.IDictionary globalProperties, 
            System.Collections.IDictionary targetOutputs) => true;

        public void LogCustomEvent(CustomBuildEventArgs e) { }
        public void LogErrorEvent(BuildErrorEventArgs e) { }
        public void LogMessageEvent(BuildMessageEventArgs e) { }
        public void LogWarningEvent(BuildWarningEventArgs e) { }
    }

    internal class TaskItem : ITaskItem
    {
        private readonly Dictionary<string, string> _metadata = new();

        public TaskItem(string itemSpec)
        {
            ItemSpec = itemSpec;
        }

        public string ItemSpec { get; set; }

        public System.Collections.ICollection MetadataNames => _metadata.Keys;
        public int MetadataCount => _metadata.Count;

        public string GetMetadata(string metadataName) => _metadata.GetValueOrDefault(metadataName, string.Empty);

        public void SetMetadata(string metadataName, string metadataValue) => _metadata[metadataName] = metadataValue;

        public void RemoveMetadata(string metadataName) => _metadata.Remove(metadataName);

        public void CopyMetadataTo(ITaskItem destinationItem)
        {
            foreach (var kvp in _metadata)
            {
                destinationItem.SetMetadata(kvp.Key, kvp.Value);
            }
        }

        public System.Collections.IDictionary CloneCustomMetadata() => new Dictionary<string, string>(_metadata);

        public void Add(string metadataName, string metadataValue) => SetMetadata(metadataName, metadataValue);
    }
}
