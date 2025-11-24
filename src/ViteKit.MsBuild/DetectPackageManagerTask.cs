using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ViteKit.MsBuild.Tasks
{
    /// <summary>
    /// High-performance C# task to replace 300+ lines of XML package manager detection logic
    /// Detects package manager from lock files, handles conflicts, provides specific guidance
    /// Supports monorepo scenarios by walking up from vite config to find package.json
    /// </summary>
    public class DetectPackageManagerTask : Task
    {
        // Static cache for package.json parsing results (survives across MSBuild invocations in same process)
        private static readonly Dictionary<string, (DateTime lastModified, string? packageManager)> _packageJsonCache = [];
        
        [Required]
        public string ViteProjectRoot { get; set; } = string.Empty;

        public string ViteConfigFile { get; set; } = string.Empty; // Optional: for better package.json discovery

        public string ConflictAction { get; set; } = "warn"; // warn, error

        public bool SkipPackageManagerValidation { get; set; } = false; // Skip PATH validation (useful for testing/CI without package managers installed)

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
        
        [Output]
        public string ResolvedPackageJsonPath { get; set; } = string.Empty; // Path where package.json was found

        private static readonly Dictionary<string, string> LockFileToPackageManager = new()
        {
            { "bun.lockb", "bun" },
            { "pnpm-lock.yaml", "pnpm" },
            { "yarn.lock", "yarn" },
            { "package-lock.json", "npm" }
        };

        // Use the new package manager abstraction for command building
        private string GetInstallCommandForPackageManager(string packageManagerName, bool hasLockFile)
        {
            var pm = PackageManagers.PackageManagerFactory.CreateByName(packageManagerName);
            return pm.GetInstallCommand(ViteProjectRoot, hasLockFile);
        }

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
                    PackageManager = DetectFromPackageJson(ViteProjectRoot) ?? "npm";
                    
                    // No lock file exists, use regular install (will generate lock file)
                    InstallCommand = GetInstallCommandForPackageManager(PackageManager, false);
                    Log.LogMessage(MessageImportance.Normal, $"No lock files found, using {PackageManager} with regular install");
                    
                    return ValidatePackageManagerInstalled();
                }

                if (detectedLockFiles.Count == 1)
                {
                    // Single lock file - clean scenario
                    var lockFileName = Path.GetFileName(detectedLockFiles[0]);
                    PackageManager = LockFileToPackageManager[lockFileName];
                    
                    // Lock file exists, use frozen install
                    InstallCommand = GetInstallCommandForPackageManager(PackageManager, true);
                    Log.LogMessage(MessageImportance.Normal, $"Detected package manager: {PackageManager} from {lockFileName}");
                    
                    return ValidatePackageManagerInstalled();
                }

                // Multiple lock files - conflict scenario
                return HandleConflicts(detectedLockFiles, ViteProjectRoot);
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to detect package manager: {ex.Message}");
                return false;
            }
        }

        private bool HandleConflicts(List<string> conflictingFiles, string packageJsonDir)
        {
            HasConflicts = true;

            // Convert to ITaskItem array for MSBuild, sorted for predictable order
            ConflictingFiles = conflictingFiles
                .OrderBy(f => Path.GetFileName(f))
                .Select(f => new TaskItem(f) as ITaskItem)
                .ToArray();

            // Use priority order: bun → pnpm → yarn → npm
            var priorityOrder = new[] { "bun.lockb", "pnpm-lock.yaml", "yarn.lock", "package-lock.json" };
            var selectedLockFile = priorityOrder
                .FirstOrDefault(lockFile => conflictingFiles.Any(f => Path.GetFileName(f) == lockFile));

            if (selectedLockFile != null)
            {
                PackageManager = LockFileToPackageManager[selectedLockFile];
                InstallCommand = GetInstallCommandForPackageManager(PackageManager, true);
                CleanupCommand = GenerateCleanupCommand(PackageManager, packageJsonDir);

                var conflictFileNames = string.Join(", ", conflictingFiles.Select(Path.GetFileName));

                // ConflictAction: "error" = fail build, "warn" = log warning, "none"/"" = suppress
                if (ConflictAction == "error")
                {
                    Log.LogError($"Multiple package manager lock files detected: {conflictFileNames}. This causes dependency conflicts and must be resolved before building.");
                    
                    // Provide specific guidance
                    Log.LogMessage(MessageImportance.High, $"[INFO] To resolve with {PackageManager}:");
                    Log.LogMessage(MessageImportance.High, $"   {CleanupCommand}");
                    Log.LogMessage(MessageImportance.Normal, $"[INFO] Then commit the updated {PackageManager} lock file to your repository.");
                    
                    return false;
                }
                else if (ConflictAction == "warn")
                {
                    Log.LogWarning($"Multiple package manager lock files detected: {conflictFileNames}. This may cause dependency conflicts.");
                    
                    // Provide specific guidance
                    Log.LogMessage(MessageImportance.High, $"[INFO] To resolve with {PackageManager}:");
                    Log.LogMessage(MessageImportance.High, $"   {CleanupCommand}");
                    Log.LogMessage(MessageImportance.Normal, $"[INFO] Then commit the updated {PackageManager} lock file to your repository.");
                }
                // else: "none" or empty = suppress message (user knows what they're doing)

                return ValidatePackageManagerInstalled();
            }

            Log.LogError("Unable to resolve package manager from conflicting lock files");
            return false;
        }

        private string? DetectFromPackageJson(string packageJsonDir)
        {
            var packageJsonPath = Path.Combine(packageJsonDir, "package.json");
            if (!File.Exists(packageJsonPath))
                return null;

            try
            {
                var fileInfo = new FileInfo(packageJsonPath);
                var lastModified = fileInfo.LastWriteTimeUtc;

                // Check cache first (performance optimization - avoid re-parsing unchanged files)
                if (_packageJsonCache.TryGetValue(packageJsonPath, out var cached))
                {
                    if (cached.lastModified == lastModified)
                    {
                        Log.LogMessage(MessageImportance.Low, "⚡ Using cached package.json parse result");
                        return cached.packageManager;
                    }
                }

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

                        // Validate it's a known package manager
                        var validManagers = new[] { "npm", "pnpm", "yarn", "bun" };
                        if (validManagers.Contains(pmName.ToLowerInvariant()))
                        {
                            Log.LogMessage(MessageImportance.Normal, $"Using package manager from package.json: {pmName}");
                            
                            // Cache the result
                            _packageJsonCache[packageJsonPath] = (lastModified, pmName);
                            return pmName;
                        }
                    }
                }

                // Cache null result too (file exists but no packageManager field)
                _packageJsonCache[packageJsonPath] = (lastModified, null);
            }
            catch (Exception ex)
            {
                Log.LogMessage(MessageImportance.Low, $"Could not parse package.json for packageManager field: {ex.Message}");
            }

            return null;
        }

        private string GenerateCleanupCommand(string chosenPackageManager, string packageJsonDir)
        {
            // Only remove lock files that actually exist and aren't the chosen one
            var filesToRemove = LockFileToPackageManager.Keys
                .Where(lockFile => LockFileToPackageManager[lockFile] != chosenPackageManager)
                .Where(lockFile => File.Exists(Path.Combine(packageJsonDir, lockFile))) // Only existing files
                .OrderBy(lockFile => lockFile) // Sort alphabetically for predictable output
                .ToList();

            var rmCommand = string.Join(" ", filesToRemove);
            return $"rm {rmCommand} && {GetInstallCommandForPackageManager(chosenPackageManager, true)}";
        }

        private bool ValidatePackageManagerInstalled()
        {
            if (SkipPackageManagerValidation)
            {
                Log.LogMessage(MessageImportance.Low, $"Skipping package manager validation for '{PackageManager}'");
                return true;
            }

            try
            {
                // Check if package manager is in PATH
                var processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = OperatingSystem.IsWindows() ? "where" : "which",
                    Arguments = PackageManager,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(processStartInfo);
                if (process == null)
                {
                    LogPackageManagerNotFound();
                    return false;
                }

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    LogPackageManagerNotFound();
                    return false;
                }

                Log.LogMessage(MessageImportance.Low, $"✓ Package manager '{PackageManager}' is installed");
                return true;
            }
            catch (Exception ex)
            {
                Log.LogMessage(MessageImportance.Low, $"Could not verify package manager installation: {ex.Message}");
                // Don't fail on verification errors, let the actual install command fail if needed
                return true;
            }
        }

        private void LogPackageManagerNotFound()
        {
            Log.LogError($"Package manager '{PackageManager}' is not installed or not in PATH.");
            Log.LogMessage(MessageImportance.High, $"[ERROR] To fix this issue, install {PackageManager}:");
            
            switch (PackageManager)
            {
                case "pnpm":
                    Log.LogMessage(MessageImportance.High, "   npm install -g pnpm");
                    Log.LogMessage(MessageImportance.High, "   OR use Corepack: corepack enable && corepack prepare pnpm@latest --activate");
                    break;
                case "yarn":
                    Log.LogMessage(MessageImportance.High, "   npm install -g yarn");
                    Log.LogMessage(MessageImportance.High, "   OR use Corepack: corepack enable && corepack prepare yarn@stable --activate");
                    break;
                case "bun":
                    if (OperatingSystem.IsWindows())
                    {
                        Log.LogMessage(MessageImportance.High, "   powershell -c \"irm bun.sh/install.ps1 | iex\"");
                    }
                    else
                    {
                        Log.LogMessage(MessageImportance.High, "   curl -fsSL https://bun.sh/install | bash");
                    }
                    break;
                case "npm":
                    Log.LogMessage(MessageImportance.High, "   npm is included with Node.js. Install Node.js from https://nodejs.org");
                    break;
            }
        }
    }
}