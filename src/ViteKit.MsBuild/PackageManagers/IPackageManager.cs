using System.Collections.Generic;

namespace ViteKit.MsBuild.Tasks.PackageManagers
{
    /// <summary>
    /// Abstraction for package manager-specific behavior
    /// Each implementation encapsulates discovery rules, command building, and validation
    /// </summary>
    public interface IPackageManager
    {
        /// <summary>
        /// Package manager name (npm, pnpm, yarn, bun)
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Lock file name for this package manager
        /// </summary>
        string LockFileName { get; }
        
        /// <summary>
        /// Finds the package.json that governs the given directory
        /// Follows package-manager-specific resolution rules (workspaces, etc.)
        /// </summary>
        /// <param name="startDirectory">Directory to start search from (e.g., vite.config.ts location)</param>
        /// <param name="gitRoot">Repository root (stop boundary)</param>
        /// <returns>Path to package.json, or null if not found</returns>
        string? FindPackageJson(string startDirectory, string gitRoot);
        
        /// <summary>
        /// Checks if the given package.json is a workspace root
        /// </summary>
        bool IsWorkspaceRoot(string packageJsonPath);
        
        /// <summary>
        /// Gets the install command for this package manager
        /// </summary>
        /// <param name="packageJsonDirectory">Directory containing package.json</param>
        /// <param name="hasLockFile">Whether lock file exists</param>
        /// <param name="useCiInstall">Use CI-optimized install (npm ci, --frozen-lockfile). Default false for faster local builds.</param>
        /// <returns>Install command (e.g., "npm install" or "npm ci")</returns>
        string GetInstallCommand(string packageJsonDirectory, bool hasLockFile, bool useCiInstall = false);
        
        /// <summary>
        /// Gets the working directory for install command execution
        /// Usually the directory containing package.json
        /// </summary>
        string GetWorkingDirectory(string packageJsonPath);
        
        /// <summary>
        /// Checks if this package manager is installed and available in PATH
        /// </summary>
        bool IsInstalled();
        
        /// <summary>
        /// Gets user-friendly installation instructions
        /// </summary>
        string GetInstallInstructions();
        
        /// <summary>
        /// Environment variables to set for commands (if any)
        /// </summary>
        Dictionary<string, string> GetEnvironmentVariables();
    }
}
