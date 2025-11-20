using FluentAssertions;
using System;
using System.IO;
using ViteKit.MsBuild.Tasks;
using Xunit;

namespace ViteKit.MsBuild.PureUnitTests.Tasks
{
    public class ValidateViteConfigTests : IDisposable
    {
        private readonly string _tempDir;

        public ValidateViteConfigTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"ViteMsBuildTests_{System.Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
        }

        [Fact]
        public void Execute_WithValidConfig_ReturnsValidTrue()
        {
            // Arrange
            var configPath = Path.Combine(_tempDir, "vite.config.ts");
            File.WriteAllText(configPath, @"
import { defineConfig } from 'vite'
export default defineConfig({
  build: {
    outDir: 'wwwroot/dist'
  }
})");
            
            var task = new ValidateViteConfig
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ConfigPath = configPath
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Execute_WithNonExistentConfig_ReturnsValidFalse()
        {
            // Arrange
            var task = new ValidateViteConfig
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ConfigPath = "/nonexistent/vite.config.ts"
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue(); // Execute returns true, but IsValid is false
            task.IsValid.Should().BeFalse();
        }

        [Fact]
        public void Execute_WithExportDefault_PassesValidation()
        {
            // Arrange
            var configPath = Path.Combine(_tempDir, "vite.config.ts");
            File.WriteAllText(configPath, "export default {}");
            
            var task = new ValidateViteConfig
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ConfigPath = configPath
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Execute_WithModuleExports_PassesValidation()
        {
            // Arrange
            var configPath = Path.Combine(_tempDir, "vite.config.js");
            File.WriteAllText(configPath, "module.exports = {}");
            
            var task = new ValidateViteConfig
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ConfigPath = configPath
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Execute_WithoutExportStatement_AddsWarning()
        {
            // Arrange
            var configPath = Path.Combine(_tempDir, "vite.config.ts");
            File.WriteAllText(configPath, "const config = {}");
            
            var task = new ValidateViteConfig
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ConfigPath = configPath
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.IsValid.Should().BeTrue(); // Still valid, just warnings
            task.Warnings.Should().NotBeNull();
            task.Warnings!.Should().Contain(w => w.Contains("export statement"));
        }

        [Fact]
        public void Execute_DetectsOutputDirectory()
        {
            // Arrange
            var configPath = Path.Combine(_tempDir, "vite.config.ts");
            File.WriteAllText(configPath, @"
export default {
  build: {
    outDir: 'wwwroot/custom'
  }
}");
            
            var task = new ValidateViteConfig
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ConfigPath = configPath
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.DetectedOutputDir.Should().Be("wwwroot/custom");
        }

        [Fact]
        public void Execute_DetectsOutputDirectorySimplePattern()
        {
            // Arrange
            var configPath = Path.Combine(_tempDir, "vite.config.ts");
            File.WriteAllText(configPath, "outDir: 'dist/output'");
            
            var task = new ValidateViteConfig
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ConfigPath = configPath
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.DetectedOutputDir.Should().Be("dist/output");
        }

        [Fact]
        public void Execute_WithExpectedOutputDirMatch_NoWarning()
        {
            // Arrange
            var configPath = Path.Combine(_tempDir, "vite.config.ts");
            File.WriteAllText(configPath, "export default { build: { outDir: 'wwwroot/dist' } }");
            
            var task = new ValidateViteConfig
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ConfigPath = configPath,
                ExpectedOutputDir = "wwwroot/dist"
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.Warnings.Should().BeEmpty();
        }

        [Fact]
        public void Execute_WithExpectedOutputDirMismatch_AddsWarning()
        {
            // Arrange
            var configPath = Path.Combine(_tempDir, "vite.config.ts");
            File.WriteAllText(configPath, "export default { build: { outDir: 'wwwroot/custom' } }");
            
            var task = new ValidateViteConfig
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ConfigPath = configPath,
                ExpectedOutputDir = "wwwroot/dist"
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.Warnings.Should().NotBeNull();
            task.Warnings!.Should().Contain(w => 
                w.Contains("Output directory mismatch") && 
                w.Contains("wwwroot/dist") && 
                w.Contains("wwwroot/custom"));
        }

        [Fact]
        public void Execute_CaseInsensitiveOutputDirMatch_NoWarning()
        {
            // Arrange
            var configPath = Path.Combine(_tempDir, "vite.config.ts");
            File.WriteAllText(configPath, "export default { build: { outDir: 'WWWRoot/Dist' } }");
            
            var task = new ValidateViteConfig
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ConfigPath = configPath,
                ExpectedOutputDir = "wwwroot/dist"
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.Warnings.Should().BeEmpty();
        }

        [Fact]
        public void Execute_WithNoDetectedOutputDir_NoMismatchWarning()
        {
            // Arrange
            var configPath = Path.Combine(_tempDir, "vite.config.ts");
            File.WriteAllText(configPath, "export default {}"); // No outDir
            
            var task = new ValidateViteConfig
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ConfigPath = configPath,
                ExpectedOutputDir = "wwwroot/dist"
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.DetectedOutputDir.Should().BeNullOrEmpty();
            // Should not warn about mismatch if we couldn't detect output dir
            task.Warnings.Should().NotContain(w => w.Contains("mismatch"));
        }

        [Fact]
        public void Execute_WithComplexConfig_DetectsOutputDir()
        {
            // Arrange
            var configPath = Path.Combine(_tempDir, "vite.config.ts");
            File.WriteAllText(configPath, @"
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  build: {
    outDir: 'wwwroot/spa/admin',
    emptyOutDir: true
  },
  server: {
    port: 5173
  }
})");
            
            var task = new ValidateViteConfig
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ConfigPath = configPath
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.DetectedOutputDir.Should().Be("wwwroot/spa/admin");
        }

        [Fact]
        public void Execute_WithSingleQuotes_DetectsOutputDir()
        {
            // Arrange
            var configPath = Path.Combine(_tempDir, "vite.config.ts");
            File.WriteAllText(configPath, "outDir: 'dist/output'");
            
            var task = new ValidateViteConfig
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ConfigPath = configPath
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.DetectedOutputDir.Should().Be("dist/output");
        }

        [Fact]
        public void Execute_WithDoubleQuotes_DetectsOutputDir()
        {
            // Arrange
            var configPath = Path.Combine(_tempDir, "vite.config.ts");
            File.WriteAllText(configPath, "outDir: \"dist/output\"");
            
            var task = new ValidateViteConfig
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ConfigPath = configPath
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.DetectedOutputDir.Should().Be("dist/output");
        }

        [Fact]
        public void Execute_LogsWarningsToMSBuild()
        {
            // Arrange
            var mockEngine = new Helpers.MockBuildEngine();
            var configPath = Path.Combine(_tempDir, "vite.config.ts");
            File.WriteAllText(configPath, "const config = {}"); // Missing export
            
            var task = new ValidateViteConfig
            {
                BuildEngine = mockEngine,
                ConfigPath = configPath
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            mockEngine.LoggedWarnings.Should().ContainSingle(w => w.Message.Contains("export statement"));
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                try
                {
                    Directory.Delete(_tempDir, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}
