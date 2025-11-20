using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;
using FluentAssertions;
using ViteKit.MsBuild.Tasks;

namespace Vite.MsBuild.PureUnitTests
{
    /// <summary>
    /// Enterprise Multi-SPA validation tests - showing how complex enterprise
    /// configurations are validated with pure unit tests
    /// </summary>
    public class EnterpriseMultiSpaTests : IDisposable
    {
        private readonly List<string> _tempDirectories = new();

        [Fact]
        public void Should_Support_Complete_Enterprise_Multi_SPA_Configuration()
        {
            // Arrange: Enterprise project with multiple SPAs
            var projectDir = CreateEnterpriseProject();

            // Act: Build commands for each SPA with different configurations
            var adminConfig = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Pnpm,
                ConfigFile = "Areas/Admin/vite.admin.config.ts",
                Mode = "development",  // AdminViteMode override
                OutputDir = "wwwroot/admin",
                LogLevel = "info"
            };
            var adminCommand = new ViteCommandBuilder(adminConfig).Build();

            var customerConfig = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Pnpm,
                ConfigFile = "Areas/Customer/vite.customer.config.ts",
                Mode = "production",  // CustomerViteMode override
                OutputDir = "wwwroot/customer",
                LogLevel = "info"
            };
            var customerCommand = new ViteCommandBuilder(customerConfig).Build();

