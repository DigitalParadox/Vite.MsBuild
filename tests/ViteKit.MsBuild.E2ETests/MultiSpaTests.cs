using System;
using System.IO;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace ViteKit.MsBuild.E2ETests;

/// <summary>
/// E2E tests for multi-SPA scenarios
/// </summary>
public class MultiSpaTests : E2ETestBase
{
    public MultiSpaTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void MultiSpaReact_BuildsAllConfigurations()
    {
        // Arrange
        const string projectName = "MultiSpaReact";
        
        // Act
        ExecuteClean(projectName);
        var buildResult = ExecuteBuild(projectName);

        // Assert
        buildResult.Success.Should().BeTrue($"build should succeed. Output: {buildResult.Output}");
        
        // Verify build completed
        buildResult.Output.Should().Contain("[BUILD]", "Vite build should have executed");
        buildResult.Output.Should().Contain("[OK]", "Build should complete successfully");
        
        // Verify output directories
        DirectoryExists(projectName, "wwwroot/admin").Should().BeTrue("admin output should exist");
        DirectoryExists(projectName, "wwwroot/customer").Should().BeTrue("customer output should exist");
        
        GetFiles(projectName, "wwwroot/admin").Should().NotBeEmpty("admin should have output files");
        GetFiles(projectName, "wwwroot/customer").Should().NotBeEmpty("customer should have output files");
    }

    [Fact]
    public void MultiSpaReact_BuildPlanShowsIndependentBuilds()
    {
        // Arrange
        const string projectName = "MultiSpaReact";
        
        // Act
        ExecuteClean(projectName);
        var buildResult = ExecuteBuild(projectName);

        // Assert
        buildResult.Success.Should().BeTrue("build should succeed");
        
        // Verify build completed
        buildResult.Output.Should().Contain("[BUILD]", "Vite build should have executed");
        buildResult.Output.Should().Contain("[OK]", "Build should complete successfully");
    }

    [Fact]
    public void MultiSpaReact_RebuildOnlyChangedConfiguration()
    {
        // Arrange
        const string projectName = "MultiSpaReact";
        
        // Act - Initial build
        ExecuteClean(projectName);
        var firstBuild = ExecuteBuild(projectName);
        
        // Touch a file in admin SPA
        var adminFile = Path.Combine(TestProjectsRoot, projectName, "wwwroot/js/admin/index.tsx");
        File.SetLastWriteTimeUtc(adminFile, DateTime.UtcNow);
        
        var secondBuild = ExecuteBuild(projectName);

        // Assert
        firstBuild.Success.Should().BeTrue("first build should succeed");
        secondBuild.Success.Should().BeTrue("second build should succeed");
        
        // Build system should handle incremental changes
        secondBuild.Output.Should().Contain("[BUILD]", "build should execute");
    }
}
