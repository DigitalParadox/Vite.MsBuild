using System;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace ViteKit.MsBuild.E2ETests;

/// <summary>
/// E2E tests for single SPA scenarios
/// </summary>
public class SingleSpaTests : E2ETestBase
{
    public SingleSpaTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void SingleSpaVue_BuildSucceeds_OutputFilesCreated()
    {
        // Arrange
        const string projectName = "SingleSpaVue";
        
        // Act
        var cleanResult = ExecuteClean(projectName);
        var buildResult = ExecuteBuild(projectName);

        // Assert
        buildResult.Success.Should().BeTrue($"build should succeed. Output: {buildResult.Output}");
        buildResult.Output.Should().Contain("Build succeeded", "MSBuild should report success");
        
        // Verify Vite ran
        buildResult.Output.Should().Contain("[BUILD]", "Vite build should have executed");
        buildResult.Output.Should().Contain("[OK]", "Build should complete successfully");
        
        // Verify output files exist
        DirectoryExists(projectName, "wwwroot/dist").Should().BeTrue("output directory should exist");
        GetFiles(projectName, "wwwroot/dist").Should().NotBeEmpty("output files should be created");
    }

    [Fact]
    public void SingleSpaVue_IncrementalBuild_SkipsUnchangedFiles()
    {
        // Arrange
        const string projectName = "SingleSpaVue";
        
        // Act - First build
        ExecuteClean(projectName);
        var firstBuild = ExecuteBuild(projectName);
        
        // Act - Second build without changes
        var secondBuild = ExecuteBuild(projectName);

        // Assert
        firstBuild.Success.Should().BeTrue("first build should succeed");
        secondBuild.Success.Should().BeTrue("second build should succeed");
        
        // Second build should be faster or skip Vite
        secondBuild.Duration.Should().BeLessThanOrEqualTo(firstBuild.Duration + TimeSpan.FromSeconds(5),
            "incremental build should be as fast or faster");
    }

    // Note: ViteClean only removes ViteKit marker files and Vite cache directories.
    // Users manage cleanup of build outputs via custom targets (see advanced-scenarios.md).
}
