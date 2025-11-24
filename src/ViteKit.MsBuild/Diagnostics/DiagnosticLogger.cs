using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace ViteKit.MsBuild.Tasks.Diagnostics
{
    /// <summary>
    /// Handles diagnostic logging for ViteKit builds.
    /// All logging is opt-in and privacy-safe.
    /// </summary>
    public static class DiagnosticLogger
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Log diagnostics to MSBuild output and optionally to JSON file
        /// </summary>
        public static void LogDiagnostics(TaskLoggingHelper log, ViteBuildDiagnostics diagnostics, bool writeToFile, string? logPath)
        {
            if (diagnostics == null) return;

            // Always log to MSBuild at Low importance (visible with -v:detailed)
            LogToMSBuild(log, diagnostics);

            // Determine if we should write to file
            var shouldWrite = writeToFile;
            var effectiveLogPath = logPath;

            // Check if diagnostics should auto-enable (CI debug mode or explicit opt-in)
            if (!shouldWrite && CIDetection.ShouldAutoEnableDiagnostics())
            {
                shouldWrite = true;
                effectiveLogPath = CIDetection.GetCIArtifactPath() ?? logPath;
                
                var provider = CIDetection.GetCIProvider();
                var debugEnabled = CIDetection.IsDebugModeEnabled();
                
                if (debugEnabled)
                {
                    log.LogMessage(MessageImportance.Normal,
                        "🐛 CI debug mode detected ({0}). Auto-enabling diagnostic logging.", provider);
                }
                else
                {
                    log.LogMessage(MessageImportance.Normal,
                        "📊 ViteKit diagnostic logging enabled via VITEKIT_DIAGNOSTIC_LOG.");
                }
            }

            // Write structured JSON log
            if (shouldWrite && !string.IsNullOrEmpty(effectiveLogPath))
            {
                var filePath = WriteJsonLog(log, diagnostics, effectiveLogPath);
                
                // Emit CI-specific artifact upload commands
                if (filePath != null && CIDetection.IsCI)
                {
                    var provider = CIDetection.GetCIProvider();
                    if (provider != null)
                    {
                        CIDetection.EmitArtifactUploadCommand(filePath, provider);
                    }
                }
            }
        }

        private static void LogToMSBuild(TaskLoggingHelper log, ViteBuildDiagnostics diagnostics)
        {
            log.LogMessage(MessageImportance.Low,
                "[BUILD] ViteKit Diagnostics:\n" +
                "  Package Version: {0}\n" +
                "  Vite Version: {1}\n" +
                "  Package Manager: {2}\n" +
                "  Framework: {3}\n" +
                "  Command Type: {4}\n" +
                "  Executed Command: {5}\n" +
                "  Working Directory: {6}\n" +
                "  Build Duration: {7}ms\n" +
                "  Input Files: {8}\n" +
                "  Output Size: {9} KB\n" +
                "  Success: {10}\n" +
                "  Exit Code: {11}\n" +
                "  Platform: {12} {13} (.NET {14})\n" +
                "  Error Type: {15}\n" +
                "  CI Provider: {16}",
                diagnostics.PackageVersion,
                diagnostics.ViteVersion ?? "unknown",
                diagnostics.PackageManager,
                diagnostics.Framework ?? "unknown",
                diagnostics.CommandType,
                diagnostics.ExecutedCommand,
                diagnostics.WorkingDirectory,
                diagnostics.BuildDurationMs,
                diagnostics.InputFileCount,
                diagnostics.OutputSizeBytes / 1024,
                diagnostics.Success ? "[OK]" : "[ERROR]",
                diagnostics.ExitCode?.ToString() ?? "n/a",
                diagnostics.OSPlatform,
                diagnostics.Architecture,
                diagnostics.DotNetVersion,
                diagnostics.ErrorType ?? "none",
                diagnostics.CIProvider ?? "none");
        }

        private static string? WriteJsonLog(TaskLoggingHelper log, ViteBuildDiagnostics diagnostics, string logPath)
        {
            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(logPath);

                // Create timestamped filename
                var fileName = $"vitekit-build-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
                var fullPath = Path.Combine(logPath, fileName);

                // Serialize and write
                var json = JsonSerializer.Serialize(diagnostics, JsonOptions);
                File.WriteAllText(fullPath, json);

                log.LogMessage(MessageImportance.Low,
                    "📊 Diagnostic log written to: {0}", fullPath);

                // Cleanup old logs (keep last 50)
                CleanupOldLogs(logPath, maxFiles: 50);

                return fullPath;
            }
            catch (Exception ex)
            {
                // Never fail build due to diagnostic logging
                log.LogMessage(MessageImportance.Low,
                    "[WARN] Failed to write diagnostic log: {0}", ex.Message);
                return null;
            }
        }

        private static void CleanupOldLogs(string logPath, int maxFiles)
        {
            try
            {
                var logFiles = Directory.GetFiles(logPath, "vitekit-build-*.json");
                if (logFiles.Length <= maxFiles) return;

                // Sort by creation time, delete oldest
                Array.Sort(logFiles, (a, b) =>
                    File.GetCreationTimeUtc(a).CompareTo(File.GetCreationTimeUtc(b)));

                var filesToDelete = logFiles.Length - maxFiles;
                for (int i = 0; i < filesToDelete; i++)
                {
                    File.Delete(logFiles[i]);
                }
            }
            catch
            {
                // Ignore cleanup failures
            }
        }

        public static string GetOSPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "Windows";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "Linux";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "macOS";
            return "Unknown";
        }
    }
}
