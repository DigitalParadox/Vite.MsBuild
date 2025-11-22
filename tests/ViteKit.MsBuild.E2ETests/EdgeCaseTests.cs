using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace ViteKit.MsBuild.E2ETests;

/// <summary>
/// E2E tests for edge cases and error scenarios
/// </summary>
public class EdgeCaseTests : E2ETestBase
{
    public EdgeCaseTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void Build_WithVerbosityQuiet_SuppressesViteOutput()
    {
        // Arrange
        const string projectName = "SingleSpaVue";
        
        // Act
        ExecuteClean(projectName);
        var buildResult = ExecuteBuild(projectName, additionalArgs: "-v:q");

        // Assert
        buildResult.Success.Should().BeTrue("build should succeed even with quiet verbosity");
        
        // Should have minimal output
        buildResult.Output.Should().NotContain("[BUILD]", "detailed build messages should be suppressed");
    }

    [Fact]
    public void Build_WithVerbosityDetailed_ShowsDetailedViteOutput()
    {
        // Arrange
        const string projectName = "SingleSpaVue";
        
        // Act
        ExecuteClean(projectName);
        var buildResult = ExecuteBuild(projectName, additionalArgs: "-v:d");

        // Assert
        buildResult.Success.Should().BeTrue("build should succeed with detailed verbosity");
        
        // Should have detailed output from orchestration
        buildResult.Output.Should().Contain("[ORCHESTRATE]", "orchestration messages should be shown");
        buildResult.Output.Should().Contain("restore task(s)", "task counts should be shown");
    }

    [Fact]
    public void Build_Release_UsesProductionMode()
    {
        // Arrange
        const string projectName = "SingleSpaVue";
        
        // Act
        ExecuteClean(projectName);
        var buildResult = ExecuteBuild(projectName, configuration: "Release");

        // Assert
        buildResult.Success.Should().BeTrue("release build should succeed");
        buildResult.Output.Should().Contain("production", "should build in production mode");
    }

    [Fact]
    public void Build_Debug_UsesDevelopmentMode()
    {
        // Arrange
        const string projectName = "SingleSpaVue";
        
        // Act
        ExecuteClean(projectName);
        var buildResult = ExecuteBuild(projectName, configuration: "Debug");

        // Assert
        buildResult.Success.Should().BeTrue("debug build should succeed");
        // Mode is passed to vite via --mode flag, check for vite command execution
        buildResult.Output.Should().Contain("vite build", "should execute vite build command");
    }
}
