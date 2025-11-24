using System;

namespace ViteKit.MsBuild.Tasks.Diagnostics
{
    /// <summary>
    /// Detects CI environment and provides CI-specific logging commands
    /// </summary>
    public static class CIDetection
    {
        public static bool IsCI => !string.IsNullOrEmpty(GetCIProvider());

        public static string? GetCIProvider()
        {
            // GitHub Actions
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true")
                return "GitHub";

            // Azure DevOps
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TF_BUILD")))
                return "AzureDevOps";

            // GitLab CI
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITLAB_CI")))
                return "GitLab";

            // Jenkins
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JENKINS_URL")))
                return "Jenkins";

            // TeamCity
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEAMCITY_VERSION")))
                return "TeamCity";

            // CircleCI
            if (Environment.GetEnvironmentVariable("CIRCLECI") == "true")
                return "CircleCI";

            return null;
        }

        public static bool ShouldAutoEnableDiagnostics()
        {
            // Check explicit user settings first
            var explicitSetting = Environment.GetEnvironmentVariable("VITEKIT_DIAGNOSTIC_LOG");
            if (explicitSetting == "1" || explicitSetting?.ToLowerInvariant() == "true")
                return true;
            if (explicitSetting == "0" || explicitSetting?.ToLowerInvariant() == "false")
                return false;

            // Check CI-specific debug flags
            if (IsDebugModeEnabled())
                return true;

            // Default: OFF (no auto-enable)
            return false;
        }

        /// <summary>
        /// Check if CI provider has debug/verbose mode enabled
        /// </summary>
        public static bool IsDebugModeEnabled()
        {
            var provider = GetCIProvider();

            switch (provider)
            {
                case "GitHub":
                    // GitHub Actions: Check for debug runner or step debug
                    // Set via: "Re-run jobs" → "Enable debug logging"
                    return Environment.GetEnvironmentVariable("RUNNER_DEBUG") == "1" ||
                           Environment.GetEnvironmentVariable("ACTIONS_STEP_DEBUG") == "true";

                case "AzureDevOps":
                    // Azure Pipelines: Check system.debug variable
                    return Environment.GetEnvironmentVariable("SYSTEM_DEBUG")?.ToLowerInvariant() == "true";

                case "GitLab":
                    // GitLab: Check CI_DEBUG_TRACE
                    return Environment.GetEnvironmentVariable("CI_DEBUG_TRACE")?.ToLowerInvariant() == "true";

                case "Jenkins":
                    // Jenkins: No standard debug flag, check verbose
                    return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JENKINS_DEBUG"));

                case "TeamCity":
                    // TeamCity: Check debug mode
                    return Environment.GetEnvironmentVariable("TEAMCITY_BUILD_DEBUG_MODE")?.ToLowerInvariant() == "true";

                default:
                    return false;
            }
        }

        /// <summary>
        /// Get the appropriate artifact path for the CI provider
        /// </summary>
        public static string? GetCIArtifactPath()
        {
            var provider = GetCIProvider();
            
            switch (provider)
            {
                case "GitHub":
                    // GitHub Actions: Write to workflow temp
                    var runnerTemp = Environment.GetEnvironmentVariable("RUNNER_TEMP");
                    return runnerTemp != null 
                        ? System.IO.Path.Combine(runnerTemp, "vitekit-diagnostics")
                        : null;

                case "AzureDevOps":
                    // Azure DevOps: Write to staging directory
                    var stagingDir = Environment.GetEnvironmentVariable("BUILD_ARTIFACTSTAGINGDIRECTORY");
                    return stagingDir != null
                        ? System.IO.Path.Combine(stagingDir, "vitekit-diagnostics")
                        : null;

                case "GitLab":
                    // GitLab: Use CI_PROJECT_DIR
                    var projectDir = Environment.GetEnvironmentVariable("CI_PROJECT_DIR");
                    return projectDir != null
                        ? System.IO.Path.Combine(projectDir, "vitekit-diagnostics")
                        : null;

                default:
                    return null;
            }
        }

        /// <summary>
        /// Emit CI-specific commands to upload artifacts
        /// </summary>
        public static void EmitArtifactUploadCommand(string filePath, string provider)
        {
            switch (provider)
            {
                case "GitHub":
                    // GitHub Actions workflow command
                    Console.WriteLine($"::notice title=ViteKit Diagnostics::Diagnostic log available at {filePath}");
                    
                    // Write to $GITHUB_OUTPUT for actions to consume
                    var githubOutput = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");
                    if (!string.IsNullOrEmpty(githubOutput))
                    {
                        System.IO.File.AppendAllText(githubOutput, 
                            $"diagnostic-log={filePath}{Environment.NewLine}");
                    }
                    break;

                case "AzureDevOps":
                    // Azure Pipelines logging command
                    Console.WriteLine($"##vso[artifact.upload containerfolder=vitekit-diagnostics;artifactname=vitekit-diagnostics]{filePath}");
                    break;

                case "GitLab":
                    // GitLab uses artifacts: paths in .gitlab-ci.yml
                    Console.WriteLine($"GitLab artifact available at: {filePath}");
                    break;

                case "TeamCity":
                    // TeamCity service message
                    Console.WriteLine($"##teamcity[publishArtifacts '{filePath}']");
                    break;
            }
        }
    }
}
