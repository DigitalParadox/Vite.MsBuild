using System.IO;

namespace ViteKit.MsBuild.Tasks.PackageManagers
{
    /// <summary>
    /// pnpm package manager implementation
    /// Supports pnpm workspaces via pnpm-workspace.yaml
    /// </summary>
    public class PnpmPackageManager : PackageManagerBase
    {
        public override string Name => "pnpm";
        public override string LockFileName => "pnpm-lock.yaml";

        public override string? FindPackageJson(string startDirectory, string gitRoot)
        {
            // Check for pnpm workspace root (pnpm-workspace.yaml)
            var workspaceRoot = FindWorkspaceRoot(startDirectory, gitRoot, dir =>
                File.Exists(Path.Combine(dir, "pnpm-workspace.yaml")));

            if (workspaceRoot != null)
            {
                var packageJsonPath = Path.Combine(workspaceRoot, "package.json");
                if (File.Exists(packageJsonPath))
                {
                    return packageJsonPath;
                }
            }

            // Fallback to standard resolution
            return base.FindPackageJson(startDirectory, gitRoot);
        }

        public override string GetInstallCommand(string projectRoot, bool hasLockFile, bool useCiInstall = false)
        {
            // pnpm install --frozen-lockfile: CI-optimized (strict lock file enforcement)
            // pnpm install: Development-friendly (updates lock file if needed)
            if (useCiInstall && hasLockFile)
            {
                return "pnpm install --frozen-lockfile";
            }
            return "pnpm install";
        }

        public override string GetInstallInstructions()
        {
            return @"Install pnpm:
  npm install -g pnpm
  OR use Corepack: corepack enable && corepack prepare pnpm@latest --activate";
        }
    }
}
