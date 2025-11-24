using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace ViteKit.MsBuild.Tasks.PackageManagers
{
    /// <summary>
    /// Base class for package manager implementations
    /// Provides common functionality like package.json discovery and validation
    /// </summary>
    public abstract class PackageManagerBase : IPackageManager
    {
        public abstract string Name { get; }
        public abstract string LockFileName { get; }

        public virtual string? FindPackageJson(string startDirectory, string gitRoot)
        {
            // Standard Node.js resolution: walk up to first package.json
            return WalkUpToPackageJson(startDirectory, gitRoot);
        }

        public virtual bool IsWorkspaceRoot(string packageJsonPath)
        {
            try
            {
                var content = File.ReadAllText(packageJsonPath);
                using var doc = JsonDocument.Parse(content);
                
                // Check for "workspaces" field (npm/yarn standard)
                return doc.RootElement.TryGetProperty("workspaces", out _);
            }
            catch
            {
                return false;
            }
        }

        public abstract string GetInstallCommand(string packageJsonDirectory, bool hasLockFile, bool useCiInstall = false);

        public virtual string GetWorkingDirectory(string packageJsonPath)
        {
            return Path.GetDirectoryName(packageJsonPath) ?? packageJsonPath;
        }

        public virtual bool IsInstalled()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = OperatingSystem.IsWindows() ? "where" : "which",
                    Arguments = Name,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null) return false;

                process.WaitForExit(3000); // 3 second timeout
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        public abstract string GetInstallInstructions();

        public virtual Dictionary<string, string> GetEnvironmentVariables()
        {
            return new Dictionary<string, string>();
        }

        /// <summary>
        /// Walks up directory tree to find package.json
        /// Stops at gitRoot or when package.json is found
        /// </summary>
        protected string? WalkUpToPackageJson(string startDirectory, string gitRoot)
        {
            var current = new DirectoryInfo(startDirectory);
            var gitRootInfo = new DirectoryInfo(gitRoot);

            while (current != null)
            {
                var packageJsonPath = Path.Combine(current.FullName, "package.json");
                if (File.Exists(packageJsonPath))
                {
                    return packageJsonPath;
                }

                // Stop at git root
                if (current.FullName.Equals(gitRootInfo.FullName, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                current = current.Parent;
            }

            return null;
        }

        /// <summary>
        /// Finds workspace root by walking up and checking for workspace indicators
        /// </summary>
        protected string? FindWorkspaceRoot(string startDirectory, string gitRoot, Func<string, bool> isWorkspaceRoot)
        {
            var current = new DirectoryInfo(startDirectory);
            var gitRootInfo = new DirectoryInfo(gitRoot);

            while (current != null)
            {
                if (isWorkspaceRoot(current.FullName))
                {
                    return current.FullName;
                }

                // Stop at git root
                if (current.FullName.Equals(gitRootInfo.FullName, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                current = current.Parent;
            }

            return null;
        }
    }
}
