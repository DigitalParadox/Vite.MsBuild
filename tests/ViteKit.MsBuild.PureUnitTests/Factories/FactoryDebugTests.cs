using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using ViteKit.MsBuild.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ViteKit.MsBuild.PureUnitTests.Factories
{
    /// <summary>
    /// Debug tests to understand what's happening with our factory pattern
    /// </summary>
    public class FactoryDebugTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly List<string> _tempDirectories = new();

        public FactoryDebugTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Debug_Custom_Command_Behavior()
        {
            // Arrange: Simple project with build script
            var projectDir = CreateProjectWithScript("build", "vite build");
            _output.WriteLine($"Project directory: {projectDir}");
            
            // Act: Try different custom command formats
            var simpleConfig = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Npm,
                CustomBuildCommand = "my-custom-script"
            };
            var simpleCommand = new ViteCommandBuilder(simpleConfig).BuildCommand();
            
            var complexConfig = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Npm,
                CustomBuildCommand = "yarn build:enterprise --mode production"
            };
            var complexCommand = new ViteCommandBuilder(complexConfig).BuildCommand();
            
            // Debug output for simple command (just executable)
            _output.WriteLine("=== SIMPLE CUSTOM COMMAND ===");
            _output.WriteLine($"Input: 'my-custom-script'");
            _output.WriteLine($"Executable: '{simpleCommand.Executable}'");
            _output.WriteLine($"Command: '{simpleCommand.Command}'");
            _output.WriteLine($"ToString(): '{simpleCommand}'");
            
            // Debug output for complex command (executable + arguments)
            _output.WriteLine("\n=== COMPLEX CUSTOM COMMAND ===");
            _output.WriteLine($"Input: 'yarn build:enterprise --mode production'");
            _output.WriteLine($"Executable: '{complexCommand.Executable}'");
            _output.WriteLine($"Command: '{complexCommand.Command}'");
            _output.WriteLine($"ToString(): '{complexCommand}'");
            
            // Validate our new correct behavior
            // Simple command: just executable, no arguments
            if (simpleCommand.Executable != "my-custom-script" || simpleCommand.Command != "")
            {
                throw new Exception($"Simple command failed: Expected Executable='my-custom-script', Command='', but got Executable='{simpleCommand.Executable}', Command='{simpleCommand.Command}'");
            }
            
            // Complex command: executable + arguments  
            if (complexCommand.Executable != "yarn" || complexCommand.Command != "build:enterprise --mode production")
            {
                throw new Exception($"Complex command failed: Expected Executable='yarn', Command='build:enterprise --mode production', but got Executable='{complexCommand.Executable}', Command='{complexCommand.Command}'");
            }
            
            _output.WriteLine("\n✅ SUCCESS: CustomCommandBuilder correctly uses literal command execution!");
        }

        [Fact]
        public void Debug_Script_Based_Behavior()
        {
            // Arrange: Simple project with build script
            var projectDir = CreateProjectWithScript("build", "vite build");
            
            // Act: Regular build (should detect script)
            var config = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Npm
            };
            var command = new ViteCommandBuilder(config).BuildCommand();
            
            // Debug output
            _output.WriteLine($"Script-based Executable: '{command.Executable}'");
            _output.WriteLine($"Script-based Command: '{command.Command}'");
        }

        [Fact]
        public void Debug_Direct_Tool_Behavior()
        {
            // Arrange: Project without scripts
            var projectDir = CreateTempDirectory();
            
            // Act: Regular build (should fallback to direct tool)
            var config = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Npm
            };
            var command = new ViteCommandBuilder(config).BuildCommand();
            
            // Debug output
            _output.WriteLine($"Direct tool Executable: '{command.Executable}'");
            _output.WriteLine($"Direct tool Command: '{command.Command}'");
        }

        [Fact]
        public void Debug_PowerShell_And_Script_Commands()
        {
            var projectDir = CreateTempDirectory();
            _output.WriteLine($"Project directory: {projectDir}");
            
            // Test PowerShell commands
            var powershellConfig = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Npm,
                CustomBuildCommand = "powershell -Command \"Write-Host 'Building with PowerShell'; npm run build\""
            };
            var powershellCommand = new ViteCommandBuilder(powershellConfig).BuildCommand();
            
            // Test batch script
            var batchConfig = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Npm,
                CustomBuildCommand = "cmd /c \"echo Building... && npm run build\""
            };
            var batchCommand = new ViteCommandBuilder(batchConfig).BuildCommand();
            
            // Test shell script (Linux/Mac)
            var shellConfig = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Npm,
                CustomBuildCommand = "sh -c \"echo 'Building with shell' && npm run build\""
            };
            var shellCommand = new ViteCommandBuilder(shellConfig).BuildCommand();
            
            // Test direct executable with full path
            var directConfig = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Npm,
                CustomBuildCommand = "C:\\MyScripts\\custom-build.bat --production"
            };
            var directCommand = new ViteCommandBuilder(directConfig).BuildCommand();
            
            _output.WriteLine("=== POWERSHELL COMMAND ===");
            _output.WriteLine($"Input: PowerShell with embedded npm command");
            _output.WriteLine($"Executable: '{powershellCommand.Executable}'");
            _output.WriteLine($"Command: '{powershellCommand.Command}'");
            
            _output.WriteLine("\n=== BATCH COMMAND ===");
            _output.WriteLine($"Input: cmd /c with echo and npm");
            _output.WriteLine($"Executable: '{batchCommand.Executable}'");
            _output.WriteLine($"Command: '{batchCommand.Command}'");
            
            _output.WriteLine("\n=== SHELL SCRIPT ===");
            _output.WriteLine($"Input: sh -c with echo and npm");
            _output.WriteLine($"Executable: '{shellCommand.Executable}'");
            _output.WriteLine($"Command: '{shellCommand.Command}'");
            
            _output.WriteLine("\n=== DIRECT EXECUTABLE ===");
            _output.WriteLine($"Input: Full path to custom script");
            _output.WriteLine($"Executable: '{directCommand.Executable}'");
            _output.WriteLine($"Command: '{directCommand.Command}'");
            
            // Validate they parse correctly
            if (powershellCommand.Executable != "powershell")
                throw new Exception($"PowerShell command failed to parse correctly");
                
            if (batchCommand.Executable != "cmd")
                throw new Exception($"Batch command failed to parse correctly");
                
            if (shellCommand.Executable != "sh") 
                throw new Exception($"Shell command failed to parse correctly");
                
            if (directCommand.Executable != "C:\\MyScripts\\custom-build.bat")
                throw new Exception($"Direct executable failed to parse correctly");
            
            _output.WriteLine("\n✅ SUCCESS: All script types work with CustomCommandBuilder!");
        }

        [Fact]
        public void Debug_OS_Conditional_Commands()
        {
            var projectDir = CreateTempDirectory();
            _output.WriteLine($"Project directory: {projectDir}");
            _output.WriteLine($"Current OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
            _output.WriteLine($"Is Windows: {System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)}");
            _output.WriteLine($"Is Linux: {System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux)}");
            _output.WriteLine($"Is macOS: {System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX)}");
            
            // Simulate what MSBuild conditional properties would do
            string osSpecificCommand;
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                osSpecificCommand = "powershell -Command \"Write-Host 'Windows Build'; npm run build\"";
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
            {
                osSpecificCommand = "sh -c \"echo 'Linux Build' && npm run build\"";
            }
            else // macOS or other Unix
            {
                osSpecificCommand = "sh -c \"echo 'macOS Build' && npm run build\"";
            }
            
            // Test the OS-specific command
            var osConfig = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Npm,
                CustomBuildCommand = osSpecificCommand
            };
            var osCommand = new ViteCommandBuilder(osConfig).BuildCommand();
            
            _output.WriteLine("\n=== OS-SPECIFIC COMMAND ===");
            _output.WriteLine($"Detected Command: {osSpecificCommand}");
            _output.WriteLine($"Executable: '{osCommand.Executable}'");
            _output.WriteLine($"Command: '{osCommand.Command}'");
            _output.WriteLine($"Full Command: '{osCommand}'");
            
            // Validate the command was parsed correctly
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                if (osCommand.Executable != "powershell")
                    throw new Exception($"Expected PowerShell on Windows, got: {osCommand.Executable}");
                _output.WriteLine("✅ Windows: PowerShell command detected correctly");
            }
            else
            {
                if (osCommand.Executable != "sh")
                    throw new Exception($"Expected sh on Unix, got: {osCommand.Executable}");
                _output.WriteLine("✅ Unix: Shell command detected correctly");
            }
            
            _output.WriteLine("\n✅ SUCCESS: OS conditional commands work perfectly!");
        }

        [Fact]
        public void Debug_Working_Directory_Commands()
        {
            var projectDir = CreateTempDirectory();
            _output.WriteLine($"Project directory: {projectDir}");
            
            // Test different working directory approaches
            
            // 1. PowerShell with directory change
            var powershellDirConfig = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Npm,
                CustomBuildCommand = "powershell -Command \"cd frontend; npm run build\""
            };
            var powershellDir = new ViteCommandBuilder(powershellDirConfig).BuildCommand();
            
            // 2. Shell with directory change
            var shellDirConfig = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Npm,
                CustomBuildCommand = "sh -c \"cd frontend && npm run build\""
            };
            var shellDir = new ViteCommandBuilder(shellDirConfig).BuildCommand();
            
            // 3. Batch with directory change
            var batchDirConfig = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Npm,
                CustomBuildCommand = "cmd /c \"cd frontend && npm run build\""
            };
            var batchDir = new ViteCommandBuilder(batchDirConfig).BuildCommand();
            
            // 4. Complex multi-directory command
            var multiDirConfig = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Npm,
                CustomBuildCommand = "powershell -Command \"cd apps/admin; npm run build; cd ../customer; npm run build\""
            };
            var multiDir = new ViteCommandBuilder(multiDirConfig).BuildCommand();
            
            // 5. Docker with working directory
            var dockerDirConfig = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Npm,
                CustomBuildCommand = "docker run --rm -v \"$(pwd):/app\" -w /app/frontend node:18 npm run build"
            };
            var dockerDir = new ViteCommandBuilder(dockerDirConfig).BuildCommand();
            
            _output.WriteLine("\n=== POWERSHELL WITH DIRECTORY ===");
            _output.WriteLine($"Command: '{powershellDir}'");
            _output.WriteLine($"Executable: '{powershellDir.Executable}'");
            _output.WriteLine($"Args: '{powershellDir.Command}'");
            
            _output.WriteLine("\n=== SHELL WITH DIRECTORY ===");
            _output.WriteLine($"Command: '{shellDir}'");
            _output.WriteLine($"Executable: '{shellDir.Executable}'");
            _output.WriteLine($"Args: '{shellDir.Command}'");
            
            _output.WriteLine("\n=== BATCH WITH DIRECTORY ===");
            _output.WriteLine($"Command: '{batchDir}'");
            _output.WriteLine($"Executable: '{batchDir.Executable}'");
            _output.WriteLine($"Args: '{batchDir.Command}'");
            
            _output.WriteLine("\n=== MULTI-DIRECTORY COMMAND ===");
            _output.WriteLine($"Command: '{multiDir}'");
            _output.WriteLine($"Full Command: '{multiDir}'");
            
            _output.WriteLine("\n=== DOCKER WITH WORKING DIR ===");
            _output.WriteLine($"Command: '{dockerDir}'");
            _output.WriteLine($"Executable: '{dockerDir.Executable}'");
            _output.WriteLine($"Args: '{dockerDir.Command}'");
            
            // Validate they parse correctly
            powershellDir.Executable.Should().Be("powershell");
            powershellDir.Command.Should().Contain("cd frontend");
            
            shellDir.Executable.Should().Be("sh");
            shellDir.Command.Should().Contain("cd frontend");
            
            batchDir.Executable.Should().Be("cmd");
            batchDir.Command.Should().Contain("cd frontend");
            
            multiDir.Executable.Should().Be("powershell");
            multiDir.Command.Should().Contain("cd apps/admin");
            
            dockerDir.Executable.Should().Be("docker");
            dockerDir.Command.Should().Contain("-w /app/frontend");
            
            _output.WriteLine("\n✅ SUCCESS: All working directory commands parse correctly!");
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
