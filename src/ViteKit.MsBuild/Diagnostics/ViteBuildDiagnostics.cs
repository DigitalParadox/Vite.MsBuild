using System;

namespace ViteKit.MsBuild.Tasks.Diagnostics
{
    /// <summary>
    /// Diagnostic data for performance and error analysis.
    /// Contains NO user data, NO file paths, NO sensitive information.
    /// </summary>
    public class ViteBuildDiagnostics
    {
        // Build metadata
        public string PackageVersion { get; set; } = string.Empty;
        public string ViteVersion { get; set; } = string.Empty;
        public PackageManager PackageManager { get; set; }
        public string? Framework { get; set; } // "vue" | "react" | "svelte" | null
        public ViteCommandType CommandType { get; set; }
        
        // Command details
        public string ExecutedCommand { get; set; } = string.Empty; // Full command executed (sanitized)
        public string WorkingDirectory { get; set; } = string.Empty; // Project root only, no full paths

        // Performance metrics
        public long BuildDurationMs { get; set; }
        public int InputFileCount { get; set; }
        public long OutputSizeBytes { get; set; }

        // Build result
        public bool Success { get; set; }
        public int? ExitCode { get; set; }

        // Error info (if failed) - NO error messages (may contain sensitive data)
        public string? ErrorType { get; set; } // Exception type name only
        public string? FailedCommand { get; set; } // "npm" | "pnpm" | "vite" (command name only)

        // Environment (technical only)
        public string DotNetVersion { get; set; } = string.Empty;
        public string OSPlatform { get; set; } = string.Empty; // "Windows" | "Linux" | "macOS"
        public string Architecture { get; set; } = string.Empty; // "x64" | "arm64"
        public string? CIProvider { get; set; } // "GitHub" | "AzureDevOps" | "GitLab" | null

        // Timestamp
        public DateTime BuildStartUtc { get; set; }
    }
}
