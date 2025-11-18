using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Vite.MsBuild.Tasks
{
    /// <summary>
    /// High-performance C# task to replace 200+ lines of XML multi-config resolution logic
    /// Resolves Vite configurations with smart defaults and validation
    /// </summary>
    public class ResolveViteConfigurationsTask : Task
    {
        [Required]
        public string ViteProjectRoot { get; set; } = string.Empty;

        public string? ViteConfigFile { get; set; }

        public string ViteOutputDir { get; set; } = "wwwroot/dist";

        public string ViteMode { get; set; } = "development";

        public string PackageManager { get; set; } = "npm";

        public ITaskItem[]? UserDefinedConfigs { get; set; }

        [Output]
        public ITaskItem[] ResolvedConfigs { get; set; } = Array.Empty<ITaskItem>();

        [Output]
        public bool IsMultiConfig { get; set; }

        [Output]
        public int ConfigCount { get; set; }

        public override bool Execute()
        {
            try
            {
                if (string.IsNullOrEmpty(ViteProjectRoot) || !Directory.Exists(ViteProjectRoot))
                {
                    Log.LogError($"ViteProjectRoot '{ViteProjectRoot}' does not exist");
                    return false;
                }

                var resolvedConfigs = new List<TaskItem>();

                // Check if user defined multiple configurations
                if (UserDefinedConfigs != null && UserDefinedConfigs.Length > 0)
                {
                    resolvedConfigs.AddRange(ProcessUserDefinedConfigs());
                    IsMultiConfig = resolvedConfigs.Count > 1;
                }
                else
                {
                    // Single configuration with auto-detection
                    var singleConfig = CreateDefaultConfiguration();
                    if (singleConfig != null)
                    {
                        resolvedConfigs.Add(singleConfig);
                        IsMultiConfig = false;
                    }
                }

                // Validate all configurations
                if (!ValidateConfigurations(resolvedConfigs))
                {
                    return false;
                }

                ResolvedConfigs = resolvedConfigs.ToArray();
                ConfigCount = ResolvedConfigs.Length;

                if (IsMultiConfig)
                {
                    Log.LogMessage(MessageImportance.Normal, 
                        $"🎯 Resolved {ConfigCount} Vite configurations for multi-SPA build");
                }
                else
                {
                    Log.LogMessage(MessageImportance.Normal, 
                        $"🎯 Resolved single Vite configuration: {ResolvedConfigs[0].GetMetadata("ConfigFile")}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to resolve Vite configurations: {ex.Message}");
                return false;
            }
        }

        private List<TaskItem> ProcessUserDefinedConfigs()
        {
            var configs = new List<TaskItem>();

            foreach (var userConfig in UserDefinedConfigs!)
            {
                var configPath = userConfig.ItemSpec;
                var buildId = userConfig.GetMetadata("BuildId");
                var outputDir = userConfig.GetMetadata("OutputDir");
                var mode = userConfig.GetMetadata("Mode");

                // Resolve to absolute path
                if (!Path.IsPathRooted(configPath))
                {
                    configPath = Path.Combine(ViteProjectRoot, configPath);
                }

                var config = new TaskItem(configPath);
                
                // Set metadata with smart defaults
                config.SetMetadata("ConfigFile", configPath);
                config.SetMetadata("BuildId", string.IsNullOrEmpty(buildId) ? 
                    GenerateBuildId(configPath) : buildId);
                config.SetMetadata("OutputDir", string.IsNullOrEmpty(outputDir) ? 
                    GenerateOutputDir(configPath) : outputDir);
                config.SetMetadata("Mode", string.IsNullOrEmpty(mode) ? ViteMode : mode);
                config.SetMetadata("PackageManager", PackageManager);
                config.SetMetadata("ProjectRoot", ViteProjectRoot);

                // Detect architecture pattern
                config.SetMetadata("Architecture", DetectArchitecture(configPath));

                configs.Add(config);
            }

            return configs;
        }

        private TaskItem? CreateDefaultConfiguration()
        {
            var configFile = FindDefaultConfigFile();
            if (configFile == null)
            {
                Log.LogMessage(MessageImportance.Normal, 
                    "No vite.config file found, but Vite can work with defaults");
                configFile = Path.Combine(ViteProjectRoot, "vite.config.ts"); // Virtual config
            }

            var config = new TaskItem(configFile);
            config.SetMetadata("ConfigFile", configFile);
            config.SetMetadata("BuildId", "default");
            config.SetMetadata("OutputDir", ViteOutputDir);
            config.SetMetadata("Mode", ViteMode);
            config.SetMetadata("PackageManager", PackageManager);
            config.SetMetadata("ProjectRoot", ViteProjectRoot);
            config.SetMetadata("Architecture", "SPA");

            return config;
        }

        private string? FindDefaultConfigFile()
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
                    Log.LogMessage(MessageImportance.Low, $"Found Vite config: {configName}");
                    return configPath;
                }
            }

            return null;
        }

        private string GenerateBuildId(string configPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(configPath);
            
            // Extract meaningful part from config file name
            // vite.admin.config.ts -> admin
            // Areas/Admin/vite.config.ts -> admin
            
            if (fileName.Contains('.'))
            {
                var parts = fileName.Split('.');
                if (parts.Length >= 2 && parts[0] == "vite")
                {
                    return parts[1].ToLowerInvariant();
                }
            }

            // Use parent directory name for Areas pattern
            var parentDir = Path.GetFileName(Path.GetDirectoryName(configPath));
            if (!string.IsNullOrEmpty(parentDir) && 
                !parentDir.Equals("Areas", StringComparison.OrdinalIgnoreCase) &&
                !parentDir.Equals(Path.GetFileName(ViteProjectRoot), StringComparison.OrdinalIgnoreCase))
            {
                return parentDir.ToLowerInvariant();
            }

            return Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();
        }

        private string GenerateOutputDir(string configPath)
        {
            var buildId = GenerateBuildId(configPath);
            
            if (buildId == "default" || string.IsNullOrEmpty(buildId))
            {
                return ViteOutputDir;
            }

            // Smart output directory based on architecture
            var architecture = DetectArchitecture(configPath);
            
            return architecture switch
            {
                "Areas" => $"wwwroot/{buildId}",
                "MultiSPA" => $"wwwroot/spa/{buildId}",
                _ => $"wwwroot/{buildId}"
            };
        }

        private string DetectArchitecture(string configPath)
        {
            var relativePath = GetRelativePathCompat(ViteProjectRoot, configPath);
            
            if (relativePath.StartsWith("Areas", StringComparison.OrdinalIgnoreCase))
            {
                return "Areas";
            }
            
            if (relativePath.Contains("spa", StringComparison.OrdinalIgnoreCase) ||
                relativePath.Contains("apps", StringComparison.OrdinalIgnoreCase))
            {
                return "MultiSPA";
            }

            return "SPA";
        }

        private string GetRelativePathCompat(string basePath, string fullPath)
        {
            // .NET Standard 2.0 compatible relative path
            var baseUri = new Uri(basePath.TrimEnd('\\') + "\\");
            var fullUri = new Uri(fullPath);
            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString().Replace('/', '\\'));
        }

        private bool ValidateConfigurations(List<TaskItem> configs)
        {
            if (configs.Count == 0)
            {
                Log.LogError("No Vite configurations resolved");
                return false;
            }

            var buildIds = new HashSet<string>();
            var outputDirs = new HashSet<string>();

            foreach (var config in configs)
            {
                var configFile = config.GetMetadata("ConfigFile");
                var buildId = config.GetMetadata("BuildId");
                var outputDir = config.GetMetadata("OutputDir");

                // Validate config file exists (unless it's the virtual default)
                if (!configFile.EndsWith("vite.config.ts") || buildId != "default")
                {
                    if (!File.Exists(configFile))
                    {
                        Log.LogError($"Vite config file not found: {configFile}");
                        return false;
                    }
                }

                // Check for duplicate build IDs
                if (!buildIds.Add(buildId))
                {
                    Log.LogError($"Duplicate BuildId '{buildId}' found. Each ViteConfig must have a unique BuildId.");
                    return false;
                }

                // Check for conflicting output directories
                if (!outputDirs.Add(outputDir))
                {
                    Log.LogWarning($"Multiple configurations output to same directory: {outputDir}");
                }

                Log.LogMessage(MessageImportance.Low, 
                    $"✅ Validated config: {buildId} → {outputDir} ({config.GetMetadata("Architecture")})");
            }

            return true;
        }
    }
}