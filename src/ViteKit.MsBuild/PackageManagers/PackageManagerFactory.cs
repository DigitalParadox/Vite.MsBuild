using System.IO;

namespace ViteKit.MsBuild.Tasks.PackageManagers
{
    /// <summary>
    /// Factory for creating package manager instances
    /// Detects package manager from lock files or package.json
    /// </summary>
    public class PackageManagerFactory
    {
        /// <summary>
        /// Creates a package manager instance by detecting from directory
        /// Priority: bun → pnpm → yarn → npm (default)
        /// </summary>
        public static IPackageManager Create(string directory)
        {
            // Check for lock files in priority order
            if (File.Exists(Path.Combine(directory, "bun.lockb")))
                return new BunPackageManager();
            
            if (File.Exists(Path.Combine(directory, "pnpm-lock.yaml")))
                return new PnpmPackageManager();
            
            if (File.Exists(Path.Combine(directory, "yarn.lock")))
                return new YarnPackageManager();
            
            // Default to npm (includes package-lock.json case and no lock file)
            return new NpmPackageManager();
        }

        /// <summary>
        /// Creates package manager from explicit name
        /// </summary>
        public static IPackageManager CreateByName(string name)
        {
            return name.ToLowerInvariant() switch
            {
                "npm" => new NpmPackageManager(),
                "pnpm" => new PnpmPackageManager(),
                "yarn" => new YarnPackageManager(),
                "bun" => new BunPackageManager(),
                _ => new NpmPackageManager() // default
            };
        }
    }
}
