namespace ViteKit.MsBuild.Tasks.PackageManagers
{
    /// <summary>
    /// npm package manager implementation
    /// Default package manager for Node.js projects
    /// </summary>
    public class NpmPackageManager : PackageManagerBase
    {
        public override string Name => "npm";
        public override string LockFileName => "package-lock.json";

        public override string GetInstallCommand(string projectRoot, bool hasLockFile, bool useCiInstall = false)
        {
            // npm ci: CI-optimized (clean install, strict), requires lock file
            // npm install: Development-friendly (incremental, forgiving)
            if (useCiInstall && hasLockFile)
            {
                return "npm ci";
            }
            return "npm install";
        }

        public override string GetInstallInstructions()
        {
            return "npm is included with Node.js. Install Node.js from https://nodejs.org";
        }
    }
}
