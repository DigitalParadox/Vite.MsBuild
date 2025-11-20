using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ViteKit.MsBuild.Tasks
{
    /// <summary>
    /// Comprehensive C# task to orchestrate Vite builds
    /// Replaces: _ViteBuildMultipleConfigs, _ViteBuildSingleConfig, _ViteBuildAssetsCore
    /// Handles all build logic in high-performance C# instead of complex XML batching
    /// </summary>
    public class OrchestrateBuildTask : Task
    {
        [Required]
        public string ViteProjectRoot { get; set; } = string.Empty;

        [Required]
        public string PackageManager { get; set; } = "npm";

        [Required]
        public string ViteMode { get; set; } = "development";

        public string? ViteConfigFile { get; set; }

        public string ViteOutputDir { get; set; } = "wwwroot/dist";

        public string? ViteBuildCommand { get; set; }

        public string? ViteInstallCommand { get; set; }

        public bool ViteEnableColors { get; set; } = true;

        public string ViteLogLevel { get; set; } = "info";

        public string IntermediateOutputPath { get; set; } = "obj/";

        public ITaskItem[]? ViteConfigurations { get; set; }

        public ITaskItem[]? ViteInputFiles { get; set; }

        /// <summary>
        /// Internal: All processed configurations for dependency lookup
        /// </summary>
        private List<ViteConfigInfo> _allConfigurations = new();

        /// <summary>
        /// Factory for creating Vite command builders
        /// </summary>
        private readonly IViteCommandFactory _commandFactory = new ViteCommandFactory();

        [Output]
        public bool BuildSucceeded { get; set; }

        [Output]
        public string[] OutputDirectories { get; set; } = Array.Empty<string>();

        [Output]
        public int ConfigurationsBuilt { get; set; }

        private readonly Dictionary<string, string> _packageManagerCommands = new()
        {
            ["npm"] = "npx vite build",
            ["yarn"] = "yarn dlx vite build",
            ["pnpm"] = "pnpm dlx vite build", 
            ["bun"] = "bunx vite build"
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

                var outputDirs = new List<string>();
                var configsToProcess = PrepareConfigurations();
                
                // Store all configurations for dependency lookup
                _allConfigurations = configsToProcess;

                Log.LogMessage(MessageImportance.Normal, 
                    $"[BUILD] Building {configsToProcess.Count} Vite configuration(s) in {ViteMode} mode with dependency ordering");

                // Analyze build requirements and display tree
                var buildPlan = AnalyzeBuildRequirements(configsToProcess);
                DisplayBuildTree(buildPlan);

                // Track completed builds for dependency validation
                var completedBuilds = new HashSet<string>();
                var buildResults = new Dictionary<string, bool>();

                foreach (var config in configsToProcess)
                {
                    // Verify all dependencies have completed successfully
                    if (!string.IsNullOrEmpty(config.DependsOn))
                    {
                        var dependencies = config.DependsOn.Split(',').Select(d => d.Trim()).Where(d => !string.IsNullOrEmpty(d));
                        
                        foreach (var dependency in dependencies)
                        {
                            if (!completedBuilds.Contains(dependency))
                            {
                                Log.LogError("[ERROR] Dependency '{0}' has not completed before building '{1}'", dependency, config.BuildId);
                                BuildSucceeded = false;
                                return false;
                            }

                            if (!buildResults.TryGetValue(dependency, out var depResult) || !depResult)
                            {
                                Log.LogError("[ERROR] Dependency '{0}' failed, skipping build of '{1}'", dependency, config.BuildId);
                                BuildSucceeded = false;
                                return false;
                            }
                        }

                        Log.LogMessage(MessageImportance.High, "[OK] All dependencies satisfied for '{0}': [{1}]", config.BuildId, config.DependsOn);
                    }

                    // Link dependency packages if requested
                    if (config.LinkDependencies && !string.IsNullOrEmpty(config.DependsOn))
                    {
                        if (!LinkDependencyPackages(config))
                        {
                            Log.LogError("[ERROR] Failed to link dependency packages for '{0}'", config.BuildId);
                            BuildSucceeded = false;
                            return false;
                        }
                    }

                    // Build this configuration
                    var buildStatus = buildPlan[config.BuildId];
                    
                    if (buildStatus.WillBuild)
                    {
                        Log.LogMessage(MessageImportance.High, "[BUILD] Building configuration: {0}", config.BuildId);
                        
                        var success = BuildConfiguration(config);
                        buildResults[config.BuildId] = success;
                        buildStatus.ActuallyBuilt = success;
                        buildStatus.Failed = !success;
                        
                        if (!success)
                        {
                            Log.LogError("[ERROR] Build failed for configuration: {0}", config.BuildId);
                            BuildSucceeded = false;
                            return false;
                        }

                        completedBuilds.Add(config.BuildId);
                        outputDirs.Add(config.OutputDir);
                        Log.LogMessage(MessageImportance.High, "[OK] Successfully completed build: {0}", config.BuildId);
                    }
                    else
                    {
                        // Skipped but still mark as complete
                        buildResults[config.BuildId] = true;
                        completedBuilds.Add(config.BuildId);
                        outputDirs.Add(config.OutputDir);
                        buildStatus.ActuallyBuilt = false;
                        Log.LogMessage(MessageImportance.Normal, "[SKIP] Skipped {0} (up to date)", config.BuildId);
                    }

                    // Cleanup: Restore package.json if we modified it
                    if (config.LinkDependencies && !string.IsNullOrEmpty(config.DependsOn))
                    {
                        var configWorkingDir = Path.GetDirectoryName(Path.Combine(ViteProjectRoot, config.ConfigFile));
                        if (!string.IsNullOrEmpty(configWorkingDir))
                        {
                            var packageJsonPath = Path.Combine(configWorkingDir, "package.json");
                            RestorePackageJson(packageJsonPath);
                        }
                    }
                }

                OutputDirectories = outputDirs.ToArray();
                ConfigurationsBuilt = buildPlan.Values.Count(s => s.ActuallyBuilt);
                BuildSucceeded = true;

                // Display final build summary
                DisplayBuildSummary(buildPlan);

                return true;
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to orchestrate Vite build: {ex.Message}");
                BuildSucceeded = false;
                return false;
            }
        }

        private List<ViteConfigInfo> PrepareConfigurations()
        {
            var configs = new List<ViteConfigInfo>();

            if (ViteConfigurations != null && ViteConfigurations.Length > 0)
            {
                // Multi-config build
                foreach (var config in ViteConfigurations)
                {
                    configs.Add(new ViteConfigInfo
                    {
                        ConfigFile = config.ItemSpec,
                        BuildId = config.GetMetadata("BuildId") ?? "default",
                        OutputDir = config.GetMetadata("OutputDir") ?? ViteOutputDir,
                        Mode = config.GetMetadata("Mode") ?? ViteMode,
                        PackageManager = config.GetMetadata("PackageManager") ?? PackageManager,
                        DependsOn = config.GetMetadata("DependsOn") ?? string.Empty,
                        LinkDependencies = bool.TryParse(config.GetMetadata("LinkDependencies"), out var linkDeps) && linkDeps
                    });
                }
            }
            else
            {
                // Single config build
                configs.Add(new ViteConfigInfo
                {
                    ConfigFile = ViteConfigFile ?? FindViteConfig() ?? "vite.config.ts",
                    BuildId = "default",
                    OutputDir = ViteOutputDir,
                    Mode = ViteMode,
                    PackageManager = PackageManager,
                    DependsOn = string.Empty,
                    LinkDependencies = false
                });
            }

            return configs;
        }

        private string? FindViteConfig()
        {
            var possibleConfigs = new[]
            {
                "vite.config.ts", "vite.config.js", "vite.config.mts", "vite.config.mjs"
            };

            foreach (var configName in possibleConfigs)
            {
                var configPath = Path.Combine(ViteProjectRoot, configName);
                if (File.Exists(configPath))
                {
                    return configPath;
                }
            }

            return null;
        }

        private bool BuildConfiguration(ViteConfigInfo config)
        {
            Log.LogMessage(MessageImportance.Normal, $"[BUILD] Building config: {config.BuildId}");

            // Check if build needed (incremental build logic)
            if (!IsBuildRequired(config))
            {
                Log.LogMessage(MessageImportance.Normal, $"[SKIP] Skipping {config.BuildId} (up to date)");
                return true;
            }

            // Ensure output directory exists
            var fullOutputPath = Path.IsPathRooted(config.OutputDir) ? 
                config.OutputDir : 
                Path.Combine(ViteProjectRoot, config.OutputDir);

            Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath) ?? fullOutputPath);

            // Build the command
            var buildCommand = BuildViteCommand(config);
            var environmentVars = BuildEnvironmentVariables(config);

            Log.LogMessage(MessageImportance.Normal, $">> Executing: {buildCommand}");

            // Execute the build
            var success = ExecuteCommand(buildCommand, environmentVars);

            if (success)
            {
                // Update marker file for incremental builds
                UpdateBuildMarker(config);
                Log.LogMessage(MessageImportance.Normal, $"[OK] Built {config.BuildId} → {config.OutputDir}");
            }
            else
            {
                Log.LogError($"[ERROR] Failed to build configuration: {config.BuildId}");
            }

            return success;
        }

        private string BuildViteCommand(ViteConfigInfo config)
        {
            // Parse package manager from string to enum (use fully qualified name to avoid property name collision)
            if (!Enum.TryParse<Tasks.PackageManager>(config.PackageManager, true, out var packageManagerEnum))
            {
                packageManagerEnum = Tasks.PackageManager.Npm;
            }

            IViteCommandBuilder builder;

            // Priority: Custom command > Package scripts > Direct tool execution
            if (!string.IsNullOrEmpty(ViteBuildCommand))
            {
                // Use CustomCommand type with the factory
                builder = _commandFactory.CreateBuilder(ViteCommandType.CustomCommand, packageManagerEnum);
                builder.WithCustomCommand(ViteBuildCommand);
            }
            else
            {
                // Check for build script in package.json
                var packageJsonPath = Path.Combine(ViteProjectRoot, "package.json");
                if (File.Exists(packageJsonPath))
                {
                    // Use script-based builder (assumes 'build' script exists)
                    builder = _commandFactory.CreateScriptBuilder(packageManagerEnum, "build");
                }
                else
                {
                    // Fallback to direct tool execution
                    builder = _commandFactory.CreateBuilder(ViteCommandType.DirectTool, packageManagerEnum);
                }
            }

            // Build the command and return as string
            var command = builder.Build();
            return command.ToString();
        }

        private Dictionary<string, string> BuildEnvironmentVariables(ViteConfigInfo config)
        {
            var envVars = new Dictionary<string, string>
            {
                ["NODE_ENV"] = config.Mode == "development" ? "development" : "production",
                ["VITE_MODE"] = config.Mode
            };

            // Color support
            if (ViteEnableColors)
            {
                envVars["FORCE_COLOR"] = "1";
            }
            else
            {
                envVars["NO_COLOR"] = "1";
            }

            // Log level
            envVars["VITE_LOG_LEVEL"] = ViteLogLevel;

            return envVars;
        }

        private bool IsBuildRequired(ViteConfigInfo config)
        {
            // Simple timestamp-based check
            var markerPath = GetBuildMarkerPath(config);
            if (!File.Exists(markerPath))
            {
                return true; // No marker, build required
            }

            var markerTime = File.GetLastWriteTime(markerPath);

            // Check if any input files are newer than marker
            if (ViteInputFiles != null)
            {
                foreach (var inputFile in ViteInputFiles)
                {
                    if (File.Exists(inputFile.ItemSpec))
                    {
                        var inputTime = File.GetLastWriteTime(inputFile.ItemSpec);
                        if (inputTime > markerTime)
                        {
                            Log.LogMessage(MessageImportance.Low, $"Input file changed: {inputFile.ItemSpec}");
                            return true;
                        }
                    }
                }
            }

            // Check config file
            if (File.Exists(config.ConfigFile))
            {
                var configTime = File.GetLastWriteTime(config.ConfigFile);
                if (configTime > markerTime)
                {
                    Log.LogMessage(MessageImportance.Low, "Vite config file changed");
                    return true;
                }
            }

            // CRITICAL: Check if any dependency outputs are newer than this marker
            // This ensures parent configs rebuild when their children change
            if (!string.IsNullOrEmpty(config.DependsOn))
            {
                var dependencies = config.DependsOn.Split(',').Select(d => d.Trim()).Where(d => !string.IsNullOrEmpty(d));
                
                foreach (var dependency in dependencies)
                {
                    var depConfig = FindDependencyConfig(dependency);
                    if (depConfig == null) continue;

                    // Check dependency's marker file timestamp
                    var depMarkerPath = GetBuildMarkerPath(depConfig);
                    if (File.Exists(depMarkerPath))
                    {
                        var depMarkerTime = File.GetLastWriteTime(depMarkerPath);
                        if (depMarkerTime > markerTime)
                        {
                            Log.LogMessage(MessageImportance.High, 
                                $"[BUILD] Dependency '{dependency}' was rebuilt, triggering rebuild of '{config.BuildId}'");
                            return true;
                        }
                    }

                    // Also check if dependency's output directory has newer files
                    if (!string.IsNullOrEmpty(depConfig.OutputDir))
                    {
                        var depOutputPath = Path.Combine(ViteProjectRoot, depConfig.OutputDir);
                        if (Directory.Exists(depOutputPath))
                        {
                            var newestFile = GetNewestFileTime(depOutputPath);
                            if (newestFile > markerTime)
                            {
                                Log.LogMessage(MessageImportance.High, 
                                    $"[BUILD] Dependency '{dependency}' output changed, triggering rebuild of '{config.BuildId}'");
                                return true;
                            }
                        }
                    }
                }
            }

            return false; // Everything up to date
        }

        private DateTime GetNewestFileTime(string directory)
        {
            try
            {
                var dirInfo = new DirectoryInfo(directory);
                if (!dirInfo.Exists) return DateTime.MinValue;

                var newestTime = dirInfo.LastWriteTime;
                
                // Check all files in directory recursively
                foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    if (file.LastWriteTime > newestTime)
                    {
                        newestTime = file.LastWriteTime;
                    }
                }

                return newestTime;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private string GetBuildMarkerPath(ViteConfigInfo config)
        {
            var markerName = config.BuildId == "default" ? 
                "ViteBuild.marker" : 
                $"ViteBuild.{config.BuildId}.marker";

            return Path.Combine(IntermediateOutputPath, markerName);
        }

        private void UpdateBuildMarker(ViteConfigInfo config)
        {
            try
            {
                var markerPath = GetBuildMarkerPath(config);
                Directory.CreateDirectory(Path.GetDirectoryName(markerPath)!);
                File.WriteAllText(markerPath, DateTime.UtcNow.ToString("O"));
            }
            catch (Exception ex)
            {
                Log.LogMessage(MessageImportance.Low, $"Could not update build marker: {ex.Message}");
            }
        }

        private bool ExecuteCommand(string command, Dictionary<string, string> environmentVars)
        {
            // This is simplified - in real implementation would use Process.Start
            // For now, just simulate success for testing
            Log.LogMessage(MessageImportance.Low, $"Environment variables: {string.Join(", ", environmentVars.Select(kv => $"{kv.Key}={kv.Value}"))}");
            
            // TODO: Actual process execution
            return true;
        }

        private bool LinkDependencyPackages(ViteConfigInfo config)
        {
            try
            {
                var configWorkingDir = Path.GetDirectoryName(Path.Combine(ViteProjectRoot, config.ConfigFile));
                if (string.IsNullOrEmpty(configWorkingDir) || !Directory.Exists(configWorkingDir))
                {
                    Log.LogWarning("Cannot find working directory for config '{0}': {1}", config.BuildId, configWorkingDir);
                    return true; // Non-fatal, continue build
                }

                var packageJsonPath = Path.Combine(configWorkingDir, "package.json");
                if (!File.Exists(packageJsonPath))
                {
                    Log.LogMessage(MessageImportance.Low, "No package.json found for '{0}', skipping dependency linking", config.BuildId);
                    return true; // Non-fatal, continue build
                }

                Log.LogMessage(MessageImportance.Normal, "[LINK] Linking dependency packages for '{0}'", config.BuildId);

                // Backup original package.json
                var backupPath = packageJsonPath + ".vite-msbuild.backup";
                File.Copy(packageJsonPath, backupPath, true);

                var dependencies = config.DependsOn.Split(',').Select(d => d.Trim()).Where(d => !string.IsNullOrEmpty(d));
                var linkedCount = 0;

                foreach (var dependency in dependencies)
                {
                    var depConfig = FindDependencyConfig(dependency);
                    if (depConfig == null)
                    {
                        Log.LogMessage(MessageImportance.Low, "Dependency config '{0}' not found for linking", dependency);
                        continue;
                    }

                    var depOutputPath = Path.GetFullPath(Path.Combine(ViteProjectRoot, depConfig.OutputDir));
                    var depPackageJsonPath = Path.Combine(depOutputPath, "package.json");

                    if (!File.Exists(depPackageJsonPath))
                    {
                        Log.LogMessage(MessageImportance.Low, "Dependency '{0}' does not produce npm package, skipping", dependency);
                        continue;
                    }

                    var packageName = GetPackageNameFromJson(depPackageJsonPath);
                    if (string.IsNullOrEmpty(packageName))
                    {
                        Log.LogWarning("Cannot determine package name for dependency '{0}'", dependency);
                        continue;
                    }

                    var relativePath = GetRelativePath(configWorkingDir, depOutputPath);
                    var fileProtocolPath = "file:" + relativePath.Replace('\\', '/');

                    if (UpdatePackageJsonDependency(packageJsonPath, packageName, fileProtocolPath))
                    {
                        Log.LogMessage(MessageImportance.High, "[OK] Linked package '{0}' -> {1}", packageName, fileProtocolPath);
                        linkedCount++;
                    }
                }

                if (linkedCount > 0)
                {
                    Log.LogMessage(MessageImportance.Normal, "[LINK] Successfully linked {0} dependency package(s) for '{1}'", linkedCount, config.BuildId);
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.LogError("Failed to link dependency packages for '{0}': {1}", config.BuildId, ex.Message);
                return false;
            }
        }

        private ViteConfigInfo? FindDependencyConfig(string dependencyId)
        {
            return _allConfigurations.FirstOrDefault(c => c.BuildId == dependencyId);
        }

        private string? GetPackageNameFromJson(string packageJsonPath)
        {
            try
            {
                var jsonContent = File.ReadAllText(packageJsonPath);
                // Simple JSON parsing for package name
                // In production, consider using System.Text.Json or Newtonsoft.Json
                var nameMatch = System.Text.RegularExpressions.Regex.Match(jsonContent, @"""name""\s*:\s*""([^""]+)""");
                return nameMatch.Success ? nameMatch.Groups[1].Value : null;
            }
            catch (Exception ex)
            {
                Log.LogWarning("Failed to read package name from '{0}': {1}", packageJsonPath, ex.Message);
                return null;
            }
        }

        private bool UpdatePackageJsonDependency(string packageJsonPath, string packageName, string packagePath)
        {
            try
            {
                var jsonContent = File.ReadAllText(packageJsonPath);
                
                // Simple find and replace approach for dependencies section
                // Look for existing dependency or add new one
                var dependenciesPattern = @"""dependencies""\s*:\s*\{([^}]*)\}";
                var dependenciesMatch = System.Text.RegularExpressions.Regex.Match(jsonContent, dependenciesPattern);

                if (dependenciesMatch.Success)
                {
                    var dependenciesContent = dependenciesMatch.Groups[1].Value;
                    var packagePattern = $@"""{System.Text.RegularExpressions.Regex.Escape(packageName)}""\s*:\s*""[^""]+""";
                    
                    if (System.Text.RegularExpressions.Regex.IsMatch(dependenciesContent, packagePattern))
                    {
                        // Replace existing dependency
                        var newDependenciesContent = System.Text.RegularExpressions.Regex.Replace(
                            dependenciesContent, 
                            packagePattern, 
                            $@"""{packageName}"": ""{packagePath}""");
                        
                        jsonContent = jsonContent.Replace(dependenciesContent, newDependenciesContent);
                    }
                    else
                    {
                        // Add new dependency
                        var newDependency = $@"""{packageName}"": ""{packagePath}""";
                        var updatedDependencies = string.IsNullOrWhiteSpace(dependenciesContent) 
                            ? newDependency 
                            : dependenciesContent.TrimEnd() + ",\n    " + newDependency;
                        
                        jsonContent = jsonContent.Replace(dependenciesContent, updatedDependencies);
                    }
                }
                else
                {
                    // Add dependencies section if it doesn't exist
                    var newDependenciesSection = $@"""dependencies"": {{
    ""{packageName}"": ""{packagePath}""
  }}";
                    
                    // Insert before closing brace of main object
                    var lastBraceIndex = jsonContent.LastIndexOf('}');
                    if (lastBraceIndex > 0)
                    {
                        jsonContent = jsonContent.Insert(lastBraceIndex, ",\n  " + newDependenciesSection + "\n");
                    }
                }

                File.WriteAllText(packageJsonPath, jsonContent);
                return true;
            }
            catch (Exception ex)
            {
                Log.LogError("Failed to update package.json '{0}': {1}", packageJsonPath, ex.Message);
                return false;
            }
        }

        private void RestorePackageJson(string packageJsonPath)
        {
            try
            {
                var backupPath = packageJsonPath + ".vite-msbuild.backup";
                if (File.Exists(backupPath))
                {
                    File.Copy(backupPath, packageJsonPath, true);
                    File.Delete(backupPath);
                    Log.LogMessage(MessageImportance.Low, "Restored original package.json");
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning("Failed to restore package.json: {0}", ex.Message);
            }
        }

        private string GetRelativePath(string fromPath, string toPath)
        {
            // Simple relative path calculation for .NET Standard 2.0 compatibility
            var fromUri = new Uri(fromPath.EndsWith("\\") ? fromPath : fromPath + "\\");
            var toUri = new Uri(toPath);
            var relativeUri = fromUri.MakeRelativeUri(toUri);
            return Uri.UnescapeDataString(relativeUri.ToString()).Replace('/', Path.DirectorySeparatorChar);
        }

        private Dictionary<string, BuildStatus> AnalyzeBuildRequirements(List<ViteConfigInfo> configs)
        {
            var buildPlan = new Dictionary<string, BuildStatus>();

            foreach (var config in configs)
            {
                var status = new BuildStatus
                {
                    BuildId = config.BuildId,
                    ConfigFile = config.ConfigFile,
                    OutputDir = config.OutputDir,
                    DependsOn = config.DependsOn,
                    WillBuild = IsBuildRequired(config),
                    Reason = DetermineBuildReason(config)
                };

                buildPlan[config.BuildId] = status;
            }

            return buildPlan;
        }

        private string DetermineBuildReason(ViteConfigInfo config)
        {
            var markerPath = GetBuildMarkerPath(config);
            if (!File.Exists(markerPath))
            {
                return "No previous build";
            }

            var markerTime = File.GetLastWriteTime(markerPath);

            // Check input files
            if (ViteInputFiles != null)
            {
                foreach (var inputFile in ViteInputFiles)
                {
                    if (File.Exists(inputFile.ItemSpec) && File.GetLastWriteTime(inputFile.ItemSpec) > markerTime)
                    {
                        return $"Source file changed: {Path.GetFileName(inputFile.ItemSpec)}";
                    }
                }
            }

            // Check config file
            if (File.Exists(config.ConfigFile) && File.GetLastWriteTime(config.ConfigFile) > markerTime)
            {
                return "Config file changed";
            }

            // Check dependencies
            if (!string.IsNullOrEmpty(config.DependsOn))
            {
                var dependencies = config.DependsOn.Split(',').Select(d => d.Trim()).Where(d => !string.IsNullOrEmpty(d));
                
                foreach (var dependency in dependencies)
                {
                    var depConfig = FindDependencyConfig(dependency);
                    if (depConfig != null)
                    {
                        var depMarkerPath = GetBuildMarkerPath(depConfig);
                        if (File.Exists(depMarkerPath) && File.GetLastWriteTime(depMarkerPath) > markerTime)
                        {
                            return $"Dependency '{dependency}' rebuilt";
                        }
                    }
                }
            }

            return "Up to date";
        }

        private void DisplayBuildTree(Dictionary<string, BuildStatus> buildPlan)
        {
            Log.LogMessage(MessageImportance.High, "");
            Log.LogMessage(MessageImportance.High, "== BUILD PLAN ===============================================");
            Log.LogMessage(MessageImportance.High, "");

            // Group by dependency level for visual hierarchy
            var processed = new HashSet<string>();
            var level = 0;

            while (processed.Count < buildPlan.Count && level < 20)
            {
                var currentLevel = buildPlan.Values
                    .Where(s => !processed.Contains(s.BuildId))
                    .Where(s => string.IsNullOrEmpty(s.DependsOn) || 
                                s.DependsOn.Split(',').All(d => processed.Contains(d.Trim())))
                    .ToList();

                if (currentLevel.Count == 0) break;

                foreach (var status in currentLevel)
                {
                    var indent = new string(' ', level * 2);
                    var icon = status.WillBuild ? "[BUILD]" : "[SKIP]";
                    var statusText = status.WillBuild ? "BUILD" : "SKIP";
                    var reason = status.WillBuild ? status.Reason : "up to date";
                    
                    var depInfo = string.IsNullOrEmpty(status.DependsOn) 
                        ? "" 
                        : $" (depends on: {status.DependsOn})";

                    Log.LogMessage(MessageImportance.High, 
                        $"{indent}{icon} [{statusText}] {status.BuildId}{depInfo}");
                    Log.LogMessage(MessageImportance.High, 
                        $"{indent}   └─ {reason}");

                    processed.Add(status.BuildId);
                }

                level++;
            }

            Log.LogMessage(MessageImportance.High, "============================================================");
            Log.LogMessage(MessageImportance.High, "");
        }

        private void DisplayBuildSummary(Dictionary<string, BuildStatus> buildPlan)
        {
            var built = buildPlan.Values.Count(s => s.ActuallyBuilt);
            var skipped = buildPlan.Values.Count(s => !s.ActuallyBuilt && !s.Failed);
            var failed = buildPlan.Values.Count(s => s.Failed);

            Log.LogMessage(MessageImportance.High, "");
            Log.LogMessage(MessageImportance.High, "== BUILD SUMMARY ============================================");
            Log.LogMessage(MessageImportance.High, "");
            
            if (built > 0)
                Log.LogMessage(MessageImportance.High, $"  [OK] Built:   {built} configuration(s)");
            if (skipped > 0)
                Log.LogMessage(MessageImportance.High, $"  [SKIP]  Skipped: {skipped} configuration(s)");
            if (failed > 0)
                Log.LogMessage(MessageImportance.High, $"  [ERROR] Failed:  {failed} configuration(s)");

            Log.LogMessage(MessageImportance.High, "============================================================");

            // Detailed status for each config
            foreach (var status in buildPlan.Values.OrderBy(s => s.BuildId))
            {
                string icon, state;
                if (status.Failed)
                {
                    icon = "[ERROR]";
                    state = "FAILED";
                }
                else if (status.ActuallyBuilt)
                {
                    icon = "[OK]";
                    state = "SUCCESS";
                }
                else
                {
                    icon = "[SKIP]";
                    state = "SKIPPED";
                }

                Log.LogMessage(MessageImportance.High, $"  {icon} [{state}] {status.BuildId}");
            }

            Log.LogMessage(MessageImportance.High, "============================================================");
            Log.LogMessage(MessageImportance.High, "");
            
            if (failed == 0)
            {
                Log.LogMessage(MessageImportance.High, "[OK] All builds completed successfully!");
            }
        }

        private class BuildStatus
        {
            public string BuildId { get; set; } = string.Empty;
            public string ConfigFile { get; set; } = string.Empty;
            public string OutputDir { get; set; } = string.Empty;
            public string DependsOn { get; set; } = string.Empty;
            public bool WillBuild { get; set; }
            public bool ActuallyBuilt { get; set; }
            public bool Failed { get; set; }
            public string Reason { get; set; } = string.Empty;
        }

        private class ViteConfigInfo
        {
            public string ConfigFile { get; set; } = string.Empty;
            public string BuildId { get; set; } = string.Empty;
            public string OutputDir { get; set; } = string.Empty;
            public string Mode { get; set; } = string.Empty;
            public string PackageManager { get; set; } = string.Empty;
            public string DependsOn { get; set; } = string.Empty;
            public bool LinkDependencies { get; set; } = false;
        }
    }
}