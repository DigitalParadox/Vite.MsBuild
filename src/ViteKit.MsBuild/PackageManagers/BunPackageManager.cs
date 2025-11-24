using System;

namespace ViteKit.MsBuild.Tasks.PackageManagers
{
    /// <summary>
    /// Bun package manager implementation
    /// Similar to npm but faster with different lock file
    /// </summary>
    public class BunPackageManager : PackageManagerBase
    {
        public override string Name => "bun";
        public override string LockFileName => "bun.lockb";

        public override string GetInstallCommand(string projectRoot, bool hasLockFile, bool useCiInstall = false)
        {
            // bun install --frozen-lockfile: CI-optimized (strict lock file enforcement)
            // bun install: Development-friendly (updates lock file if needed)
            if (useCiInstall && hasLockFile)
            {
                return "bun install --frozen-lockfile";
            }
            return "bun install";
        }

        public override string GetInstallInstructions()
        {
            if (OperatingSystem.IsWindows())
            {
                return @"Install bun:
  powershell -c ""irm bun.sh/install.ps1 | iex""";
            }
            else
            {
                return @"Install bun:
  curl -fsSL https://bun.sh/install | bash";
            }
        }
    }
}
