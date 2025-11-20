using System.Collections.Generic;

namespace ViteKit.MsBuild.Tasks
{
    /// <summary>
    /// Configuration object for Vite build commands
    /// Maps directly to MSBuild properties for simple consumption
    /// </summary>
    public class ViteBuildConfiguration
    {
        // Required
        public string ProjectRoot { get; set; } = string.Empty;
        public PackageManager PackageManager { get; set; }

        // Command strategy
        public string? CustomBuildCommand { get; set; }
        public string? BuildScript { get; set; } = "build";
        public bool DirectViteBuild { get; set; }

        // Core Vite options
        public string? ConfigFile { get; set; }
        public string? Mode { get; set; }
        public string? OutputDir { get; set; }
        public string? LogLevel { get; set; }
        public bool EnableColors { get; set; } = true;

        // Extended Vite options (for DirectViteBuild)
        public string? Base { get; set; }
        public string? Minify { get; set; }
        public string? Sourcemap { get; set; }
        public bool? EmptyOutDir { get; set; }
        public bool? Manifest { get; set; }
        public string? Target { get; set; }
        public string? AssetsDir { get; set; }
        public int? AssetsInlineLimit { get; set; }

        // Diagnostics
        public bool EnableDiagnostics { get; set; }
        public string? DiagnosticLogPath { get; set; }

        // Environment
        public Dictionary<string, string> Environment { get; set; } = new();

        /// <summary>
        /// Create configuration from MSBuild properties (convenience)
        /// </summary>
        public static ViteBuildConfiguration FromMSBuildProperties(
            string projectRoot,
            PackageManager packageManager,
            string? customCommand = null,
            string? buildScript = "build",
            bool directBuild = false,
            string? configFile = null,
            string? mode = null,
            string? outputDir = null,
            string? logLevel = null,
            bool enableColors = true,
            bool enableDiagnostics = false,
            string? diagnosticLogPath = null)
        {
            return new ViteBuildConfiguration
            {
                ProjectRoot = projectRoot,
                PackageManager = packageManager,
                CustomBuildCommand = customCommand,
                BuildScript = buildScript,
                DirectViteBuild = directBuild,
                ConfigFile = configFile,
                Mode = mode,
                OutputDir = outputDir,
                LogLevel = logLevel,
                EnableColors = enableColors,
                EnableDiagnostics = enableDiagnostics,
                DiagnosticLogPath = diagnosticLogPath
            };
        }
    }
}
