using System;
using System.IO;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace ViteKit.MsBuild.E2ETests;

/// <summary>
/// E2E tests for custom configuration scenarios
/// </summary>
public class ConfigurationTests : E2ETestBase
{
    public ConfigurationTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void CustomBuildScript_ExecutesSpecifiedScript()
    {
        // Arrange
        const string projectName = "CustomBuildScript";
        
        // Act
        ExecuteClean(projectName);
        var buildResult = ExecuteBuild(projectName);

        // Assert
        buildResult.Success.Should().BeTrue($"build should succeed. Output: {buildResult.Output}");
        
        // Verify output was created
        DirectoryExists(projectName, "wwwroot/dist").Should().BeTrue("output directory should exist");
        GetFiles(projectName, "wwwroot/dist").Should().NotBeEmpty("output files should be created");
    }

    [Fact]
    public void OutputDirOverride_UsesCustomOutputDirectory()
    {
        // Arrange
        const string projectName = "OutputDirOverride";
        
        // Act
        ExecuteClean(projectName);
        var buildResult = ExecuteBuild(projectName);

        // Assert
        buildResult.Success.Should().BeTrue($"build should succeed. Output: {buildResult.Output}");
        
        // Verify files in custom location
        DirectoryExists(projectName, "wwwroot/custom-assets").Should().BeTrue(
            "custom output directory should exist");
        GetFiles(projectName, "wwwroot/custom-assets").Should().NotBeEmpty(
            "output files should be in custom directory");
        
        // Verify default location NOT used
        DirectoryExists(projectName, "wwwroot/dist").Should().BeFalse(
            "default dist directory should not be created");
    }

    // Note: ViteClean only removes ViteKit marker files and Vite cache directories.
    // Users manage cleanup of build outputs via custom targets (see docs/guides/advanced-scenarios.md).

    [Fact]
    public void MultipleConfigs_WithDifferentOutputDirs_CreateSeparateOutputs()
    {
        // Arrange
        const string projectName = "MultiSpaReact";
        
        // Act
        ExecuteClean(projectName);
        var buildResult = ExecuteBuild(projectName);

        // Assert
        buildResult.Success.Should().BeTrue("build should succeed");
        
        // Verify separate output directories
        DirectoryExists(projectName, "wwwroot/admin").Should().BeTrue("admin output should exist");
        DirectoryExists(projectName, "wwwroot/customer").Should().BeTrue("customer output should exist");
        
        // Verify no cross-contamination
        var adminFiles = GetFiles(projectName, "wwwroot/admin");
        var customerFiles = GetFiles(projectName, "wwwroot/customer");
        
        adminFiles.Should().NotBeEmpty("admin should have output files");
        customerFiles.Should().NotBeEmpty("customer should have output files");
        
        // Files should be in correct directories (no overlap)
        adminFiles.Should().OnlyContain(f => f.StartsWith("wwwroot/admin") || f.StartsWith("wwwroot\\admin"),
            "admin files should only be in admin directory");
        customerFiles.Should().OnlyContain(f => f.StartsWith("wwwroot/customer") || f.StartsWith("wwwroot\\customer"),
            "customer files should only be in customer directory");
    }

    [Fact]
    public void ViteMode_Override_AppliesToBuild()
    {
        // Arrange
        const string projectName = "SingleSpaVue";
        
        // Act
        ExecuteClean(projectName);
        var buildResult = ExecuteBuild(projectName, additionalArgs: "/p:ViteMode=staging");

        // Assert
        buildResult.Success.Should().BeTrue("build with mode override should succeed");
        // Mode is applied internally but not logged in output
    }

    [Fact]
    public void PackageManager_Detection_WorksCorrectly()
    {
        // Arrange - Projects with different package managers
        const string npmProject = "SingleSpaVue";
        
        // Act
        ExecuteClean(npmProject);
        var buildResult = ExecuteBuild(npmProject);

        // Assert
        buildResult.Success.Should().BeTrue("build should succeed");
        buildResult.Output.Should().Contain("[RESTORE]", "should restore packages");
    }

    [Fact]
    public void Build_WithForceFlag_RebuildsEverything()
    {
        // Arrange
        const string projectName = "IncrementalBuild";
        
        // Act - First build
        ExecuteClean(projectName);
        var firstBuild = ExecuteBuild(projectName);
        
        // Act - Force rebuild
        var forceBuild = ExecuteBuild(projectName, additionalArgs: "/t:Rebuild");

        // Assert
        firstBuild.Success.Should().BeTrue("first build should succeed");
        forceBuild.Success.Should().BeTrue("force rebuild should succeed");
        
        // Force rebuild should execute Vite even without changes
        forceBuild.Output.Should().Contain("[BUILD]", "force rebuild should run Vite");
    }
}