            var partnerConfig = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Pnpm,
                ConfigFile = "Areas/Partner/vite.partner.config.ts",
                Mode = "staging",  // PartnerViteMode override
                OutputDir = "wwwroot/partner",
                LogLevel = "info"
            };
            var partnerCommand = new ViteCommandBuilder(partnerConfig).Build();

            var usersConfig = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Pnpm,
                ConfigFile = "Areas/Users/vite.users.config.ts",
                Mode = "development",  // Global ViteMode fallback
                OutputDir = "wwwroot/users",
                LogLevel = "info"
            };
            var usersCommand = new ViteCommandBuilder(usersConfig).Build();

            // Assert: Each SPA has correct configuration
            // Admin Dashboard (Vue.js) - Development mode
            adminCommand.ConfigPath.Should().Be("Areas/Admin/vite.admin.config.ts");
            adminCommand.Mode.Should().Be("development");
            adminCommand.OutputDir.Should().Be("wwwroot/admin");
            adminCommand.ToString().Should().Contain("--config \"Areas/Admin/vite.admin.config.ts\"");
            adminCommand.ToString().Should().Contain("--mode development");

            // Customer Portal (React) - Production mode
            customerCommand.ConfigPath.Should().Be("Areas/Customer/vite.customer.config.ts");
            customerCommand.Mode.Should().Be("production");
            customerCommand.OutputDir.Should().Be("wwwroot/customer");
            customerCommand.ToString().Should().Contain("--mode production");

            // Partner Interface (Svelte) - Staging mode
            partnerCommand.ConfigPath.Should().Be("Areas/Partner/vite.partner.config.ts");
            partnerCommand.Mode.Should().Be("staging");
            partnerCommand.OutputDir.Should().Be("wwwroot/partner");
            partnerCommand.ToString().Should().Contain("--mode staging");

            // User Management (Angular) - Development mode (fallback)
            usersCommand.ConfigPath.Should().Be("Areas/Users/vite.users.config.ts");
            usersCommand.Mode.Should().Be("development");
            usersCommand.OutputDir.Should().Be("wwwroot/users");

            // All use same package manager
            adminCommand.Executable.Should().Be("pnpm");
            customerCommand.Executable.Should().Be("pnpm");
            partnerCommand.Executable.Should().Be("pnpm");
            usersCommand.Executable.Should().Be("pnpm");

            // All use package scripts (not direct fallback)
            adminCommand.Command.Should().Be("run build");
            customerCommand.Command.Should().Be("run build");  // PNPM uses "run build" - test fixed!
            partnerCommand.Command.Should().Be("run build");
            usersCommand.Command.Should().Be("run build");
        }

        [Fact]
        public void Should_Support_Command_Line_Override_For_All_Configs()
        {
            // Arrange: Enterprise project
            var projectDir = CreateEnterpriseProject();

            // Act: Simulate command line override: dotnet build -p:ViteMode=production
            // (All configs should use production regardless of individual settings)
            var globalMode = "production";  // Simulates -p:ViteMode=production

            var adminConfig = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Pnpm,
                ConfigFile = "Areas/Admin/vite.admin.config.ts",
                Mode = globalMode  // Global override
            };
            var adminCommand = new ViteCommandBuilder(adminConfig).Build();

            var customerConfig = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Pnpm,
                ConfigFile = "Areas/Customer/vite.customer.config.ts",
                Mode = globalMode  // Global override
            };
            var customerCommand = new ViteCommandBuilder(customerConfig).Build();

            var partnerConfig = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Pnpm,
                ConfigFile = "Areas/Partner/vite.partner.config.ts",
                Mode = globalMode  // Global override
            };
            var partnerCommand = new ViteCommandBuilder(partnerConfig).Build();

            // Assert: All configs use production mode (global override wins)
            adminCommand.Mode.Should().Be("production");
            customerCommand.Mode.Should().Be("production");
            partnerCommand.Mode.Should().Be("production");

            adminCommand.ToString().Should().Contain("--mode production");
            customerCommand.ToString().Should().Contain("--mode production");
            partnerCommand.ToString().Should().Contain("--mode production");
        }

        [Fact]
        public void Should_Support_Mixed_Override_Scenarios()
        {
            // Arrange: Enterprise project
            var projectDir = CreateEnterpriseProject();

            // Act: Simulate mixed overrides
            // dotnet build -p:CustomerViteMode=staging -p:PartnerViteMode=production
            // (Admin uses project default, Customer/Partner use command line specific)

            var adminConfig = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Pnpm,
                ConfigFile = "Areas/Admin/vite.admin.config.ts",
                Mode = "development"  // AdminViteMode project setting
            };
            var adminCommand = new ViteCommandBuilder(adminConfig).Build();

            var customerConfig = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Pnpm,
                ConfigFile = "Areas/Customer/vite.customer.config.ts",
                Mode = "staging"  // CustomerViteMode command line override
            };
            var customerCommand = new ViteCommandBuilder(customerConfig).Build();

            var partnerConfig = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Pnpm,
                ConfigFile = "Areas/Partner/vite.partner.config.ts",
                Mode = "production"  // PartnerViteMode command line override
            };
            var partnerCommand = new ViteCommandBuilder(partnerConfig).Build();

            // Assert: Each config uses correct override level
            adminCommand.Mode.Should().Be("development");  // Project default
            customerCommand.Mode.Should().Be("staging");   // Command line specific
            partnerCommand.Mode.Should().Be("production"); // Command line specific
        }

        [Fact]
        public void Should_Support_Different_Package_Managers_Per_SPA()
        {
            // Arrange: Complex scenario where different SPAs might use different tools
            var projectDir = CreateEnterpriseProject();

            // Act: Different SPAs with different package managers
            var adminConfig = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Pnpm,
                ConfigFile = "Areas/Admin/vite.admin.config.ts",
                Mode = "development"
            };
            var adminCommand = new ViteCommandBuilder(adminConfig).Build();

            var customerConfig = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Yarn,
                ConfigFile = "Areas/Customer/vite.customer.config.ts",
                Mode = "production"
            };
            var customerCommand = new ViteCommandBuilder(customerConfig).Build();

            var partnerConfig = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Bun,
                ConfigFile = "Areas/Partner/vite.partner.config.ts",
                Mode = "staging"
            };
            var partnerCommand = new ViteCommandBuilder(partnerConfig).Build();

            // Assert: Each SPA can use different package manager
            adminCommand.Executable.Should().Be("pnpm");
            customerCommand.Executable.Should().Be("yarn");
            partnerCommand.Executable.Should().Be("bun");  // Bun executable is "bun", not "bunx"

            adminCommand.Command.Should().Be("run build");
            customerCommand.Command.Should().Be("build");  // Yarn can run scripts directly
            partnerCommand.Command.Should().Be("run build");
        }

        [Fact]
        public void Should_Support_Environment_Specific_Builds()
        {
            // Arrange: Enterprise project
            var projectDir = CreateEnterpriseProject();

            // Act: Environment-specific configurations
            var devAdminConfig = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Pnpm,
                ConfigFile = "Areas/Admin/vite.admin.config.ts",
                Mode = "development"
            };
            devAdminConfig.Environment["NODE_ENV"] = "development";
            devAdminConfig.Environment["VITE_API_URL"] = "https://api.dev.company.com";
            devAdminConfig.Environment["VITE_ADMIN_FEATURES"] = "debug,analytics";
            var devAdminCommand = new ViteCommandBuilder(devAdminConfig).Build();

            var prodCustomerConfig = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Pnpm,
                ConfigFile = "Areas/Customer/vite.customer.config.ts",
                Mode = "production"
            };
            prodCustomerConfig.Environment["NODE_ENV"] = "production";
            prodCustomerConfig.Environment["VITE_API_URL"] = "https://api.prod.company.com";
            prodCustomerConfig.Environment["VITE_ENABLE_TRACKING"] = "true";
            var prodCustomerCommand = new ViteCommandBuilder(prodCustomerConfig).Build();

            var stagingPartnerConfig = new ViteBuildConfiguration
            {
                ProjectRoot = projectDir,
                PackageManager = PackageManager.Pnpm,
                ConfigFile = "Areas/Partner/vite.partner.config.ts",
                Mode = "staging"
            };
            stagingPartnerConfig.Environment["NODE_ENV"] = "staging";
            stagingPartnerConfig.Environment["VITE_API_URL"] = "https://api.staging.company.com";
            stagingPartnerConfig.Environment["VITE_PARTNER_MODE"] = "sandbox";
            var stagingPartnerCommand = new ViteCommandBuilder(stagingPartnerConfig).Build();

            // Assert: Environment variables properly configured
            devAdminCommand.Environment["NODE_ENV"].Should().Be("development");
            devAdminCommand.Environment["VITE_API_URL"].Should().Be("https://api.dev.company.com");
            devAdminCommand.Environment["VITE_ADMIN_FEATURES"].Should().Be("debug,analytics");

            prodCustomerCommand.Environment["NODE_ENV"].Should().Be("production");
            prodCustomerCommand.Environment["VITE_API_URL"].Should().Be("https://api.prod.company.com");
            prodCustomerCommand.Environment["VITE_ENABLE_TRACKING"].Should().Be("true");

            stagingPartnerCommand.Environment["NODE_ENV"].Should().Be("staging");
            stagingPartnerCommand.Environment["VITE_API_URL"].Should().Be("https://api.staging.company.com");
            stagingPartnerCommand.Environment["VITE_PARTNER_MODE"].Should().Be("sandbox");
        }

        // Helper method to create enterprise project
        private string CreateEnterpriseProject()
        {
            var projectDir = CreateTempProject();
            
            // Create package.json with build script
            var packageJson = new
            {
                name = "enterprise-multi-spa",
                version = "1.0.0",
                scripts = new Dictionary<string, string>
                {
                    ["build"] = "vite build",
                    ["build:admin"] = "vite build --config Areas/Admin/vite.admin.config.ts",
                    ["build:customer"] = "vite build --config Areas/Customer/vite.customer.config.ts",
                    ["build:partner"] = "vite build --config Areas/Partner/vite.partner.config.ts",
                    ["build:users"] = "vite build --config Areas/Users/vite.users.config.ts",
                    ["build:all"] = "npm run build:admin && npm run build:customer && npm run build:partner && npm run build:users"
                },
                workspaces = new[] { "Areas/*" }
            };

            var json = JsonSerializer.Serialize(packageJson, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(projectDir, "package.json"), json);

            return projectDir;
        }

        private string CreateTempProject()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"EnterpriseTest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            _tempDirectories.Add(tempDir);
            return tempDir;
        }

        public void Dispose()
        {
            foreach (var tempDir in _tempDirectories)
            {
                if (Directory.Exists(tempDir))
                {
                    try
                    {
                        Directory.Delete(tempDir, true);
                    }
                    catch
                    {
                        // Ignore cleanup failures in tests
                    }
                }
            }
        }
    }
}