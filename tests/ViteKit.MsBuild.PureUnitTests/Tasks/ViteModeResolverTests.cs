using FluentAssertions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using ViteKit.MsBuild.Tasks;
using Xunit;

namespace ViteKit.MsBuild.PureUnitTests.Tasks
{
    public class ViteModeResolverTests
    {
        [Fact]
        public void Execute_WithNoOverrides_UsesDefaultMode()
        {
            // Arrange
            var task = new ViteModeResolver
            {
                BuildEngine = new Fixtures.MockBuildEngine(),
                ViteConfigs = new[]
                {
                    CreateConfig("admin", "vite.admin.config.ts")
                },
                DefaultMode = "development"
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.ResolvedConfigs.Should().HaveCount(1);
            task.ResolvedConfigs![0].GetMetadata("EffectiveMode").Should().Be("development");
        }

        [Fact]
        public void Execute_WithGlobalViteMode_UsesGlobalMode()
        {
            // Arrange
            var task = new ViteModeResolver
            {
                BuildEngine = new Fixtures.MockBuildEngine(),
                ViteConfigs = new[]
                {
                    CreateConfig("admin", "vite.admin.config.ts")
                },
                ViteMode = "production",
                DefaultMode = "development"
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.ResolvedConfigs![0].GetMetadata("EffectiveMode").Should().Be("production");
        }

        [Fact]
        public void Execute_WithItemGroupMode_UsesItemGroupMode()
        {
            // Arrange
            var config = CreateConfig("admin", "vite.admin.config.ts");
            config.SetMetadata("Mode", "staging");
            
            var task = new ViteModeResolver
            {
                BuildEngine = new Fixtures.MockBuildEngine(),
                ViteConfigs = new[] { config },
                DefaultMode = "development"
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.ResolvedConfigs![0].GetMetadata("EffectiveMode").Should().Be("staging");
        }

        [Fact]
        public void Execute_WithConfigSpecificProperty_UsesConfigSpecificMode()
        {
            // Arrange
            var task = new ViteModeResolver
            {
                BuildEngine = new Fixtures.MockBuildEngine(),
                ViteConfigs = new[]
                {
                    CreateConfig("admin", "vite.admin.config.ts")
                },
                Properties = new[]
                {
                    CreateProperty("AdminViteMode", "custom")
                },
                ViteMode = "production",
                DefaultMode = "development"
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.ResolvedConfigs![0].GetMetadata("EffectiveMode").Should().Be("custom");
        }

        [Fact]
        public void Execute_FollowsOverrideHierarchy()
        {
            // Test hierarchy: Config-specific > ItemGroup > Global > Default
            
            // Arrange
            var adminConfig = CreateConfig("admin", "vite.admin.config.ts");
            adminConfig.SetMetadata("Mode", "itemgroup-mode");
            
            var customerConfig = CreateConfig("customer", "vite.customer.config.ts");
            customerConfig.SetMetadata("Mode", "itemgroup-mode");
            
            var sharedConfig = CreateConfig("shared", "vite.shared.config.ts");
            // No item group mode
            
            var task = new ViteModeResolver
            {
                BuildEngine = new Fixtures.MockBuildEngine(),
                ViteConfigs = new[] { adminConfig, customerConfig, sharedConfig },
                Properties = new[]
                {
                    CreateProperty("AdminViteMode", "admin-specific") // Override for admin only
                },
                ViteMode = "global-mode",
                DefaultMode = "default-mode"
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.ResolvedConfigs.Should().HaveCount(3);
            
            // Admin: Config-specific property wins (highest priority)
            task.ResolvedConfigs![0].GetMetadata("EffectiveMode").Should().Be("admin-specific");
            
            // Customer: ItemGroup Mode wins (has ItemGroup mode, no config-specific property)
            task.ResolvedConfigs[1].GetMetadata("EffectiveMode").Should().Be("itemgroup-mode");
            
            // Shared: Global ViteMode wins (no ItemGroup mode, no config-specific property)
            task.ResolvedConfigs[2].GetMetadata("EffectiveMode").Should().Be("global-mode");
        }

        [Fact]
        public void Execute_WithMultipleProperties_UsesCorrectOnes()
        {
            // Arrange
            var task = new ViteModeResolver
            {
                BuildEngine = new Fixtures.MockBuildEngine(),
                ViteConfigs = new[]
                {
                    CreateConfig("admin", "vite.admin.config.ts"),
                    CreateConfig("customer", "vite.customer.config.ts")
                },
                Properties = new[]
                {
                    CreateProperty("AdminViteMode", "admin-production"),
                    CreateProperty("CustomerViteMode", "customer-staging")
                },
                DefaultMode = "development"
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.ResolvedConfigs![0].GetMetadata("EffectiveMode").Should().Be("admin-production");
            task.ResolvedConfigs[1].GetMetadata("EffectiveMode").Should().Be("customer-staging");
        }

        [Fact]
        public void Execute_PropertyNamesAreCaseInsensitive()
        {
            // Arrange
            var task = new ViteModeResolver
            {
                BuildEngine = new Fixtures.MockBuildEngine(),
                ViteConfigs = new[]
                {
                    CreateConfig("Admin", "vite.admin.config.ts") // Capital A
                },
                Properties = new[]
                {
                    CreateProperty("adminvitemode", "production") // lowercase
                },
                DefaultMode = "development"
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.ResolvedConfigs![0].GetMetadata("EffectiveMode").Should().Be("production");
        }

        [Fact]
        public void Execute_WithEmptyModeValues_FallsBackToNextLevel()
        {
            // Arrange
            var config = CreateConfig("admin", "vite.admin.config.ts");
            config.SetMetadata("Mode", ""); // Empty mode
            
            var task = new ViteModeResolver
            {
                BuildEngine = new Fixtures.MockBuildEngine(),
                ViteConfigs = new[] { config },
                ViteMode = "production",
                DefaultMode = "development"
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            // Empty ItemGroup mode should fall back to global ViteMode
            task.ResolvedConfigs![0].GetMetadata("EffectiveMode").Should().Be("production");
        }

        [Fact]
        public void Execute_WithNullViteMode_SkipsGlobalLevel()
        {
            // Arrange
            var config = CreateConfig("admin", "vite.admin.config.ts");
            
            var task = new ViteModeResolver
            {
                BuildEngine = new Fixtures.MockBuildEngine(),
                ViteConfigs = new[] { config },
                ViteMode = null, // Null global mode
                DefaultMode = "development"
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.ResolvedConfigs![0].GetMetadata("EffectiveMode").Should().Be("development");
        }

        [Fact]
        public void Execute_WithEmptyPropertyValue_IgnoresProperty()
        {
            // Arrange
            var task = new ViteModeResolver
            {
                BuildEngine = new Fixtures.MockBuildEngine(),
                ViteConfigs = new[]
                {
                    CreateConfig("admin", "vite.admin.config.ts")
                },
                Properties = new[]
                {
                    CreateProperty("AdminViteMode", "") // Empty value
                },
                ViteMode = "production",
                DefaultMode = "development"
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            // Empty property should fall back to global ViteMode
            task.ResolvedConfigs![0].GetMetadata("EffectiveMode").Should().Be("production");
        }

        [Fact]
        public void Execute_WithMultipleConfigs_ResolvesEachIndependently()
        {
            // Arrange
            var adminConfig = CreateConfig("admin", "vite.admin.config.ts");
            adminConfig.SetMetadata("Mode", "admin-staging");
            
            var customerConfig = CreateConfig("customer", "vite.customer.config.ts");
            // No mode metadata
            
            var sharedConfig = CreateConfig("shared", "vite.shared.config.ts");
            sharedConfig.SetMetadata("Mode", "shared-production");
            
            var task = new ViteModeResolver
            {
                BuildEngine = new Fixtures.MockBuildEngine(),
                ViteConfigs = new[] { adminConfig, customerConfig, sharedConfig },
                Properties = new[]
                {
                    CreateProperty("CustomerViteMode", "customer-custom")
                },
                ViteMode = "global-production",
                DefaultMode = "development"
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.ResolvedConfigs.Should().HaveCount(3);
            
            // Admin: ItemGroup mode
            task.ResolvedConfigs![0].GetMetadata("EffectiveMode").Should().Be("admin-staging");
            
            // Customer: Config-specific property
            task.ResolvedConfigs[1].GetMetadata("EffectiveMode").Should().Be("customer-custom");
            
            // Shared: ItemGroup mode
            task.ResolvedConfigs[2].GetMetadata("EffectiveMode").Should().Be("shared-production");
        }

        [Fact]
        public void Execute_PreservesOriginalMetadata()
        {
            // Arrange
            var config = CreateConfig("admin", "vite.admin.config.ts");
            config.SetMetadata("OutputDir", "wwwroot/admin");
            config.SetMetadata("CustomProperty", "CustomValue");
            
            var task = new ViteModeResolver
            {
                BuildEngine = new Fixtures.MockBuildEngine(),
                ViteConfigs = new[] { config },
                ViteMode = "production",
                DefaultMode = "development"
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            var resolved = task.ResolvedConfigs![0];
            resolved.GetMetadata("BuildId").Should().Be("admin");
            resolved.GetMetadata("OutputDir").Should().Be("wwwroot/admin");
            resolved.GetMetadata("CustomProperty").Should().Be("CustomValue");
            resolved.GetMetadata("EffectiveMode").Should().Be("production");
        }

        [Fact]
        public void Execute_WithNoConfigs_ReturnsEmptyArray()
        {
            // Arrange
            var task = new ViteModeResolver
            {
                BuildEngine = new Fixtures.MockBuildEngine(),
                ViteConfigs = System.Array.Empty<ITaskItem>(),
                DefaultMode = "development"
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.ResolvedConfigs.Should().BeEmpty();
        }

        private static ITaskItem CreateConfig(string buildId, string configFile)
        {
            var item = new TaskItem(configFile);
            item.SetMetadata("BuildId", buildId);
            return item;
        }

        private static ITaskItem CreateProperty(string name, string value)
        {
            var item = new TaskItem(name);
            item.SetMetadata("Value", value);
            return item;
        }
    }
}

