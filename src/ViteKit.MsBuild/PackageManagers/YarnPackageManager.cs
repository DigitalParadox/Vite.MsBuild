using System.IO;

namespace ViteKit.MsBuild.Tasks.PackageManagers
{
    /// <summary>
    /// Yarn package manager implementation
    /// Supports yarn workspaces via package.json "workspaces" field
    /// </summary>
    public class YarnPackageManager : PackageManagerBase
    {
        public override string Name => "yarn";
        public override string LockFileName => "yarn.lock";

        public override string? FindPackageJson(string startDirectory, string gitRoot)
        {
            // Check for yarn workspace root (package.json with "workspaces" field)
            var workspaceRoot = FindWorkspaceRoot(startDirectory, gitRoot, dir =>
            {
                var packageJsonPath = Path.Combine(dir, "package.json");
                return File.Exists(packageJsonPath) && IsWorkspaceRoot(packageJsonPath);
            });

            if (workspaceRoot != null)
            {
                return Path.Combine(workspaceRoot, "package.json");
            }

            // Fallback to standard resolution
            return base.FindPackageJson(startDirectory, gitRoot);
        }

        public override string GetInstallCommand(string projectRoot, bool hasLockFile, bool useCiInstall = false)
        {
            // yarn install --frozen-lockfile: CI-optimized (strict lock file enforcement)
            // yarn install: Development-friendly (updates lock file if needed)
            if (useCiInstall && hasLockFile)
            {
                return "yarn install --frozen-lockfile";
            }
            return "yarn install";
        }

        public override string GetInstallInstructions()
        {
            return @"Install yarn:
  npm install -g yarn
  OR use Corepack: corepack enable && corepack prepare yarn@stable --activate";
        }
    }
}
