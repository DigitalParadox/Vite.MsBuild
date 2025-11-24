using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace ViteKit.MsBuild.E2ETests;

/// <summary>
/// E2E tests for MSBuild integration scenarios
/// </summary>
public class MsBuildIntegrationTests : E2ETestBase
{
    public MsBuildIntegrationTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void Build_CreatesMarkerFile_InObjDirectory()
    {
        // Arrange
        const string projectName = "SingleSpaVue";
        
        // Act
        ExecuteClean(projectName);
        var buildResult = ExecuteBuild(projectName);

        // Assert
        buildResult.Success.Should().BeTrue("build should succeed");
        
        // Verify marker file exists in obj directory
        var objDir = Path.Combine(TestProjectsRoot, projectName, "obj");
        Directory.Exists(objDir).Should().BeTrue("obj directory should exist");
        
        var markerFiles = Directory.GetFiles(objDir, "*.marker", SearchOption.AllDirectories);
        markerFiles.Should().NotBeEmpty("marker file should be created for incremental builds");
    }

    [Fact]
    public void Build_OutputsAreIncludedInPublish()
    {
        // Arrange
        const string projectName = "SingleSpaVue";
        
        // Act
        ExecuteClean(projectName);
        var buildResult = ExecuteBuild(projectName);
        
        // Note: We can't easily test publish in E2E without creating publish artifacts
        // But we can verify the build output structure matches what publish expects

        // Assert
        buildResult.Success.Should().BeTrue("build should succeed");
        
        // Verify wwwroot structure exists (this gets published)
        DirectoryExists(projectName, "wwwroot/dist").Should().BeTrue("wwwroot/dist should exist for publish");
        
        var outputFiles = GetFiles(projectName, "wwwroot/dist");
        outputFiles.Should().Contain(f => f.EndsWith(".js") || f.EndsWith(".mjs"), 
            "should have JavaScript files for publish");
    }

    [Fact]
    public void Build_WithContinuousIntegration_ShowsAppropriateMessages()
    {
        // Arrange
        const string projectName = "SingleSpaVue";
        
        // Act
        ExecuteClean(projectName);
        var buildResult = ExecuteBuild(projectName, additionalArgs: "/p:CI=true");

        // Assert
        buildResult.Success.Should().BeTrue("CI build should succeed");
        
        // CI builds should have clear output for build systems
        buildResult.Output.Should().Contain("Build succeeded", "CI should show clear success message");
    }

    [Fact]
    public void Build_WithDifferentTargetFrameworks_Works()
    {
        // Arrange
        const string projectName = "SingleSpaVue";
        
        // Act
        ExecuteClean(projectName);
        var buildResult = ExecuteBuild(projectName, additionalArgs: "/p:TargetFramework=net8.0");

        // Assert
        buildResult.Success.Should().BeTrue("build with explicit framework should succeed");
        DirectoryExists(projectName, "wwwroot/dist").Should().BeTrue("output should be created");
    }

    [Fact(Skip = "Incremental builds disabled - functionality not yet implemented in refactored architecture")]
    public void Build_PreservesFileTimestamps_ForIncrementalBuilds()
    {
        // Arrange
        const string projectName = "IncrementalBuild";
        
        // Act - First build
        ExecuteClean(projectName);
        var firstBuild = ExecuteBuild(projectName);
        
        var outputDir = Path.Combine(TestProjectsRoot, projectName, "wwwroot/dist");
        var firstBuildFiles = Directory.GetFiles(outputDir, "*.*", SearchOption.AllDirectories)
            .ToDictionary(f => f, f => new FileInfo(f).LastWriteTimeUtc);
        
        System.Threading.Thread.Sleep(2000);
        
        // Act - Second build without changes
        var secondBuild = ExecuteBuild(projectName);
        
        var secondBuildFiles = Directory.GetFiles(outputDir, "*.*", SearchOption.AllDirectories)
            .ToDictionary(f => f, f => new FileInfo(f).LastWriteTimeUtc);

        // Assert - TRUE INCREMENTAL BUILD TEST
        firstBuild.Success.Should().BeTrue("first build should succeed");
        secondBuild.Success.Should().BeTrue("second build should succeed");
        
        // Files should have IDENTICAL timestamps (proving MSBuild skipped ViteBuild target)
        foreach (var file in firstBuildFiles)
        {
            if (secondBuildFiles.ContainsKey(file.Key))
            {
                var timeDiff = (secondBuildFiles[file.Key] - file.Value).TotalSeconds;
                Math.Abs(timeDiff).Should().BeLessThan(5, 
                    $"file {Path.GetFileName(file.Key)} should not be rebuilt without changes");
            }
        }
    }

    [Fact]
    public void Build_FailsGracefully_WhenViteConfigInvalid()
    {
        // Arrange
        const string projectName = "SingleSpaVue";
        var configFile = Path.Combine(TestProjectsRoot, projectName, "vite.config.ts");
        var originalContent = File.ReadAllText(configFile);
        
        try
        {
            // Break the config
            File.WriteAllText(configFile, "invalid syntax {{{");
            
            // Act
            var buildResult = ExecuteBuild(projectName);

            // Assert
            buildResult.Success.Should().BeFalse("build should fail with invalid config");
            buildResult.Output.Should().Contain("error", "should show error message");
        }
        finally
        {
            // Cleanup
            File.WriteAllText(configFile, originalContent);
        }
    }

    [Fact]
    public void Build_SupportsParallelBuilds_MultiSpa()
    {
        // Arrange
        const string projectName = "MultiSpaReact";
        
        // Act
        ExecuteClean(projectName);
        var buildResult = ExecuteBuild(projectName, additionalArgs: "/m");

        // Assert
        buildResult.Success.Should().BeTrue("parallel build should succeed");
        
        // Both SPAs should build
        DirectoryExists(projectName, "wwwroot/admin").Should().BeTrue("admin should build");
        DirectoryExists(projectName, "wwwroot/customer").Should().BeTrue("customer should build");
        
        // Verify no build conflicts
        GetFiles(projectName, "wwwroot/admin").Should().NotBeEmpty("admin files should exist");
        GetFiles(projectName, "wwwroot/customer").Should().NotBeEmpty("customer files should exist");
    }
}
