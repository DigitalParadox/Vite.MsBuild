using FluentAssertions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using ViteKit.MsBuild.Tasks;
using Xunit;

namespace ViteKit.MsBuild.PureUnitTests.Tasks
{
    public class ViteConfigDependencyResolverTests
    {
        [Fact]
        public void Execute_WithNoDependencies_ReturnsConfigsInOriginalOrder()
        {
            // Arrange
            var task = new ViteConfigDependencyResolver
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ViteConfigurations = new[]
                {
                    CreateConfig("admin", "vite.admin.config.ts"),
                    CreateConfig("customer", "vite.customer.config.ts"),
                    CreateConfig("shared", "vite.shared.config.ts")
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.OrderedConfigurations.Should().HaveCount(3);
            task.DependencyGroups.Should().NotBeNull();
            task.DependencyGroups!.Length.Should().Be(1); // All in same group since no dependencies
        }

        [Fact]
        public void Execute_WithLinearDependencies_ReturnsCorrectOrder()
        {
            // Arrange
            var task = new ViteConfigDependencyResolver
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ViteConfigurations = new[]
                {
                    CreateConfig("app", "vite.app.config.ts", "shared"), // Depends on shared
                    CreateConfig("shared", "vite.shared.config.ts")      // No dependencies
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.OrderedConfigurations.Should().HaveCount(2);
            task.OrderedConfigurations![0].GetMetadata("BuildId").Should().Be("shared");
            task.OrderedConfigurations[1].GetMetadata("BuildId").Should().Be("app");
        }

        [Fact]
        public void Execute_WithMultipleDependencies_ReturnsCorrectOrder()
        {
            // Arrange
            var task = new ViteConfigDependencyResolver
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ViteConfigurations = new[]
                {
                    CreateConfig("app", "vite.app.config.ts", "shared,components"), // Depends on both
                    CreateConfig("shared", "vite.shared.config.ts"),                 // No dependencies
                    CreateConfig("components", "vite.components.config.ts")          // No dependencies
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.OrderedConfigurations.Should().HaveCount(3);
            
            // Last item should be 'app' since it depends on others
            task.OrderedConfigurations![2].GetMetadata("BuildId").Should().Be("app");
            
            // First two should be shared and components (order may vary)
            var firstTwo = new[] { 
                task.OrderedConfigurations[0].GetMetadata("BuildId"), 
                task.OrderedConfigurations[1].GetMetadata("BuildId") 
            };
            firstTwo.Should().Contain("shared");
            firstTwo.Should().Contain("components");
        }

        [Fact]
        public void Execute_WithChainedDependencies_ReturnsCorrectOrder()
        {
            // Arrange
            var task = new ViteConfigDependencyResolver
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ViteConfigurations = new[]
                {
                    CreateConfig("level3", "vite.level3.config.ts", "level2"),
                    CreateConfig("level2", "vite.level2.config.ts", "level1"),
                    CreateConfig("level1", "vite.level1.config.ts")
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.OrderedConfigurations.Should().HaveCount(3);
            task.OrderedConfigurations![0].GetMetadata("BuildId").Should().Be("level1");
            task.OrderedConfigurations[1].GetMetadata("BuildId").Should().Be("level2");
            task.OrderedConfigurations[2].GetMetadata("BuildId").Should().Be("level3");
        }

        [Fact]
        public void Execute_WithCircularDependency_ReturnsFalseAndLogsError()
        {
            // Arrange
            var mockEngine = new Helpers.MockBuildEngine();
            var task = new ViteConfigDependencyResolver
            {
                BuildEngine = mockEngine,
                ViteConfigurations = new[]
                {
                    CreateConfig("a", "vite.a.config.ts", "b"),
                    CreateConfig("b", "vite.b.config.ts", "a") // Circular!
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeFalse();
            mockEngine.LoggedErrors.Should().ContainSingle(e => e.Message.Contains("Circular dependency"));
        }

        [Fact]
        public void Execute_WithComplexCircularDependency_DetectsCircle()
        {
            // Arrange
            var mockEngine = new Helpers.MockBuildEngine();
            var task = new ViteConfigDependencyResolver
            {
                BuildEngine = mockEngine,
                ViteConfigurations = new[]
                {
                    CreateConfig("a", "vite.a.config.ts", "b"),
                    CreateConfig("b", "vite.b.config.ts", "c"),
                    CreateConfig("c", "vite.c.config.ts", "a") // Circle: a->b->c->a
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeFalse();
            mockEngine.LoggedErrors.Should().ContainSingle(e => e.Message.Contains("Circular dependency"));
        }

        [Fact]
        public void Execute_WithMissingDependency_LogsErrorAndReturnsFalse()
        {
            // Arrange
            var mockEngine = new Helpers.MockBuildEngine();
            var task = new ViteConfigDependencyResolver
            {
                BuildEngine = mockEngine,
                ViteConfigurations = new[]
                {
                    CreateConfig("app", "vite.app.config.ts", "nonexistent")
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeFalse();
            mockEngine.LoggedErrors.Should().ContainSingle(e => 
                e.Message.Contains("depends on") && e.Message.Contains("nonexistent") && e.Message.Contains("does not exist"));
        }

        [Fact]
        public void Execute_WithMissingBuildId_LogsErrorAndReturnsFalse()
        {
            // Arrange
            var mockEngine = new Helpers.MockBuildEngine();
            var config = new TaskItem("vite.config.ts");
            // Don't set BuildId metadata
            
            var task = new ViteConfigDependencyResolver
            {
                BuildEngine = mockEngine,
                ViteConfigurations = new[] { config }
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeFalse(); // BuildId is required
            mockEngine.LoggedErrors.Should().ContainSingle(e => e.Message.Contains("missing BuildId"));
        }

        [Fact]
        public void Execute_WithDiamondDependency_ResolvesCorrectly()
        {
            // Arrange
            //      core
            //     /    \
            //   ui     data
            //     \    /
            //      app
            var task = new ViteConfigDependencyResolver
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ViteConfigurations = new[]
                {
                    CreateConfig("app", "vite.app.config.ts", "ui,data"),
                    CreateConfig("ui", "vite.ui.config.ts", "core"),
                    CreateConfig("data", "vite.data.config.ts", "core"),
                    CreateConfig("core", "vite.core.config.ts")
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.OrderedConfigurations.Should().HaveCount(4);
            
            // Core must be first
            task.OrderedConfigurations![0].GetMetadata("BuildId").Should().Be("core");
            
            // App must be last
            task.OrderedConfigurations[3].GetMetadata("BuildId").Should().Be("app");
            
            // ui and data should be in the middle (order may vary)
            var middle = new[] { 
                task.OrderedConfigurations[1].GetMetadata("BuildId"), 
                task.OrderedConfigurations[2].GetMetadata("BuildId") 
            };
            middle.Should().Contain("ui");
            middle.Should().Contain("data");
        }

        [Fact]
        public void Execute_WithWhitespaceInDependencies_HandlesCorrectly()
        {
            // Arrange
            var task = new ViteConfigDependencyResolver
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ViteConfigurations = new[]
                {
                    CreateConfig("app", "vite.app.config.ts", " shared , components "), // Extra whitespace
                    CreateConfig("shared", "vite.shared.config.ts"),
                    CreateConfig("components", "vite.components.config.ts")
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.OrderedConfigurations.Should().HaveCount(3);
            task.OrderedConfigurations![2].GetMetadata("BuildId").Should().Be("app");
        }

        [Fact]
        public void Execute_CreatesDependencyGroups_WithCorrectLevels()
        {
            // Arrange
            var task = new ViteConfigDependencyResolver
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ViteConfigurations = new[]
                {
                    CreateConfig("level3", "vite.level3.config.ts", "level2"),
                    CreateConfig("level2", "vite.level2.config.ts", "level1a,level1b"),
                    CreateConfig("level1a", "vite.level1a.config.ts"),
                    CreateConfig("level1b", "vite.level1b.config.ts")
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.DependencyGroups.Should().NotBeNull();
            task.DependencyGroups!.Length.Should().Be(3); // Three dependency levels
            
            // Group 0 (level 0) should contain level1a and level1b
            var group0Configs = task.DependencyGroups[0].GetMetadata("Configurations");
            group0Configs.Should().Contain("level1a");
            group0Configs.Should().Contain("level1b");
        }

        [Fact]
        public void Execute_WithEmptyDependsOn_TreatsAsNoDependencies()
        {
            // Arrange
            var task = new ViteConfigDependencyResolver
            {
                BuildEngine = new Helpers.MockBuildEngine(),
                ViteConfigurations = new[]
                {
                    CreateConfig("app", "vite.app.config.ts", ""),
                    CreateConfig("shared", "vite.shared.config.ts")
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            result.Should().BeTrue();
            task.OrderedConfigurations.Should().HaveCount(2);
            task.DependencyGroups![0].GetMetadata("Configurations").Should().Contain("app");
            task.DependencyGroups[0].GetMetadata("Configurations").Should().Contain("shared");
        }

        private static ITaskItem CreateConfig(string buildId, string configFile, string? dependsOn = null)
        {
            var item = new TaskItem(configFile);
            item.SetMetadata("BuildId", buildId);
            if (dependsOn != null)
            {
                item.SetMetadata("DependsOn", dependsOn);
            }
            return item;
        }
    }
}
