using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Vite.MsBuild.Tasks
{
    /// <summary>
    /// High-performance C# task to replace 300+ lines of XML package manager detection logic
    /// Detects package manager from lock files, handles conflicts, provides specific guidance
    /// </summary>
    public class DetectPackageManagerTask : Task
    {
        [Required]
        public string ViteProjectRoot { get; set; } = string.Empty;

        public string ConflictAction { get; set; } = "warn"; // warn, error

        [Output]
        public string PackageManager { get; set; } = string.Empty;

        [Output]
        public bool HasConflicts { get; set; }

        [Output]
        public ITaskItem[] ConflictingFiles { get; set; } = Array.Empty<ITaskItem>();

        [Output]
        public string CleanupCommand { get; set; } = string.Empty;

        [Output]
        public string InstallCommand { get; set; } = string.Empty;

        private static readonly Dictionary<string, string> LockFileToPackageManager = new()
        {
            { "bun.lockb", "bun" },
            { "pnpm-lock.yaml", "pnpm" },
            { "yarn.lock", "yarn" },
            { "package-lock.json", "npm" }
        };

        private static readonly Dictionary<string, string> InstallCommands = new()
        {
            { "bun", "bun install --frozen-lockfile" },
            { "pnpm", "pnpm install --frozen-lockfile" },
            { "yarn", "yarn install --frozen-lockfile" },
            { "npm", "npm ci" }
        };

        public override bool Execute()
        {
            try
            {
                if (string.IsNullOrEmpty(ViteProjectRoot) || !Directory.Exists(ViteProjectRoot))
                {
                    Log.LogError($"ViteProjectRoot '{ViteProjectRoot}' does not exist");
                    return false;
                }

                // Detect all lock files (much faster than XML ItemGroup processing)
                var detectedLockFiles = LockFileToPackageManager.Keys
                    .Select(lockFile => Path.Combine(ViteProjectRoot, lockFile))
                    .Where(File.Exists)
                    .ToList();

                Log.LogMessage(MessageImportance.Low, $"Detected {detectedLockFiles.Count} lock files in {ViteProjectRoot}");

                if (detectedLockFiles.Count == 0)
                {
                    // No lock files, check for package.json packageManager field
                    PackageManager = DetectFromPackageJson() ?? "npm";
                    InstallCommand = InstallCommands[PackageManager];
                    Log.LogMessage(MessageImportance.Normal, $"No lock files found, using {PackageManager} as default");
                    return true;
                }

                if (detectedLockFiles.Count == 1)
                {
                    // Single lock file - clean scenario
                    var lockFileName = Path.GetFileName(detectedLockFiles[0]);
                    PackageManager = LockFileToPackageManager[lockFileName];
                    InstallCommand = InstallCommands[PackageManager];
                    Log.LogMessage(MessageImportance.Normal, $"Detected package manager: {PackageManager} from {lockFileName}");
                    return true;
                }

                // Multiple lock files - conflict scenario
                return HandleConflicts(detectedLockFiles);
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to detect package manager: {ex.Message}");
                return false;
            }
        }

        private bool HandleConflicts(List<string> conflictingFiles)
        {
            HasConflicts = true;

            // Convert to ITaskItem array for MSBuild, sorted for predictable order
            ConflictingFiles = conflictingFiles
                .OrderBy(f => Path.GetFileName(f))
                .Select(f => new TaskItem(f))
                .ToArray();

            // Use priority order: bun → pnpm → yarn → npm
            var priorityOrder = new[] { "bun.lockb", "pnpm-lock.yaml", "yarn.lock", "package-lock.json" };
            var selectedLockFile = priorityOrder
                .FirstOrDefault(lockFile => conflictingFiles.Any(f => Path.GetFileName(f) == lockFile));

            if (selectedLockFile != null)
            {
                PackageManager = LockFileToPackageManager[selectedLockFile];
                InstallCommand = InstallCommands[PackageManager];
                CleanupCommand = GenerateCleanupCommand(PackageManager);

                var conflictFileNames = string.Join(", ", conflictingFiles.Select(Path.GetFileName));

                if (ConflictAction == "error")
                {
                    Log.LogError($"Multiple package manager lock files detected: {conflictFileNames}. This causes dependency conflicts and must be resolved before building.");
                }
                else
                {
                    Log.LogWarning($"Multiple package manager lock files detected: {conflictFileNames}. This may cause dependency conflicts.");
                }

                // Provide specific guidance
                Log.LogMessage(MessageImportance.High, $"💡 To resolve with {PackageManager}:");
                Log.LogMessage(MessageImportance.High, $"   {CleanupCommand}");
                Log.LogMessage(MessageImportance.Normal, $"💡 Then commit the updated {PackageManager} lock file to your repository.");

                // Return false for error, true for warning
                return ConflictAction != "error";
            }

            Log.LogError("Unable to resolve package manager from conflicting lock files");
            return false;
        }

        private string? DetectFromPackageJson()
        {
            var packageJsonPath = Path.Combine(ViteProjectRoot, "package.json");
            if (!File.Exists(packageJsonPath))
                return null;

            try
            {
                var packageJsonContent = File.ReadAllText(packageJsonPath);
                using var doc = JsonDocument.Parse(packageJsonContent);

                if (doc.RootElement.TryGetProperty("packageManager", out var packageManagerElement))
                {
                    var packageManagerValue = packageManagerElement.GetString();
                    if (!string.IsNullOrEmpty(packageManagerValue))
                    {
                        // Extract package manager name from "yarn@3.2.0" format
                        var atIndex = packageManagerValue.IndexOf('@');
                        var pmName = atIndex > 0 ? packageManagerValue.Substring(0, atIndex) : packageManagerValue;

                        if (InstallCommands.ContainsKey(pmName))
                        {
                            Log.LogMessage(MessageImportance.Normal, $"Using package manager from package.json: {pmName}");
                            return pmName;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogMessage(MessageImportance.Low, $"Could not parse package.json for packageManager field: {ex.Message}");
            }

            return null;
        }

        private string GenerateCleanupCommand(string chosenPackageManager)
        {
            // Only remove lock files that actually exist and aren't the chosen one
            var filesToRemove = LockFileToPackageManager.Keys
                .Where(lockFile => LockFileToPackageManager[lockFile] != chosenPackageManager)
                .Where(lockFile => File.Exists(Path.Combine(ViteProjectRoot, lockFile))) // Only existing files
                .OrderBy(lockFile => lockFile) // Sort alphabetically for predictable output
                .ToList();

            var rmCommand = string.Join(" ", filesToRemove);
            return $"rm {rmCommand} && {InstallCommands[chosenPackageManager]}";
        }
    }
}