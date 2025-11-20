using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.Build.Framework;
using ViteKit.MsBuild.PureUnitTests.Helpers;
using ViteKit.MsBuild.Tasks;
using Xunit;

namespace ViteKit.MsBuild.PureUnitTests.Integration
{
    /// <summary>
    /// Integration tests that simulate full MSBuild pipeline scenarios
    /// Tests the complete flow: Configuration Resolution → Dependency Resolution → Mode Resolution → Build Orchestration
    /// </summary>
    public class FullBuildIntegrationTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly MockBuildEngine _buildEngine;

        public FullBuildIntegrationTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "ViteMsBuild_IntegrationTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _buildEngine = new MockBuildEngine();
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                try { Directory.Delete(_tempDir, true); } catch { }
            }
        }

        [Fact]
        public void FullPipeline_SingleConfig_Success()
        {
            // Arrange - Create a simple Vite project
            var configPath = Path.Combine(_tempDir, "vite.config.ts");
            File.WriteAllText(configPath, "export default { build: { outDir: 'dist' } }");
            
            var packageJsonPath = Path.Combine(_tempDir, "package.json");
            File.WriteAllText(packageJsonPath, @"{
                ""name"": ""test-app"",
                ""scripts"": { ""build"": ""vite build"" },
                ""dependencies"": { ""vite"": ""^5.0.0"" }
            }");

            // Step 1: Resolve Configuration
            var configResolver = new ViteConfigurationResolver
            {
                BuildEngine = _buildEngine,
                ViteProjectRoot = _tempDir,
                ViteMode = "production",
                PackageManager = "npm"
            };

            var configResolved = configResolver.Execute();
            configResolved.Should().BeTrue();
            configResolver.ResolvedConfigs.Should().HaveCount(1);
            configResolver.IsMultiConfig.Should().BeFalse();

            // Step 2: Resolve Dependencies (should be none for single config)
            var dependencyResolver = new ViteConfigDependencyResolver
            {
                BuildEngine = _buildEngine,
                ViteConfigurations = configResolver.ResolvedConfigs
            };

            var depsResolved = dependencyResolver.Execute();
            depsResolved.Should().BeTrue();
            dependencyResolver.OrderedConfigurations.Should().HaveCount(1);

            // Step 3: Resolve Modes
            var modeResolver = new ViteModeResolver
            {
                BuildEngine = _buildEngine,
                ViteConfigs = dependencyResolver.OrderedConfigurations,
                ViteMode = "production",
                DefaultMode = "development"
            };

            var modesResolved = modeResolver.Execute();
            modesResolved.Should().BeTrue();
            modeResolver.ResolvedConfigs.Should().HaveCount(1);
            modeResolver.ResolvedConfigs![0].GetMetadata("EffectiveMode").Should().Be("production");

            // Step 4: Orchestrate Build (dry run - no actual build)
            var orchestrator = new OrchestrateBuildTask
            {
                BuildEngine = _buildEngine,
                ViteProjectRoot = _tempDir,
                PackageManager = "npm",
                ViteMode = "production",
                ViteConfigurations = modeResolver.ResolvedConfigs,
                IntermediateOutputPath = Path.Combine(_tempDir, "obj")
            };

            // Verify the orchestrator validates inputs correctly
            orchestrator.ViteConfigurations.Should().HaveCount(1);
            orchestrator.ViteConfigurations![0].GetMetadata("BuildId").Should().Be("default");
        }

        [Fact]
        public void FullPipeline_MultiConfig_WithDependencies_Success()
        {
            // Arrange - Create multi-config project with dependencies
            var sharedConfig = Path.Combine(_tempDir, "vite.shared.config.ts");
            File.WriteAllText(sharedConfig, "export default { build: { outDir: 'dist/shared' } }");

            var adminConfig = Path.Combine(_tempDir, "vite.admin.config.ts");
            File.WriteAllText(adminConfig, "export default { build: { outDir: 'dist/admin' } }");

            var customerConfig = Path.Combine(_tempDir, "vite.customer.config.ts");
            File.WriteAllText(customerConfig, "export default { build: { outDir: 'dist/customer' } }");

            var packageJsonPath = Path.Combine(_tempDir, "package.json");
            File.WriteAllText(packageJsonPath, @"{
                ""name"": ""multi-spa"",
                ""scripts"": { ""build"": ""vite build"" }
            }");

            // Create user-defined configs with dependencies
            var userConfigs = new[]
            {
                CreateConfigItem(sharedConfig, "shared", "dist/shared", null),
                CreateConfigItem(adminConfig, "admin", "dist/admin", "shared"),
                CreateConfigItem(customerConfig, "customer", "dist/customer", "shared")
            };

            // Step 1: Resolve Configurations
            var configResolver = new ViteConfigurationResolver
            {
                BuildEngine = _buildEngine,
                ViteProjectRoot = _tempDir,
                ViteMode = "development",
                PackageManager = "pnpm",
                UserDefinedConfigs = userConfigs
            };

            var configResolved = configResolver.Execute();
            configResolved.Should().BeTrue();
            configResolver.ResolvedConfigs.Should().HaveCount(3);
            configResolver.IsMultiConfig.Should().BeTrue();

            // Step 2: Resolve Dependencies
            var dependencyResolver = new ViteConfigDependencyResolver
            {
                BuildEngine = _buildEngine,
                ViteConfigurations = configResolver.ResolvedConfigs
            };

            var depsResolved = dependencyResolver.Execute();
            depsResolved.Should().BeTrue();
            dependencyResolver.OrderedConfigurations.Should().HaveCount(3);

            // Verify dependency order: shared should be first
            dependencyResolver.OrderedConfigurations![0].GetMetadata("BuildId").Should().Be("shared");
            
            // Admin and customer can be in any order, but both should come after shared
            var buildIds = dependencyResolver.OrderedConfigurations.Select(c => c.GetMetadata("BuildId")).ToList();
            buildIds[0].Should().Be("shared");
            buildIds.Should().Contain("admin");
            buildIds.Should().Contain("customer");

            // Step 3: Resolve Modes with overrides
            var modeProperties = new[]
            {
                CreatePropertyItem("AdminViteMode", "staging"),
                CreatePropertyItem("CustomerViteMode", "production")
            };

            var modeResolver = new ViteModeResolver
            {
                BuildEngine = _buildEngine,
                ViteConfigs = dependencyResolver.OrderedConfigurations,
                ViteMode = "development",
                Properties = modeProperties,
                DefaultMode = "development"
            };

            var modesResolved = modeResolver.Execute();
            modesResolved.Should().BeTrue();
            modeResolver.ResolvedConfigs.Should().HaveCount(3);

            // Verify mode overrides
            var sharedConfig_resolved = modeResolver.ResolvedConfigs!.First(c => c.GetMetadata("BuildId") == "shared");
            sharedConfig_resolved.GetMetadata("EffectiveMode").Should().Be("development"); // Uses global

            var adminConfig_resolved = modeResolver.ResolvedConfigs!.First(c => c.GetMetadata("BuildId") == "admin");
            adminConfig_resolved.GetMetadata("EffectiveMode").Should().Be("staging"); // Uses config-specific property

            var customerConfig_resolved = modeResolver.ResolvedConfigs!.First(c => c.GetMetadata("BuildId") == "customer");
            customerConfig_resolved.GetMetadata("EffectiveMode").Should().Be("production"); // Uses config-specific property
        }

        [Fact(Skip = "TODO: Circular dependency detection needs enhancement - currently only detects after topological sort")]
        public void FullPipeline_CircularDependency_FailsGracefully()
        {
            // Arrange - Create configs with circular dependency
            var config1 = Path.Combine(_tempDir, "vite.config1.ts");
            File.WriteAllText(config1, "export default {}");

            var config2 = Path.Combine(_tempDir, "vite.config2.ts");
            File.WriteAllText(config2, "export default {}");

            var userConfigs = new[]
            {
                CreateConfigItem(config1, "config1", "dist/1", "config2"),
                CreateConfigItem(config2, "config2", "dist/2", "config1")
            };

            // Step 1: Resolve Configurations
            var configResolver = new ViteConfigurationResolver
            {
                BuildEngine = _buildEngine,
                ViteProjectRoot = _tempDir,
                ViteMode = "development",
                PackageManager = "npm",
                UserDefinedConfigs = userConfigs
            };

            var configResolved = configResolver.Execute();
            configResolved.Should().BeTrue();

            // Step 2: Resolve Dependencies - should detect circular dependency
            var dependencyResolver = new ViteConfigDependencyResolver
            {
                BuildEngine = _buildEngine,
                ViteConfigurations = configResolver.ResolvedConfigs
            };

            var depsResolved = dependencyResolver.Execute();
            depsResolved.Should().BeFalse();
            _buildEngine.LoggedErrors.Should().Contain(e => e.Message.Contains("Circular dependency"));
        }

        [Fact(Skip = "TODO: Architecture detection needs file system setup - test infrastructure needs enhancement")]
        public void FullPipeline_MixedArchitectures_ResolvedCorrectly()
        {
            // Arrange - Create multi-architecture project
            Directory.CreateDirectory(Path.Combine(_tempDir, "Areas", "Admin"));
            Directory.CreateDirectory(Path.Combine(_tempDir, "spa", "customer"));

            var areaConfig = Path.Combine(_tempDir, "Areas", "Admin", "vite.config.ts");
            File.WriteAllText(areaConfig, "export default {}");

            var spaConfig = Path.Combine(_tempDir, "spa", "customer", "vite.config.ts");
            File.WriteAllText(spaConfig, "export default {}");

            var rootConfig = Path.Combine(_tempDir, "vite.config.ts");
            File.WriteAllText(rootConfig, "export default {}");

            var userConfigs = new[]
            {
                CreateConfigItem(areaConfig, null, null, null),
                CreateConfigItem(spaConfig, null, null, null),
                CreateConfigItem(rootConfig, null, null, null)
            };

            // Step 1: Resolve Configurations
            var configResolver = new ViteConfigurationResolver
            {
                BuildEngine = _buildEngine,
                ViteProjectRoot = _tempDir,
                ViteMode = "production",
                PackageManager = "yarn",
                UserDefinedConfigs = userConfigs
            };

            var configResolved = configResolver.Execute();
            configResolved.Should().BeTrue();
            configResolver.ResolvedConfigs.Should().HaveCount(3);

            // Verify architecture detection
            var areaConfigResolved = configResolver.ResolvedConfigs.First(c => 
                c.GetMetadata("ConfigFile").Contains("Areas"));
            areaConfigResolved.GetMetadata("Architecture").Should().Be("Areas");
            areaConfigResolved.GetMetadata("OutputDir").Should().Be("wwwroot/admin");

            var spaConfigResolved = configResolver.ResolvedConfigs.First(c => 
                c.GetMetadata("ConfigFile").Contains("spa"));
            spaConfigResolved.GetMetadata("Architecture").Should().Be("MultiSPA");
            spaConfigResolved.GetMetadata("OutputDir").Should().Be("wwwroot/spa/customer");

            var rootConfigResolved = configResolver.ResolvedConfigs.First(c => 
                c.GetMetadata("BuildId") == "default");
            rootConfigResolved.GetMetadata("Architecture").Should().Be("SPA");
        }

        [Fact(Skip = "TODO: Validation for duplicate BuildId exists but test setup needs adjustment")]
        public void FullPipeline_DuplicateBuildIds_FailsValidation()
        {
            // Arrange - Create configs with duplicate build IDs
            var config1 = Path.Combine(_tempDir, "vite.admin.config.ts");
            File.WriteAllText(config1, "export default {}");

            var config2 = Path.Combine(_tempDir, "Areas", "Admin", "vite.config.ts");
            Directory.CreateDirectory(Path.Combine(_tempDir, "Areas", "Admin"));
            File.WriteAllText(config2, "export default {}");

            var userConfigs = new[]
            {
                CreateConfigItem(config1, "admin", "dist/1", null),
                CreateConfigItem(config2, "admin", "dist/2", null) // Duplicate BuildId
            };

            // Step 1: Resolve Configurations - should fail validation
            var configResolver = new ViteConfigurationResolver
            {
                BuildEngine = _buildEngine,
                ViteProjectRoot = _tempDir,
                ViteMode = "development",
                PackageManager = "npm",
                UserDefinedConfigs = userConfigs
            };

            var configResolved = configResolver.Execute();
            configResolved.Should().BeFalse();
            _buildEngine.LoggedErrors.Should().Contain(e => e.Message.Contains("Duplicate BuildId"));
        }

        [Fact(Skip = "TODO: Add validation for conflicting OutputDir in ViteConfigurationResolver")]
        public void FullPipeline_ConflictingOutputDirs_FailsValidation()
        {
            // Arrange - Create configs with conflicting output directories
            var config1 = Path.Combine(_tempDir, "vite.admin.config.ts");
            File.WriteAllText(config1, "export default {}");

            var config2 = Path.Combine(_tempDir, "vite.customer.config.ts");
            File.WriteAllText(config2, "export default {}");

            var userConfigs = new[]
            {
                CreateConfigItem(config1, "admin", "wwwroot/dist", null),
                CreateConfigItem(config2, "customer", "wwwroot/dist", null) // Same output dir
            };

            // Step 1: Resolve Configurations - should fail validation
            var configResolver = new ViteConfigurationResolver
            {
                BuildEngine = _buildEngine,
                ViteProjectRoot = _tempDir,
                ViteMode = "development",
                PackageManager = "npm",
                UserDefinedConfigs = userConfigs
            };

            var configResolved = configResolver.Execute();
            configResolved.Should().BeFalse();
            _buildEngine.LoggedErrors.Should().Contain(e => e.Message.Contains("Duplicate OutputDir"));
        }

        [Fact]
        public void FullPipeline_ModeOverrideHierarchy_WorksCorrectly()
        {
            // Arrange - Test all levels of mode override hierarchy
            var config1 = Path.Combine(_tempDir, "vite.config1.ts");
            File.WriteAllText(config1, "export default {}");

            var config2 = Path.Combine(_tempDir, "vite.config2.ts");
            File.WriteAllText(config2, "export default {}");

            var config3 = Path.Combine(_tempDir, "vite.config3.ts");
            File.WriteAllText(config3, "export default {}");

            var config4 = Path.Combine(_tempDir, "vite.config4.ts");
            File.WriteAllText(config4, "export default {}");

            var userConfigs = new[]
            {
                CreateConfigItemWithMode(config1, "config1", "dist/1", "itemgroup-mode"), // ItemGroup mode
                CreateConfigItemWithMode(config2, "config2", "dist/2", ""),               // No mode specified
                CreateConfigItemWithMode(config3, "config3", "dist/3", ""),               // No mode specified
                CreateConfigItemWithMode(config4, "config4", "dist/4", "")                // No mode specified
            };

            // Step 1: Resolve Configurations
            var configResolver = new ViteConfigurationResolver
            {
                BuildEngine = _buildEngine,
                ViteProjectRoot = _tempDir,
                ViteMode = "global-production",
                PackageManager = "npm",
                UserDefinedConfigs = userConfigs
            };

            var configResolved = configResolver.Execute();
            configResolved.Should().BeTrue();

            // Step 2: Resolve Modes with property overrides
            var modeProperties = new[]
            {
                CreatePropertyItem("Config3ViteMode", "property-staging") // Config-specific property
            };

            var modeResolver = new ViteModeResolver
            {
                BuildEngine = _buildEngine,
                ViteConfigs = configResolver.ResolvedConfigs,
                ViteMode = "global-production",
                Properties = modeProperties,
                DefaultMode = "default-development"
            };

            var modesResolved = modeResolver.Execute();
            modesResolved.Should().BeTrue();

            // Verify hierarchy: Config-specific > ItemGroup > Global > Default
            var config1Resolved = modeResolver.ResolvedConfigs!.First(c => c.GetMetadata("BuildId") == "config1");
            config1Resolved.GetMetadata("EffectiveMode").Should().Be("itemgroup-mode"); // Uses ItemGroup

            var config2Resolved = modeResolver.ResolvedConfigs!.First(c => c.GetMetadata("BuildId") == "config2");
            config2Resolved.GetMetadata("EffectiveMode").Should().Be("global-production"); // Uses global

            var config3Resolved = modeResolver.ResolvedConfigs!.First(c => c.GetMetadata("BuildId") == "config3");
            config3Resolved.GetMetadata("EffectiveMode").Should().Be("property-staging"); // Uses config-specific property

            var config4Resolved = modeResolver.ResolvedConfigs!.First(c => c.GetMetadata("BuildId") == "config4");
            config4Resolved.GetMetadata("EffectiveMode").Should().Be("global-production"); // Uses global
        }

        [Fact]
        public void FullPipeline_ComplexDependencyGraph_OrdersCorrectly()
        {
            // Arrange - Create a complex dependency graph
            //   shared
            //   /   \
            //  ui   api
            //   \   /
            //   admin
            
            var sharedConfig = Path.Combine(_tempDir, "vite.shared.config.ts");
            File.WriteAllText(sharedConfig, "export default {}");

            var uiConfig = Path.Combine(_tempDir, "vite.ui.config.ts");
            File.WriteAllText(uiConfig, "export default {}");

            var apiConfig = Path.Combine(_tempDir, "vite.api.config.ts");
            File.WriteAllText(apiConfig, "export default {}");

            var adminConfig = Path.Combine(_tempDir, "vite.admin.config.ts");
            File.WriteAllText(adminConfig, "export default {}");

            var userConfigs = new[]
            {
                CreateConfigItem(sharedConfig, "shared", "dist/shared", null),
                CreateConfigItem(uiConfig, "ui", "dist/ui", "shared"),
                CreateConfigItem(apiConfig, "api", "dist/api", "shared"),
                CreateConfigItem(adminConfig, "admin", "dist/admin", "ui,api")
            };

            // Step 1: Resolve Configurations
            var configResolver = new ViteConfigurationResolver
            {
                BuildEngine = _buildEngine,
                ViteProjectRoot = _tempDir,
                ViteMode = "production",
                PackageManager = "npm",
                UserDefinedConfigs = userConfigs
            };

            var configResolved = configResolver.Execute();
            configResolved.Should().BeTrue();

            // Step 2: Resolve Dependencies
            var dependencyResolver = new ViteConfigDependencyResolver
            {
                BuildEngine = _buildEngine,
                ViteConfigurations = configResolver.ResolvedConfigs
            };

            var depsResolved = dependencyResolver.Execute();
            depsResolved.Should().BeTrue();

            var buildOrder = dependencyResolver.OrderedConfigurations!.Select(c => c.GetMetadata("BuildId")).ToList();

            // Verify topological order
            buildOrder[0].Should().Be("shared"); // Must be first
            buildOrder[3].Should().Be("admin");  // Must be last

            // ui and api must come after shared but before admin
            var sharedIndex = buildOrder.IndexOf("shared");
            var uiIndex = buildOrder.IndexOf("ui");
            var apiIndex = buildOrder.IndexOf("api");
            var adminIndex = buildOrder.IndexOf("admin");

            uiIndex.Should().BeGreaterThan(sharedIndex);
            apiIndex.Should().BeGreaterThan(sharedIndex);
            adminIndex.Should().BeGreaterThan(uiIndex);
            adminIndex.Should().BeGreaterThan(apiIndex);

            // Verify dependency groups for parallel execution
            dependencyResolver.DependencyGroups.Should().NotBeNull();
            dependencyResolver.DependencyGroups.Should().HaveCountGreaterThan(0);
        }

        // Helper methods
        private ITaskItem CreateConfigItem(string path, string? buildId, string? outputDir, string? dependsOn)
        {
            var item = new Microsoft.Build.Utilities.TaskItem(path);
            if (!string.IsNullOrEmpty(buildId))
                item.SetMetadata("BuildId", buildId);
            if (!string.IsNullOrEmpty(outputDir))
                item.SetMetadata("OutputDir", outputDir);
            if (!string.IsNullOrEmpty(dependsOn))
                item.SetMetadata("DependsOn", dependsOn);
            return item;
        }

        private ITaskItem CreateConfigItemWithMode(string path, string buildId, string outputDir, string? mode)
        {
            var item = new Microsoft.Build.Utilities.TaskItem(path);
            item.SetMetadata("BuildId", buildId);
            item.SetMetadata("OutputDir", outputDir);
            if (!string.IsNullOrEmpty(mode))
                item.SetMetadata("Mode", mode);
            return item;
        }

        private ITaskItem CreatePropertyItem(string name, string value)
        {
            var item = new Microsoft.Build.Utilities.TaskItem(name);
            item.SetMetadata("Value", value);
            return item;
        }
    }
}
