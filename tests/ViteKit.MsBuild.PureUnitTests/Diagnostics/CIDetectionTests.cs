using System;
using ViteKit.MsBuild.Tasks.Diagnostics;
using Xunit;

namespace ViteKit.MsBuild.PureUnitTests.Diagnostics
{
    public class CIDetectionTests
    {
        [Fact]
        public void GetCIProvider_NoEnvironmentVariables_ReturnsNull()
        {
            // Arrange - Clean environment
            ClearCIEnvironmentVariables();

            // Act
            var provider = CIDetection.GetCIProvider();

            // Assert
            Assert.Null(provider);
        }

        [Theory]
        [InlineData("GITHUB_ACTIONS", "true", "GitHub")]
        [InlineData("TF_BUILD", "True", "AzureDevOps")]
        [InlineData("GITLAB_CI", "true", "GitLab")]
        [InlineData("JENKINS_URL", "http://jenkins", "Jenkins")]
        [InlineData("TEAMCITY_VERSION", "2023.1", "TeamCity")]
        [InlineData("CIRCLECI", "true", "CircleCI")]
        public void GetCIProvider_DetectsCorrectProvider(string envVar, string value, string expectedProvider)
        {
            // Arrange
            ClearCIEnvironmentVariables();
            Environment.SetEnvironmentVariable(envVar, value);

            try
            {
                // Act
                var provider = CIDetection.GetCIProvider();

                // Assert
                Assert.Equal(expectedProvider, provider);
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable(envVar, null);
            }
        }

        [Fact]
        public void ShouldAutoEnableDiagnostics_NoCI_ReturnsFalse()
        {
            // Arrange
            ClearCIEnvironmentVariables();
            Environment.SetEnvironmentVariable("VITEKIT_DIAGNOSTIC_LOG", null);

            // Act
            var result = CIDetection.ShouldAutoEnableDiagnostics();

            // Assert
            Assert.False(result, "Diagnostics should default to OFF");
        }

        [Theory]
        [InlineData("true")]
        [InlineData("True")]
        [InlineData("TRUE")]
        [InlineData("1")]
        public void ShouldAutoEnableDiagnostics_ExplicitEnable_ReturnsTrue(string value)
        {
            // Arrange
            ClearCIEnvironmentVariables();
            Environment.SetEnvironmentVariable("VITEKIT_DIAGNOSTIC_LOG", value);

            try
            {
                // Act
                var result = CIDetection.ShouldAutoEnableDiagnostics();

                // Assert
                Assert.True(result);
            }
            finally
            {
                Environment.SetEnvironmentVariable("VITEKIT_DIAGNOSTIC_LOG", null);
            }
        }

        [Theory]
        [InlineData("false")]
        [InlineData("False")]
        [InlineData("FALSE")]
        [InlineData("0")]
        public void ShouldAutoEnableDiagnostics_ExplicitDisable_ReturnsFalse(string value)
        {
            // Arrange
            Environment.SetEnvironmentVariable("GITHUB_ACTIONS", "true");
            Environment.SetEnvironmentVariable("RUNNER_DEBUG", "1");
            Environment.SetEnvironmentVariable("VITEKIT_DIAGNOSTIC_LOG", value);

            try
            {
                // Act
                var result = CIDetection.ShouldAutoEnableDiagnostics();

                // Assert
                Assert.False(result, "Explicit disable should override CI debug mode");
            }
            finally
            {
                Environment.SetEnvironmentVariable("GITHUB_ACTIONS", null);
                Environment.SetEnvironmentVariable("RUNNER_DEBUG", null);
                Environment.SetEnvironmentVariable("VITEKIT_DIAGNOSTIC_LOG", null);
            }
        }

        [Theory]
        [InlineData("RUNNER_DEBUG", "1")]
        [InlineData("ACTIONS_STEP_DEBUG", "true")]
        public void IsDebugModeEnabled_GitHubActions_DetectsDebugFlags(string envVar, string value)
        {
            // Arrange
            ClearCIEnvironmentVariables();
            Environment.SetEnvironmentVariable("GITHUB_ACTIONS", "true");
            Environment.SetEnvironmentVariable(envVar, value);

            try
            {
                // Act
                var result = CIDetection.IsDebugModeEnabled();

                // Assert
                Assert.True(result);
            }
            finally
            {
                Environment.SetEnvironmentVariable("GITHUB_ACTIONS", null);
                Environment.SetEnvironmentVariable(envVar, null);
            }
        }

        [Fact]
        public void ShouldAutoEnableDiagnostics_GitHubDebugMode_ReturnsTrue()
        {
            // Arrange
            ClearCIEnvironmentVariables();
            Environment.SetEnvironmentVariable("GITHUB_ACTIONS", "true");
            Environment.SetEnvironmentVariable("RUNNER_DEBUG", "1");
            Environment.SetEnvironmentVariable("VITEKIT_DIAGNOSTIC_LOG", null);

            try
            {
                // Act
                var result = CIDetection.ShouldAutoEnableDiagnostics();

                // Assert
                Assert.True(result, "Should auto-enable when GitHub Actions debug mode is ON");
            }
            finally
            {
                Environment.SetEnvironmentVariable("GITHUB_ACTIONS", null);
                Environment.SetEnvironmentVariable("RUNNER_DEBUG", null);
            }
        }

        private void ClearCIEnvironmentVariables()
        {
            // Clear all CI-related environment variables
            Environment.SetEnvironmentVariable("GITHUB_ACTIONS", null);
            Environment.SetEnvironmentVariable("RUNNER_DEBUG", null);
            Environment.SetEnvironmentVariable("ACTIONS_STEP_DEBUG", null);
            Environment.SetEnvironmentVariable("RUNNER_TEMP", null);
            
            Environment.SetEnvironmentVariable("TF_BUILD", null);
            Environment.SetEnvironmentVariable("SYSTEM_DEBUG", null);
            Environment.SetEnvironmentVariable("BUILD_ARTIFACTSTAGINGDIRECTORY", null);
            
            Environment.SetEnvironmentVariable("GITLAB_CI", null);
            Environment.SetEnvironmentVariable("CI_DEBUG_TRACE", null);
            Environment.SetEnvironmentVariable("CI_PROJECT_DIR", null);
            
            Environment.SetEnvironmentVariable("JENKINS_URL", null);
            Environment.SetEnvironmentVariable("JENKINS_DEBUG", null);
            
            Environment.SetEnvironmentVariable("TEAMCITY_VERSION", null);
            Environment.SetEnvironmentVariable("TEAMCITY_BUILD_DEBUG_MODE", null);
            
            Environment.SetEnvironmentVariable("CIRCLECI", null);
            
            Environment.SetEnvironmentVariable("VITEKIT_DIAGNOSTIC_LOG", null);
        }
    }
}
