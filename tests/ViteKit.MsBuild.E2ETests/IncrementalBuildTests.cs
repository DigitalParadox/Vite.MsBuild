using System;
using System.IO;
using System.Threading;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace ViteKit.MsBuild.E2ETests;

/// <summary>
/// E2E tests for incremental build scenarios
/// </summary>
public class IncrementalBuildTests : E2ETestBase
{
    public IncrementalBuildTests(ITestOutputHelper output) : base(output) { }

    [Fact(Skip = "Incremental builds disabled - functionality not yet implemented in refactored architecture")]
    public void IncrementalBuild_NoChanges_SkipsViteBuild()
    {
        // Arrange
        const string projectName = "IncrementalBuild";
        
        // Act - First build
        ExecuteClean(projectName);
        var firstBuild = ExecuteBuild(projectName);
        
        // Wait a moment to ensure file timestamps are different
        Thread.Sleep(1000);
        
        // Act - Second build without changes
        var secondBuild = ExecuteBuild(projectName);

        // Assert
        firstBuild.Success.Should().BeTrue("first build should succeed");
        secondBuild.Success.Should().BeTrue("second build should succeed");
        
        // First build should execute Vite
        firstBuild.Output.Should().Contain("[BUILD]", "first build should run Vite");
        
        // TRUE INCREMENTAL BUILD TEST: Second build should report "Skipping target" for ViteBuild
        // MSBuild only skips when Inputs/Outputs are configured and no inputs changed
        secondBuild.Output.Should().Contain("Skipping target \"ViteBuild\"", 
            "MSBuild should skip ViteBuild target when no inputs changed");
        
        // Additional check: Should NOT see [BUILD] in second build output
        secondBuild.Output.Should().NotContain("[BUILD]", 
            "second build should not execute Vite when no files changed");
    }

    [Fact(Skip = "Incremental builds disabled - functionality not yet implemented in refactored architecture")]
    public void IncrementalBuild_JavaScriptChange_TriggersRebuild()
    {
        // Arrange
        const string projectName = "IncrementalBuild";
        
        // Act - Initial build
        ExecuteClean(projectName);
        var firstBuild = ExecuteBuild(projectName);
        
        // Modify JavaScript file
        var jsFile = Path.Combine(TestProjectsRoot, projectName, "wwwroot/js/main.ts");
        var originalContent = File.ReadAllText(jsFile);
        File.WriteAllText(jsFile, originalContent + "\n// Modified");
        Thread.Sleep(1000);
        
        var secondBuild = ExecuteBuild(projectName);
        
        // Cleanup
        File.WriteAllText(jsFile, originalContent);

        // Assert
        firstBuild.Success.Should().BeTrue("first build should succeed");
        secondBuild.Success.Should().BeTrue("second build should succeed");
        
        // TRUE INCREMENTAL BUILD TEST: Second build should NOT skip ViteBuild
        secondBuild.Output.Should().NotContain("Skipping target \"ViteBuild\"", 
            "MSBuild should NOT skip ViteBuild when input files changed");
        
        // Both builds should execute Vite
        firstBuild.Output.Should().Contain("[BUILD]", "first build should run Vite");
        secondBuild.Output.Should().Contain("[BUILD]", 
            "second build should rebuild due to JS file change");
    }

    [Fact(Skip = "Incremental builds disabled - functionality not yet implemented in refactored architecture")]
    public void IncrementalBuild_CssChange_TriggersRebuild()
    {
        // Arrange
        const string projectName = "IncrementalBuild";
        
        // Act - Initial build
        ExecuteClean(projectName);
        var firstBuild = ExecuteBuild(projectName);
        
        // Modify CSS file
        var cssFile = Path.Combine(TestProjectsRoot, projectName, "wwwroot/css/styles.css");
        var originalContent = File.ReadAllText(cssFile);
        File.WriteAllText(cssFile, originalContent + "\n/* Modified */");
        Thread.Sleep(1000);
        
        var secondBuild = ExecuteBuild(projectName);
        
        // Cleanup
        File.WriteAllText(cssFile, originalContent);

        // Assert
        firstBuild.Success.Should().BeTrue("first build should succeed");
        secondBuild.Success.Should().BeTrue("second build should succeed");
        
        // TRUE INCREMENTAL BUILD TEST: MSBuild should detect CSS file change
        secondBuild.Output.Should().NotContain("Skipping target \"ViteBuild\"", 
            "MSBuild should NOT skip ViteBuild when CSS files changed");
        
        secondBuild.Output.Should().Contain("[BUILD]", "should rebuild due to CSS change");
    }

    [Fact(Skip = "Incremental builds disabled - functionality not yet implemented in refactored architecture")]
    public void IncrementalBuild_ConfigFileChange_TriggersRebuild()
    {
        // Arrange
        const string projectName = "IncrementalBuild";
        
        // Act - Initial build
        ExecuteClean(projectName);
        var firstBuild = ExecuteBuild(projectName);
        
        // Touch config file
        var configFile = Path.Combine(TestProjectsRoot, projectName, "vite.config.ts");
        File.SetLastWriteTimeUtc(configFile, DateTime.UtcNow);
        Thread.Sleep(1000);
        
        var secondBuild = ExecuteBuild(projectName);

        // Assert
        firstBuild.Success.Should().BeTrue("first build should succeed");
        secondBuild.Success.Should().BeTrue("second build should succeed");
        
        // TRUE INCREMENTAL BUILD TEST: Config changes should trigger rebuild
        secondBuild.Output.Should().NotContain("Skipping target \"ViteBuild\"", 
            "MSBuild should NOT skip ViteBuild when config file changed");
        
        secondBuild.Output.Should().Contain("[BUILD]", "should rebuild due to config change");
    }

