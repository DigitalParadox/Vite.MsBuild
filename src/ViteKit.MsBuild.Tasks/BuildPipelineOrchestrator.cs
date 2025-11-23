using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ViteKit.MsBuild.Tasks.PackageManagers;
using ViteKit.MsBuild.Tasks.BuildPipeline;
using ViteKit.MsBuild.Tasks.Utilities;

namespace ViteKit.MsBuild.Tasks
{
    /// <summary>
    /// Orchestrates the build pipeline by analyzing configurations and generating ordered task lists
    /// Separates concerns: package restoration vs Vite builds
    /// Supports multi-package.json monorepos with proper deduplication
    /// </summary>
    public class BuildPipelineOrchestrator
    {
        public bool EnableColors { get; }
        private readonly string _viteProjectRoot;
        private readonly string _gitRoot;
        private readonly IViteCommandFactory _commandFactory;

        public BuildPipelineOrchestrator(string viteProjectRoot, bool enableColors = false)
        {
            EnableColors = enableColors;
            _viteProjectRoot = viteProjectRoot;
            _gitRoot = GitUtilities.FindGitRoot(viteProjectRoot);
            _commandFactory = new ViteCommandFactory();
        }

        /// <summary>
        /// Builds package restore tasks for all configurations
        /// Deduplicates by package.json location - only one install per unique package.json
        /// </summary>
        public List<PackageRestoreTask> BuildRestoreTasks(List<ViteConfigInfo> configurations)
        {
            var tasks = new List<PackageRestoreTask>();
            var processedPackageJsons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var config in configurations)
            {
                var configDir = Path.GetDirectoryName(Path.GetFullPath(config.ConfigFile)) 
                    ?? _viteProjectRoot;

                // 1. Detect package manager from lock files near config
                var packageManager = PackageManagerFactory.Create(configDir);

                // 2. Use PM-specific logic to find package.json
                var packageJsonPath = packageManager.FindPackageJson(configDir, _gitRoot);

                if (packageJsonPath == null)
                {
                    throw new InvalidOperationException(
                        $"No package.json found for config '{config.ConfigFile}'. " +
                        $"Searched from '{configDir}' up to git root '{_gitRoot}'.");
                }

                // 3. Deduplicate - only one install per package.json
                if (processedPackageJsons.Contains(packageJsonPath))
                {
                    continue;
                }
                processedPackageJsons.Add(packageJsonPath);

                // 4. Check if lock file exists
                var packageJsonDir = Path.GetDirectoryName(packageJsonPath)!;
                var lockFilePath = Path.Combine(packageJsonDir, packageManager.LockFileName);
                var hasLockFile = File.Exists(lockFilePath);

                // 5. Validate package manager is installed
                if (!packageManager.IsInstalled())
                {
                    throw new InvalidOperationException(
                        $"Package manager '{packageManager.Name}' is not installed or not in PATH.\n" +
                        packageManager.GetInstallInstructions());
                }

                // 6. Build install command using PM-specific logic
                var installCommand = packageManager.GetInstallCommand(packageJsonDir, hasLockFile);
                var parts = installCommand.Split(' ', 2); // Split "npm ci" into ["npm", "ci"]

                tasks.Add(new PackageRestoreTask
                {
                    TaskId = $"Restore_{Path.GetFileName(packageJsonDir)}",
                    Executable = parts[0],
                    Arguments = parts.Length > 1 ? parts[1] : string.Empty,
                    WorkingDirectory = packageManager.GetWorkingDirectory(packageJsonPath),
                    Environment = packageManager.GetEnvironmentVariables(),
                    PackageManager = packageManager.Name,
                    PackageJsonPath = packageJsonPath,
                    HasLockFile = hasLockFile,
                    Description = hasLockFile
                        ? $"Restoring packages for {Path.GetFileName(packageJsonDir)} using {packageManager.Name} (frozen lockfile)"
                        : $"Installing packages for {Path.GetFileName(packageJsonDir)} using {packageManager.Name} (will generate lockfile)"
                });
            }

            return tasks;
        }

        /// <summary>
        /// Builds Vite build tasks in dependency order
        /// Each task depends on its parent build completing first
        /// </summary>
        private List<ViteBuildTask> BuildViteTasks(
            List<ViteConfigInfo> configurations,
            string packageManager,
            string viteLogLevel,
            bool enableColors)
        {
            var tasks = new List<ViteBuildTask>();
            var buildIdToTaskId = new Dictionary<string, string>();

            foreach (var config in configurations)
            {
                var taskId = $"Build_{config.BuildId}";
                buildIdToTaskId[config.BuildId] = taskId;

                // Resolve dependencies to task IDs
                var dependsOn = new List<string>();
                if (!string.IsNullOrEmpty(config.DependsOn))
                {
                    var deps = config.DependsOn.Split(',')
                        .Select(d => d.Trim())
                        .Where(d => !string.IsNullOrEmpty(d));

                    foreach (var dep in deps)
                    {
                        if (buildIdToTaskId.TryGetValue(dep, out var depTaskId))
                        {
                            dependsOn.Add(depTaskId);
                        }
                    }
                }

                // Build Vite command
                IViteCommand viteCommand = BuildViteCommand(
                    config,
                    packageManager,
                    viteLogLevel,
                    enableColors);
                // Build the complete command arguments (includes --config, --mode, etc.)
                var fullCommand = viteCommand.ToString();
                var arguments = fullCommand.Substring(viteCommand.Executable.Length).Trim();
                
                tasks.Add(new ViteBuildTask
                {
                    TaskId = taskId,
                    Executable = viteCommand.Executable,
                    Arguments = arguments,
                    WorkingDirectory = viteCommand.WorkingDirectory,
                    Environment = viteCommand.Environment,
                    DependsOn = dependsOn,
                    BuildId = config.BuildId,
                    ConfigPath = config.ConfigFile,
                    Mode = config.Mode,
                    OutputDir = config.OutputDir,
                    Description = $"Building {config.BuildId} ({config.ConfigFile})"
                });
            }

            return tasks;
        }

        /// <summary>
        /// Builds a complete pipeline: restore tasks + build tasks
        /// </summary>
        public (List<PackageRestoreTask> RestoreTasks, List<ViteBuildTask> BuildTasks) BuildCompletePipeline(
            List<ViteConfigInfo> configurations,
            string packageManager,
            string viteLogLevel,
            bool enableColors)
        {
            var restoreTasks = BuildRestoreTasks(configurations);
            var buildTasks = BuildViteTasks(
                configurations,
                packageManager,
                viteLogLevel,
                enableColors);

            return (restoreTasks, buildTasks);
        }

        private IViteCommand BuildViteCommand(
            ViteConfigInfo config,
            string packageManager,
            string viteLogLevel,
            bool enableColors)
        {
            var pm = PackageManagerFactory.CreateByName(packageManager);
            var pmEnum = Enum.Parse<PackageManager>(pm.Name, ignoreCase: true);
            IViteCommandBuilder builder;

            // Determine command type from config
            if (!string.IsNullOrEmpty(config.BuildScript))
            {
                builder = _commandFactory.CreateScriptBuilder(pmEnum, config.BuildScript);
            }
            else
            {
                builder = _commandFactory.CreateBuilder(ViteCommandType.DirectTool, pmEnum);
            }

            return builder
                .WithConfigPath(config.ConfigFile)
                .WithMode(config.Mode)
                .WithOutputDir(config.OutputDir)
                .WithLogLevel(viteLogLevel)
                .WithColors(enableColors)
                .WithWorkingDirectory(_viteProjectRoot)
                .Build();
        }
    }
}
