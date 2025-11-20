using FluentAssertions;
using ViteKit.MsBuild.Tasks;
using Xunit;

namespace ViteKit.MsBuild.PureUnitTests.Tasks
{
    public class ViteConfigTests
    {
        [Fact]
        public void Execute_WithMinimalProperties_CreatesValidConfig()
        {
            // Arrange
            var task = new ViteConfig
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                Name = "admin",
                ConfigFile = "vite.admin.config.ts"
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.CreatedConfig.Should().NotBeNull();
            task.CreatedConfig!.ItemSpec.Should().Be("vite.admin.config.ts");
            task.CreatedConfig.GetMetadata("BuildId").Should().Be("admin");
        }

        [Fact]
        public void Execute_WithAllProperties_CreatesCompleteConfig()
        {
            // Arrange
            var task = new ViteConfig
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                Name = "admin",
                ConfigFile = "vite.admin.config.ts",
                OutputDir = "wwwroot/admin",
                Mode = "production",
                PackageManager = "pnpm",
                LogLevel = "info",
                EnableColors = true,
                CustomCommand = "custom-build",
                DependsOn = "shared,components",
                LinkDependencies = true
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            var config = task.CreatedConfig!;
            
            config.ItemSpec.Should().Be("vite.admin.config.ts");
            config.GetMetadata("BuildId").Should().Be("admin");
            config.GetMetadata("OutputDir").Should().Be("wwwroot/admin");
            config.GetMetadata("Mode").Should().Be("production");
            config.GetMetadata("PackageManager").Should().Be("pnpm");
            config.GetMetadata("LogLevel").Should().Be("info");
            config.GetMetadata("EnableColors").Should().Be("true");
            config.GetMetadata("CustomCommand").Should().Be("custom-build");
            config.GetMetadata("DependsOn").Should().Be("shared,components");
            config.GetMetadata("LinkDependencies").Should().Be("true");
        }

        [Fact]
        public void Execute_WithNullOptionalProperties_OmitsMetadata()
        {
            // Arrange
            var task = new ViteConfig
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                Name = "admin",
                ConfigFile = "vite.admin.config.ts",
                OutputDir = null,
                Mode = null,
                PackageManager = null
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            var config = task.CreatedConfig!;
            
            config.GetMetadata("BuildId").Should().Be("admin");
            config.GetMetadata("OutputDir").Should().BeEmpty();
            config.GetMetadata("Mode").Should().BeEmpty();
            config.GetMetadata("PackageManager").Should().BeEmpty();
        }

        [Fact]
        public void Execute_WithEnableColorsFalse_SetsCorrectMetadata()
        {
            // Arrange
            var task = new ViteConfig
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                Name = "admin",
                ConfigFile = "vite.admin.config.ts",
                EnableColors = false
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.CreatedConfig!.GetMetadata("EnableColors").Should().Be("false");
        }

        [Fact]
        public void Execute_WithLinkDependenciesFalse_SetsCorrectMetadata()
        {
            // Arrange
            var task = new ViteConfig
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                Name = "admin",
                ConfigFile = "vite.admin.config.ts",
                LinkDependencies = false
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.CreatedConfig!.GetMetadata("LinkDependencies").Should().Be("false");
        }

        [Fact]
        public void Execute_WithNullableBoolNull_OmitsMetadata()
        {
            // Arrange
            var task = new ViteConfig
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                Name = "admin",
                ConfigFile = "vite.admin.config.ts",
                EnableColors = null,
                LinkDependencies = null
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            var config = task.CreatedConfig!;
            
            config.GetMetadata("EnableColors").Should().BeEmpty();
            config.GetMetadata("LinkDependencies").Should().BeEmpty();
        }

        [Fact]
        public void Execute_WithEmptyStrings_SetsEmptyMetadata()
        {
            // Arrange
            var task = new ViteConfig
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                Name = "admin",
                ConfigFile = "vite.admin.config.ts",
                OutputDir = "",
                Mode = "",
                DependsOn = ""
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            // Empty strings should not add metadata (checked with IsNullOrEmpty)
            var config = task.CreatedConfig!;
            config.GetMetadata("OutputDir").Should().BeEmpty();
            config.GetMetadata("Mode").Should().BeEmpty();
            config.GetMetadata("DependsOn").Should().BeEmpty();
        }

        [Fact]
        public void Execute_WithMultipleDependencies_PreservesList()
        {
            // Arrange
            var task = new ViteConfig
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                Name = "app",
                ConfigFile = "vite.app.config.ts",
                DependsOn = "shared,components,utils"
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.CreatedConfig!.GetMetadata("DependsOn").Should().Be("shared,components,utils");
        }

        [Theory]
        [InlineData("silent")]
        [InlineData("error")]
        [InlineData("warn")]
        [InlineData("info")]
        [InlineData("debug")]
        public void Execute_WithValidLogLevels_AcceptsAll(string logLevel)
        {
            // Arrange
            var task = new ViteConfig
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                Name = "admin",
                ConfigFile = "vite.admin.config.ts",
                LogLevel = logLevel
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.CreatedConfig!.GetMetadata("LogLevel").Should().Be(logLevel);
        }

        [Theory]
        [InlineData("npm")]
        [InlineData("yarn")]
        [InlineData("pnpm")]
        [InlineData("bun")]
        public void Execute_WithValidPackageManagers_AcceptsAll(string packageManager)
        {
            // Arrange
            var task = new ViteConfig
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                Name = "admin",
                ConfigFile = "vite.admin.config.ts",
                PackageManager = packageManager
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.CreatedConfig!.GetMetadata("PackageManager").Should().Be(packageManager);
        }

        [Theory]
        [InlineData("development")]
        [InlineData("production")]
        [InlineData("staging")]
        [InlineData("test")]
        [InlineData("custom-mode")]
        public void Execute_WithVariousModes_AcceptsAll(string mode)
        {
            // Arrange
            var task = new ViteConfig
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                Name = "admin",
                ConfigFile = "vite.admin.config.ts",
                Mode = mode
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.CreatedConfig!.GetMetadata("Mode").Should().Be(mode);
        }

        [Fact]
        public void Execute_WithComplexConfigPaths_HandlesCorrectly()
        {
            // Arrange
            var task = new ViteConfig
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                Name = "admin",
                ConfigFile = "Areas/Admin/ClientApp/vite.config.ts"
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.CreatedConfig!.ItemSpec.Should().Be("Areas/Admin/ClientApp/vite.config.ts");
        }

        [Fact]
        public void Execute_WithComplexOutputPaths_HandlesCorrectly()
        {
            // Arrange
            var task = new ViteConfig
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                Name = "admin",
                ConfigFile = "vite.admin.config.ts",
                OutputDir = "wwwroot/spa/admin/dist"
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.CreatedConfig!.GetMetadata("OutputDir").Should().Be("wwwroot/spa/admin/dist");
        }
    }
}
