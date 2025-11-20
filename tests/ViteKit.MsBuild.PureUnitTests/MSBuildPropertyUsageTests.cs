using System;
using System.IO;
using Xunit;
using FluentAssertions;
using ViteKit.MsBuild.Tasks;

namespace Vite.MsBuild.PureUnitTests
{
    /// <summary>
    /// Tests for MSBuild property usage in CustomCommand and Config paths
    /// </summary>
    public class MSBuildPropertyUsageTests : IDisposable
    {
        private string _tempDirectory;

        public MSBuildPropertyUsageTests()
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
        public void Should_Support_MSBuild_Properties_In_Custom_Commands()
        {
            // Arrange: Simulate MSBuild property expansion
            var solutionRoot = @"C:\MyProject";
            var buildScript = $"{solutionRoot}/scripts/build.sh";
            
            // Act: Use "expanded" properties (as MSBuild would provide them)
            var config = new ViteBuildConfiguration
            {
                ProjectRoot = _tempDirectory,
                PackageManager = PackageManager.Npm,
                CustomBuildCommand = buildScript
            };
            var command = new ViteCommandBuilder(config).BuildCommand();

            // Assert: Command uses the full expanded path
            command.Executable.Should().Be($"{solutionRoot}/scripts/build.sh");
            command.Command.Should().Be("");
        }

        [Fact]
        public void Should_Support_Properties_In_Config_Paths()
        {
            // Arrange: Simulate MSBuild property expansion in config paths
            var solutionRoot = @"C:\MyProject";
            var configPath = $"{solutionRoot}/frontend/vite.config.ts";
            
            // Act: Use expanded config path
            var config = new ViteBuildConfiguration
            {
                ProjectRoot = _tempDirectory,
                PackageManager = PackageManager.Npm,
                ConfigFile = configPath
            };
            var command = new ViteCommandBuilder(config).BuildCommand();

            // Assert: Config path is preserved as provided
            command.ConfigPath.Should().Be(configPath);
        }

        [Fact]
        public void Should_Support_Complex_Property_Combinations()
        {
            // Arrange: Complex command with multiple property expansions
            var solutionRoot = @"C:\MyProject";
            var configuration = "Release";
            var complexCommand = $"powershell -File \"{solutionRoot}/scripts/build-{configuration}.ps1\" --target \"{solutionRoot}/src/ClientApp\"";
            
            // Act: Use complex expanded command
            var config = new ViteBuildConfiguration
            {
                ProjectRoot = _tempDirectory,
                PackageManager = PackageManager.Npm,
                CustomBuildCommand = complexCommand
            };
            var command = new ViteCommandBuilder(config).BuildCommand();

            // Assert: Command uses full expanded paths
            command.Executable.Should().Be("powershell");
            command.Command.Should().Be($"-File \"{solutionRoot}/scripts/build-{configuration}.ps1\" --target \"{solutionRoot}/src/ClientApp\"");
        }

        [Fact]
        public void Should_Support_Environment_Specific_Paths()
        {
            // Arrange: Different scripts for different environments
            var solutionRoot = @"C:\MyProject";
            var devScript = $"{solutionRoot}/scripts/dev-build.sh";
            var prodScript = $"{solutionRoot}/scripts/prod-build.sh";
            
            // Act: Test both environment scenarios
            var devConfig = new ViteBuildConfiguration
            {
                ProjectRoot = _tempDirectory,
                PackageManager = PackageManager.Npm,
                CustomBuildCommand = devScript
            };
            var devCommand = new ViteCommandBuilder(devConfig).BuildCommand();
                
            var prodConfig = new ViteBuildConfiguration
            {
                ProjectRoot = _tempDirectory,
                PackageManager = PackageManager.Npm,
                CustomBuildCommand = prodScript
            };
            var prodCommand = new ViteCommandBuilder(prodConfig).BuildCommand();

            // Assert: Both commands work with different paths
            devCommand.Executable.Should().Be(devScript);
            prodCommand.Executable.Should().Be(prodScript);
        }

        [Fact]
        public void Should_Handle_Quoted_Paths_With_Spaces()
        {
            // Arrange: Paths with spaces (common in Windows)
            var pathWithSpaces = @"C:\My Project\scripts\build script.ps1";
            
            // For paths with spaces, the ENTIRE first argument must be quoted
            var quotedCommand = $"powershell -File \"{pathWithSpaces}\" --mode production";
            
            // Act: Use properly quoted command
            var config = new ViteBuildConfiguration
            {
                ProjectRoot = _tempDirectory,
                PackageManager = PackageManager.Npm,
                CustomBuildCommand = quotedCommand
            };
            var command = new ViteCommandBuilder(config).BuildCommand();

            // Assert: PowerShell handles the quoted path as an argument
            command.Executable.Should().Be("powershell");
            command.Command.Should().Be($"-File \"{pathWithSpaces}\" --mode production");
        }

        private string CreateTempDirectory()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "ViteMsBuildTest_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tempPath);
            return tempPath;
        }
    }
}