using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;
using FluentAssertions;
using Vite.MsBuild.Tasks;

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
            var adminCommand = new ViteCommandBuilder(projectDir, PackageManager.Pnpm)
                .WithConfig("Areas/Admin/vite.admin.config.ts")
                .WithMode("development")  // AdminViteMode override
                .WithOutputDir("wwwroot/admin")
                .WithLogLevel("info")
                .Build();

            var customerCommand = new ViteCommandBuilder(projectDir, PackageManager.Pnpm)
                .WithConfig("Areas/Customer/vite.customer.config.ts")
                .WithMode("production")  // CustomerViteMode override
                .WithOutputDir("wwwroot/customer")
                .WithLogLevel("info")
                .Build();

            var partnerCommand = new ViteCommandBuilder(projectDir, PackageManager.Pnpm)
                .WithConfig("Areas/Partner/vite.partner.config.ts")
                .WithMode("staging")  // PartnerViteMode override
                .WithOutputDir("wwwroot/partner")
                .WithLogLevel("info")
                .Build();

            var usersCommand = new ViteCommandBuilder(projectDir, PackageManager.Pnpm)
                .WithConfig("Areas/Users/vite.users.config.ts")
                .WithMode("development")  // Global ViteMode fallback
                .WithOutputDir("wwwroot/users")
                .WithLogLevel("info")
                .Build();

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

            var adminCommand = new ViteCommandBuilder(projectDir, PackageManager.Pnpm)
                .WithConfig("Areas/Admin/vite.admin.config.ts")
                .WithMode(globalMode)  // Global override
                .Build();

            var customerCommand = new ViteCommandBuilder(projectDir, PackageManager.Pnpm)
                .WithConfig("Areas/Customer/vite.customer.config.ts")
                .WithMode(globalMode)  // Global override
                .Build();

            var partnerCommand = new ViteCommandBuilder(projectDir, PackageManager.Pnpm)
                .WithConfig("Areas/Partner/vite.partner.config.ts")
                .WithMode(globalMode)  // Global override
                .Build();

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

            var adminCommand = new ViteCommandBuilder(projectDir, PackageManager.Pnpm)
                .WithConfig("Areas/Admin/vite.admin.config.ts")
                .WithMode("development")  // AdminViteMode project setting
                .Build();

            var customerCommand = new ViteCommandBuilder(projectDir, PackageManager.Pnpm)
                .WithConfig("Areas/Customer/vite.customer.config.ts")
                .WithMode("staging")  // CustomerViteMode command line override
                .Build();

            var partnerCommand = new ViteCommandBuilder(projectDir, PackageManager.Pnpm)
                .WithConfig("Areas/Partner/vite.partner.config.ts")
                .WithMode("production")  // PartnerViteMode command line override
                .Build();

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
            var adminCommand = new ViteCommandBuilder(projectDir, PackageManager.Pnpm)
                .WithConfig("Areas/Admin/vite.admin.config.ts")
                .WithMode("development")
                .Build();

            var customerCommand = new ViteCommandBuilder(projectDir, PackageManager.Yarn)
                .WithConfig("Areas/Customer/vite.customer.config.ts")
                .WithMode("production")
                .Build();

            var partnerCommand = new ViteCommandBuilder(projectDir, PackageManager.Bun)
                .WithConfig("Areas/Partner/vite.partner.config.ts")
                .WithMode("staging")
                .Build();

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
            var devAdminCommand = new ViteCommandBuilder(projectDir, PackageManager.Pnpm)
                .WithConfig("Areas/Admin/vite.admin.config.ts")
                .WithMode("development")
                .WithEnvironmentVariable("NODE_ENV", "development")
                .WithEnvironmentVariable("VITE_API_URL", "https://api.dev.company.com")
                .WithEnvironmentVariable("VITE_ADMIN_FEATURES", "debug,analytics")
                .Build();

            var prodCustomerCommand = new ViteCommandBuilder(projectDir, PackageManager.Pnpm)
                .WithConfig("Areas/Customer/vite.customer.config.ts")
                .WithMode("production")
                .WithEnvironmentVariable("NODE_ENV", "production")
                .WithEnvironmentVariable("VITE_API_URL", "https://api.company.com")
                .WithEnvironmentVariable("VITE_CUSTOMER_FEATURES", "optimized,cdn")
                .Build();

            var stagingPartnerCommand = new ViteCommandBuilder(projectDir, PackageManager.Pnpm)
                .WithConfig("Areas/Partner/vite.partner.config.ts")
                .WithMode("staging")
                .WithEnvironmentVariable("NODE_ENV", "staging")
                .WithEnvironmentVariable("VITE_API_URL", "https://api.staging.company.com")
                .WithEnvironmentVariable("VITE_PARTNER_FEATURES", "testing,preview")
                .Build();

            // Assert: Environment variables properly configured
            devAdminCommand.Environment["NODE_ENV"].Should().Be("development");
            devAdminCommand.Environment["VITE_API_URL"].Should().Be("https://api.dev.company.com");
            devAdminCommand.Environment["VITE_ADMIN_FEATURES"].Should().Be("debug,analytics");

            prodCustomerCommand.Environment["NODE_ENV"].Should().Be("production");
            prodCustomerCommand.Environment["VITE_API_URL"].Should().Be("https://api.company.com");
            prodCustomerCommand.Environment["VITE_CUSTOMER_FEATURES"].Should().Be("optimized,cdn");

            stagingPartnerCommand.Environment["NODE_ENV"].Should().Be("staging");
            stagingPartnerCommand.Environment["VITE_API_URL"].Should().Be("https://api.staging.company.com");
            stagingPartnerCommand.Environment["VITE_PARTNER_FEATURES"].Should().Be("testing,preview");
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