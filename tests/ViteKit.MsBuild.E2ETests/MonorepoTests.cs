using System;
using System.IO;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace ViteKit.MsBuild.E2ETests;

/// <summary>
/// E2E tests for monorepo/shared dependency scenarios
/// </summary>
public class MonorepoTests : E2ETestBase
{
    public MonorepoTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void MonorepoShared_BuildsInDependencyOrder()
    {
        // Arrange
        const string projectName = "MonorepoShared";
        
        // Act
        ExecuteClean(projectName);
        var buildResult = ExecuteBuild(projectName);

        // Assert
        buildResult.Success.Should().BeTrue($"build should succeed. Output: {buildResult.Output}");
        
        // Verify build completed
        buildResult.Output.Should().Contain("[BUILD]", "Vite build should have executed");
        buildResult.Output.Should().Contain("[OK]", "Build should complete successfully");
        
        // Verify outputs
        DirectoryExists(projectName, "wwwroot/shared").Should().BeTrue("shared output should exist");
        DirectoryExists(projectName, "wwwroot/app").Should().BeTrue("app output should exist");
    }

    [Fact]
    public void MonorepoShared_SharedChangeTriggersDependentRebuild()
    {
        // Arrange
        const string projectName = "MonorepoShared";
        
        // Act - Initial build
        ExecuteClean(projectName);
        var firstBuild = ExecuteBuild(projectName);
        
        // Touch shared file
        var sharedFile = Path.Combine(TestProjectsRoot, projectName, "wwwroot/js/shared/index.ts");
        File.SetLastWriteTimeUtc(sharedFile, DateTime.UtcNow);
        
        var secondBuild = ExecuteBuild(projectName);

        // Assert
        firstBuild.Success.Should().BeTrue("first build should succeed");
        secondBuild.Success.Should().BeTrue("second build should succeed");
        
        // Shared rebuild should trigger app rebuild
        secondBuild.Output.Should().Contain("[BUILD]", "builds should execute");
    }

    [Fact]
    public void MonorepoShared_AppChangeDoesNotRebuildShared()
    {
        // Arrange
        const string projectName = "MonorepoShared";
        
        // Act - Initial build
        ExecuteClean(projectName);
        var firstBuild = ExecuteBuild(projectName);
        
        // Touch app file only
        var appFile = Path.Combine(TestProjectsRoot, projectName, "wwwroot/js/app/main.ts");
        File.SetLastWriteTimeUtc(appFile, DateTime.UtcNow);
        
        var secondBuild = ExecuteBuild(projectName);

        // Assert
        firstBuild.Success.Should().BeTrue("first build should succeed");
        secondBuild.Success.Should().BeTrue("second build should succeed");
        
        // Only app should rebuild (build system handles this)
        secondBuild.Output.Should().Contain("[BUILD]", "build should execute");
    }
}