    [Fact(Skip = "Incremental builds disabled - functionality not yet implemented in refactored architecture")]
    public void IncrementalBuild_PackageJsonChange_TriggersRebuild()
    {
        // Arrange
        const string projectName = "IncrementalBuild";
        
        // Act - Initial build
        ExecuteClean(projectName);
        var firstBuild = ExecuteBuild(projectName);
        
        // Touch package.json
        var packageJson = Path.Combine(TestProjectsRoot, projectName, "package.json");
        File.SetLastWriteTimeUtc(packageJson, DateTime.UtcNow);
        Thread.Sleep(1000);
        
        var secondBuild = ExecuteBuild(projectName);

        // Assert
        firstBuild.Success.Should().BeTrue("first build should succeed");
        secondBuild.Success.Should().BeTrue("second build should succeed");
        
        // TRUE INCREMENTAL BUILD TEST: package.json changes should trigger rebuild
        secondBuild.Output.Should().NotContain("Skipping target \"ViteBuild\"", 
            "MSBuild should NOT skip ViteBuild when package.json changed");
        
        secondBuild.Output.Should().Contain("[BUILD]", "should rebuild due to package.json change");
    }

    [Fact(Skip = "Incremental builds disabled - functionality not yet implemented in refactored architecture")]
    public void IncrementalBuild_CSharpChange_DoesNotTriggerViteRebuild()
    {
        // Arrange
        const string projectName = "IncrementalBuild";
        
        // Act - Initial build
        ExecuteClean(projectName);
        var firstBuild = ExecuteBuild(projectName);
        
        // Modify C# file only
        var csFile = Path.Combine(TestProjectsRoot, projectName, "Program.cs");
        var originalContent = File.ReadAllText(csFile);
        File.WriteAllText(csFile, originalContent.Replace("Incremental Build Test", "Modified Test"));
        Thread.Sleep(1000);
        
        var secondBuild = ExecuteBuild(projectName);
        
        // Cleanup
        File.WriteAllText(csFile, originalContent);

        // Assert
        firstBuild.Success.Should().BeTrue("first build should succeed");
        secondBuild.Success.Should().BeTrue("second build should succeed");
        
        // TRUE INCREMENTAL BUILD TEST: C# changes should NOT prevent ViteBuild skip
        // MSBuild should skip ViteBuild because no frontend files changed
        secondBuild.Output.Should().Contain("Skipping target \"ViteBuild\"", 
            "MSBuild should skip ViteBuild when only C# files changed (no frontend changes)");
        
        // Should NOT execute Vite at all
        secondBuild.Output.Should().NotContain("[BUILD]", 
            "should not execute Vite when only C# files changed");
    }

    [Fact(Skip = "Incremental builds disabled - functionality not yet implemented in refactored architecture")]
    public void IncrementalBuild_MultipleFileChanges_SingleRebuild()
    {
        // Arrange
        const string projectName = "IncrementalBuild";
        
        // Act - Initial build
        ExecuteClean(projectName);
        var firstBuild = ExecuteBuild(projectName);
        
        // Modify multiple files
        var jsFile = Path.Combine(TestProjectsRoot, projectName, "wwwroot/js/main.ts");
        var utilFile = Path.Combine(TestProjectsRoot, projectName, "wwwroot/js/utils.ts");
        var cssFile = Path.Combine(TestProjectsRoot, projectName, "wwwroot/css/styles.css");
        
        var jsContent = File.ReadAllText(jsFile);
        var utilContent = File.ReadAllText(utilFile);
        var cssContent = File.ReadAllText(cssFile);
        
        File.WriteAllText(jsFile, jsContent + "\n// Modified 1");
        File.WriteAllText(utilFile, utilContent + "\n// Modified 2");
        File.WriteAllText(cssFile, cssContent + "\n/* Modified 3 */");
        Thread.Sleep(1000);
        
        var secondBuild = ExecuteBuild(projectName);
        
        // Cleanup
        File.WriteAllText(jsFile, jsContent);
        File.WriteAllText(utilFile, utilContent);
        File.WriteAllText(cssFile, cssContent);

        // Assert
        firstBuild.Success.Should().BeTrue("first build should succeed");
        secondBuild.Success.Should().BeTrue("second build should succeed");
        
        // TRUE INCREMENTAL BUILD TEST: Multiple file changes should trigger single rebuild
        secondBuild.Output.Should().NotContain("Skipping target \"ViteBuild\"", 
            "MSBuild should NOT skip ViteBuild when multiple input files changed");
        
        // Should rebuild once (not multiple times) for all changes
        var buildMatches = System.Text.RegularExpressions.Regex.Matches(secondBuild.Output, @"\[BUILD\]");
        buildMatches.Count.Should().Be(1, 
            "should execute ViteBuild target exactly once despite multiple file changes");
    }

    [Fact(Skip = "Incremental builds disabled - functionality not yet implemented in refactored architecture")]
    public void IncrementalBuild_CleanBuild_RemovesMarkerFile()
    {
        // Arrange
        const string projectName = "IncrementalBuild";
        var markerFile = Path.Combine(TestProjectsRoot, projectName, "obj/ViteKit.Build.marker");
        
        // Act - Build then clean
        ExecuteBuild(projectName);
        var markerExistsAfterBuild = File.Exists(markerFile);
        
        ExecuteClean(projectName);
        var markerExistsAfterClean = File.Exists(markerFile);

        // Assert - TRUE INCREMENTAL BUILD TEST
        markerExistsAfterBuild.Should().BeTrue(
            "marker file should exist after successful build for incremental tracking");
        
        markerExistsAfterClean.Should().BeFalse(
            "marker file should be removed by Clean target to force full rebuild");
    }
}
